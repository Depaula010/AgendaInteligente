using AgendaInteligente.Api.Contracts.Requests.Webhook;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IWebhookService
{
    Task<string> ProcessWhatsAppMessageAsync(Guid tenantId, BotWebhookRequest request, CancellationToken ct = default);
}
