using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AgendaInteligente.Api.Services;

public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(ILogger<WebhookService> logger)
    {
        _logger = logger;
    }

    public Task ProcessWhatsAppMessageAsync(WebhookMessageRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.SenderPhone))
            throw new ArgumentException("SenderPhone é obrigatório.", nameof(request.SenderPhone));

        if (string.IsNullOrWhiteSpace(request.MessageText))
            throw new ArgumentException("MessageText é obrigatório.", nameof(request.MessageText));

        if (string.IsNullOrWhiteSpace(request.MessageId))
            throw new ArgumentException("MessageId é obrigatório.", nameof(request.MessageId));

        if (request.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId é obrigatório.", nameof(request.TenantId));

        _logger.LogInformation(
            "Recebendo mensagem do WhatsApp. TenantId: {TenantId}, Sender: {SenderPhone}, MessageId: {MessageId}", 
            request.TenantId, request.SenderPhone, request.MessageId);

        // TODO: Integração futura com Redis (debounce) e Google Gemini.

        return Task.CompletedTask;
    }
}
