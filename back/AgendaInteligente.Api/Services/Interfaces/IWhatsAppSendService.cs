using AgendaInteligente.Api.Contracts.Models;

namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Canal único de saída para envio de mensagens via WhatsApp.
/// Toda resposta ao cliente DEVE passar por este serviço (ARCHITECTURE.md §3.2).
/// </summary>
public interface IWhatsAppSendService
{
    /// <summary>Envia mensagem de texto simples via bot Node.js (Baileys).</summary>
    Task<bool> SendTextMessageAsync(Guid tenantId, string phone, string message, CancellationToken ct = default);

    /// <summary>Envia lista interativa (WhatsApp list message) via bot Node.js (Baileys).</summary>
    Task<bool> SendInteractiveListAsync(Guid tenantId, string phone, InteractiveListPayload payload, CancellationToken ct = default);
}
