using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgendaInteligente.Api.Contracts.Models;
using AgendaInteligente.Api.Contracts.Reminders;
using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class WebhookService : IWebhookService
{
    private readonly IConversationHistoryService _conversationHistory;
    private readonly IAiOrchestratorService      _aiOrchestrator;
    private readonly IBotIntentDispatcherService _intentDispatcher;
    private readonly IScheduleService            _scheduleService;
    private readonly ICustomerRepository         _customerRepo;
    private readonly IDistributedCache           _cache;
    private readonly ILogger<WebhookService>     _logger;

    public WebhookService(
        IConversationHistoryService conversationHistory,
        IAiOrchestratorService aiOrchestrator,
        IBotIntentDispatcherService intentDispatcher,
        IScheduleService scheduleService,
        ICustomerRepository customerRepo,
        IDistributedCache cache,
        ILogger<WebhookService> logger)
    {
        _conversationHistory = conversationHistory;
        _aiOrchestrator      = aiOrchestrator;
        _intentDispatcher    = intentDispatcher;
        _scheduleService     = scheduleService;
        _customerRepo        = customerRepo;
        _cache               = cache;
        _logger              = logger;
    }

    public async Task<BotReply> ProcessWhatsAppMessageAsync(Guid tenantId, BotWebhookRequest request, CancellationToken ct = default)
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
            return BotReply.Empty;
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

        // 3. Confirmação de lembrete pendente — intercepta ANTES do AI para economizar chamadas
        var confirmKey  = $"reminder:confirm:{tenantId}:{request.NumeroRemetente}";
        var pendingJson = await _cache.GetStringAsync(confirmKey, ct);
        if (pendingJson is not null)
        {
            var state = JsonSerializer.Deserialize<PendingReminderState>(pendingJson)!;
            var (replyText, clearKey) = await HandleReminderConfirmationAsync(state, request.Texto, ct);

            if (clearKey)
                await _cache.RemoveAsync(confirmKey, ct);

            _logger.LogInformation(
                "Confirmação de lembrete processada. TenantId={TenantId}, ScheduleId={ScheduleId}, ClearKey={Clear}",
                tenantId, state.ScheduleId, clearKey);

            return BotReply.FromText(replyText);
        }

        // 4. Histórico da conversa — carrega do Redis (chave: chat:{tenantId}:{phone})
        var history = await _conversationHistory.GetHistoryAsync(tenantId, request.NumeroRemetente, ct);

        // 5. Processamento via IA
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
            return BotReply.FromText("Desculpe, ocorreu um erro interno. Por favor, tente novamente em instantes.");
        }

        // 6. Dispatch de intenção — age sobre schedule/cancel ou passa reply da IA inalterado
        var reply = await _intentDispatcher.DispatchAsync(aiResponse, tenantId, request.NumeroRemetente, ct);

        // 7. Persiste histórico (turno atual) — armazena texto ou string vazia para interativas
        var updatedHistory = new List<MessageHistory>(history)
        {
            new() { Role = "user",  Content = request.Texto },
            new() { Role = "model", Content = reply.Text ?? "" }
        };
        await _conversationHistory.SaveHistoryAsync(tenantId, request.NumeroRemetente, updatedHistory, ct);

        // 8. Retorna resposta — consumer/endpoint despacha texto ou lista interativa conforme tipo
        return reply;
    }

    // ── Confirmação de lembrete ───────────────────────────────────────────────────

    private async Task<(string Reply, bool ClearKey)> HandleReminderConfirmationAsync(
        PendingReminderState state, string text, CancellationToken ct)
    {
        var normalized = text.Trim().ToLowerInvariant();

        if (normalized is "1" or "confirmar" or "confirmado" or "sim" or "ok")
        {
            await _scheduleService.UpdateStatusAsync(state.ScheduleId, ScheduleStatus.Confirmed, ct);
            return (
                $"Confirmado! Te esperamos em {state.AppointmentStart:dd/MM} as " +
                $"{state.AppointmentStart:HH:mm}. Ate mais!",
                true);
        }

        if (normalized is "3" or "cancelar" or "cancela" or "nao" or "não")
        {
            await _scheduleService.UpdateStatusAsync(state.ScheduleId, ScheduleStatus.Cancelled, ct);
            return ("Agendamento cancelado. Se quiser remarcar, e so falar comigo!", true);
        }

        if (normalized is "2" or "remarcar" or "remarca" or "reagendar")
        {
            // Limpa o estado pendente — próxima mensagem entra no fluxo normal de agendamento
            return ("Claro! Qual data e horario voce prefere?", true);
        }

        // Resposta não reconhecida — re-pergunta sem limpar o estado pendente
        return (
            "Nao entendi. Por favor, responda:\n" +
            "1 - Confirmar\n" +
            "2 - Remarcar\n" +
            "3 - Cancelar\n\n" +
            $"(Agendamento: {state.ServiceName} em {state.AppointmentStart:dd/MM} as {state.AppointmentStart:HH:mm})",
            false);
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
