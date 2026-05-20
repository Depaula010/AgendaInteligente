using AgendaInteligente.Api.Models.AI;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IBotIntentDispatcherService
{
    /// <summary>
    /// Age sobre a intenção detectada pela IA (criar/cancelar agendamento) e retorna
    /// a mensagem final a ser enviada ao cliente via WhatsApp.
    /// Para intenções sem ação de domínio (general, check, reschedule), retorna
    /// <see cref="GeminiIntentResponse.ReplyMessage"/> inalterado.
    /// </summary>
    Task<string> DispatchAsync(
        GeminiIntentResponse aiResponse,
        Guid tenantId,
        string senderPhone,
        CancellationToken ct = default);
}
