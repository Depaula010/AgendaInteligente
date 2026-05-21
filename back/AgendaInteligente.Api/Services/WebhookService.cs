using System.Security.Cryptography;
using System.Text;
using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class WebhookService : IWebhookService
{
    private readonly IConversationHistoryService _conversationHistory;
    private readonly IAiOrchestratorService      _aiOrchestrator;
    private readonly IBotIntentDispatcherService _intentDispatcher;
    private readonly ICustomerRepository         _customerRepo;
    private readonly ILogger<WebhookService>     _logger;

    public WebhookService(
        IConversationHistoryService conversationHistory,
        IAiOrchestratorService aiOrchestrator,
        IBotIntentDispatcherService intentDispatcher,
        ICustomerRepository customerRepo,
        ILogger<WebhookService> logger)
    {
        _conversationHistory = conversationHistory;
        _aiOrchestrator      = aiOrchestrator;
        _intentDispatcher    = intentDispatcher;
        _customerRepo        = customerRepo;
        _logger              = logger;
    }

    public async Task<string> ProcessWhatsAppMessageAsync(Guid tenantId, BotWebhookRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId é obrigatório.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(request.NumeroRemetente))
            throw new ArgumentException("NumeroRemetente é obrigatório.", nameof(request.NumeroRemetente));

        if (string.IsNullOrWhiteSpace(request.Texto))
            throw new ArgumentException("Texto é obrigatório.", nameof(request.Texto));

        var messageId = GenerateMessageId(tenantId, request.NumeroRemetente, request.Texto);

        _logger.LogInformation(
            "Mensagem recebida via WhatsApp. TenantId={TenantId}, Phone={Phone}, MessageId={MessageId}",
            tenantId, request.NumeroRemetente, messageId);

        // 1. Debounce — ignora mensagens duplicadas entregues mais de uma vez pelo bot
        if (await _conversationHistory.IsMessageDuplicateAsync(messageId, ct))
        {
            _logger.LogWarning(
                "Mensagem duplicada ignorada. MessageId={MessageId}, TenantId={TenantId}",
                messageId, tenantId);
            return string.Empty;
        }

        // 2. B22 — Find-or-create: todo número que entra recebe um Customer
        // GetByPhoneAndTenantAsync ignora o filtro global (sem JWT no webhook path)
        var existing = await _customerRepo.GetByPhoneAndTenantAsync(request.NumeroRemetente, tenantId, ct);
        if (existing is null)
        {
            await _customerRepo.CreateAsync(new Customer
            {
                Name        = request.NumeroRemetente,
                PhoneNumber = request.NumeroRemetente,
                TenantId    = tenantId
            }, ct);

            _logger.LogInformation(
                "Novo Customer registrado automaticamente via WhatsApp. TenantId={TenantId}, Phone={Phone}",
                tenantId, request.NumeroRemetente);
        }

        // 3. Histórico da conversa — carrega do Redis (chave: chat:{tenantId}:{phone})
        var history = await _conversationHistory.GetHistoryAsync(tenantId, request.NumeroRemetente, ct);

        // 4. Processamento via IA
        GeminiIntentResponse aiResponse;
        try
        {
            aiResponse = await _aiOrchestrator.ProcessUserMessageAsync(tenantId, request.Texto, history);

            _logger.LogInformation(
                "IA processou mensagem. TenantId={TenantId}, Intent={Intent}",
                tenantId, aiResponse.Intent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao processar mensagem com IA. TenantId={TenantId}, MessageId={MessageId}.",
                tenantId, messageId);
            return "Desculpe, ocorreu um erro interno. Por favor, tente novamente em instantes.";
        }

        // 5. Dispatch de intenção — age sobre schedule/cancel ou passa reply da IA inalterado
        var replyMessage = await _intentDispatcher.DispatchAsync(aiResponse, tenantId, request.NumeroRemetente, ct);

        // 6. Persiste histórico (turno atual)
        var updatedHistory = new List<MessageHistory>(history)
        {
            new() { Role = "user",  Content = request.Texto },
            new() { Role = "model", Content = replyMessage }
        };
        await _conversationHistory.SaveHistoryAsync(tenantId, request.NumeroRemetente, updatedHistory, ct);

        // 7. Retorna resposta — o bot recebe via JSON { resposta } e encaminha ao usuário
        return replyMessage;
    }

    private static string GenerateMessageId(Guid tenantId, string phone, string text)
    {
        // Agrupado por minuto: tolera pequenas variações de timestamp sem duplicar mensagens reais
        var minute    = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var raw       = $"{tenantId}:{phone}:{text}:{minute}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
