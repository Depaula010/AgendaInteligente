using AgendaInteligente.Api.Contracts.Requests.Webhook;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IWebhookService
{
    Task ProcessWhatsAppMessageAsync(WebhookMessageRequest request, CancellationToken ct = default);
}
