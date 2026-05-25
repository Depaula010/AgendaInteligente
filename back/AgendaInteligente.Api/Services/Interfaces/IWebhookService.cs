using AgendaInteligente.Api.Contracts.Models;
using AgendaInteligente.Api.Contracts.Requests.Webhook;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IWebhookService
{
    Task<BotReply> ProcessWhatsAppMessageAsync(Guid tenantId, BotWebhookRequest request, CancellationToken ct = default);
}
