using AgendaInteligente.Api.Contracts.Requests.Webhook;
using System.Threading.Tasks;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IWebhookService
{
    Task ProcessWhatsAppMessageAsync(WebhookMessageRequest request);
}
