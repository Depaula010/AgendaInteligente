using AgendaInteligente.Api.Contracts.Models;
using AgendaInteligente.Api.Models.AI;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IBotIntentDispatcherService
{
    /// <summary>
    /// Age sobre a intenção detectada pela IA (criar/cancelar agendamento) e retorna
    /// a resposta final ao cliente. Pode ser texto simples ou lista interativa (conflito de horário).
    /// </summary>
    Task<BotReply> DispatchAsync(
        GeminiIntentResponse aiResponse,
        Guid tenantId,
        string senderPhone,
        CancellationToken ct = default);
}
