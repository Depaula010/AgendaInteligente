using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class WebhookService : IWebhookService
{
    private readonly IConversationHistoryService _conversationHistory;
    private readonly IAiOrchestratorService _aiOrchestrator;
    private readonly IWhatsAppSendService _whatsAppSend;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IConversationHistoryService conversationHistory,
        IAiOrchestratorService aiOrchestrator,
        IWhatsAppSendService whatsAppSend,
        ILogger<WebhookService> logger)
    {
        _conversationHistory = conversationHistory;
        _aiOrchestrator = aiOrchestrator;
        _whatsAppSend = whatsAppSend;
        _logger = logger;
    }

    public async Task ProcessWhatsAppMessageAsync(WebhookMessageRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SenderPhone))
            throw new ArgumentException("SenderPhone é obrigatório.", nameof(request.SenderPhone));

        if (string.IsNullOrWhiteSpace(request.MessageText))
            throw new ArgumentException("MessageText é obrigatório.", nameof(request.MessageText));

        if (string.IsNullOrWhiteSpace(request.MessageId))
            throw new ArgumentException("MessageId é obrigatório.", nameof(request.MessageId));

        if (request.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId é obrigatório.", nameof(request.TenantId));

        _logger.LogInformation(
            "Mensagem recebida via WhatsApp. TenantId={TenantId}, Phone={Phone}, MessageId={MessageId}",
            request.TenantId, request.SenderPhone, request.MessageId);

        // 1. Debounce — ignora mensagens duplicadas (entregues mais de uma vez pelo Node.js)
        if (await _conversationHistory.IsMessageDuplicateAsync(request.MessageId, ct))
        {
            _logger.LogWarning(
                "Mensagem duplicada ignorada. MessageId={MessageId}, TenantId={TenantId}",
                request.MessageId, request.TenantId);
            return;
        }

        // 2. Histórico da conversa — carrega do Redis (chave: chat:{tenantId}:{phone})
        var history = await _conversationHistory.GetHistoryAsync(request.TenantId, request.SenderPhone, ct);

        // 3. Processamento via IA (sempre via AiOrchestratorService, nunca GeminiService direto)
        string replyMessage;
        try
        {
            var aiResponse = await _aiOrchestrator.ProcessUserMessageAsync(
                request.TenantId,
                request.MessageText,
                history);

            replyMessage = aiResponse.ReplyMessage;

            _logger.LogInformation(
                "IA processou mensagem. TenantId={TenantId}, Intent={Intent}",
                request.TenantId, aiResponse.Intent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao processar mensagem com IA. TenantId={TenantId}, MessageId={MessageId}. Nenhuma resposta será enviada.",
                request.TenantId, request.MessageId);
            return;
        }

        // 4. Cria lista atualizada (histórico anterior + par user/model do turno atual) e persiste no Redis
        var updatedHistory = new List<MessageHistory>(history)
        {
            new() { Role = "user",  Content = request.MessageText },
            new() { Role = "model", Content = replyMessage }
        };
        await _conversationHistory.SaveHistoryAsync(request.TenantId, request.SenderPhone, updatedHistory, ct);

        // 5. Envia resposta ao cliente via bot Node.js (canal único de saída)
        await _whatsAppSend.SendTextMessageAsync(request.TenantId, request.SenderPhone, replyMessage, ct);
    }
}
