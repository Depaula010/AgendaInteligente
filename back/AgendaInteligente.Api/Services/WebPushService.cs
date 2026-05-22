using System.Text.Json;
using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace AgendaInteligente.Api.Services;

public sealed class WebPushService : IWebPushService
{
    private readonly IPushSubscriptionRepository _subRepo;
    private readonly IProfessionalRepository     _professionalRepo;
    private readonly VapidOptions                _vapid;
    private readonly ILogger<WebPushService>     _logger;

    public WebPushService(
        IPushSubscriptionRepository subRepo,
        IProfessionalRepository professionalRepo,
        IOptions<VapidOptions> vapidOptions,
        ILogger<WebPushService> logger)
    {
        _subRepo          = subRepo;
        _professionalRepo = professionalRepo;
        _vapid            = vapidOptions.Value;
        _logger           = logger;
    }

    public async Task NotifyAsync(Guid professionalId, string title, string body, CancellationToken ct = default)
    {
        if (!_vapid.IsConfigured)
        {
            _logger.LogDebug("VAPID keys not configured — skipping push notification.");
            return;
        }

        var allSubs = await _subRepo.GetAllAsync(ct);
        if (allSubs.Count == 0) return;

        // Determina quem deve receber: o profissional do agendamento + todos os owners
        var allProfessionals = await _professionalRepo.GetAllAsync(ct);
        var recipientIds = allProfessionals
            .Where(p => p.Role == ProfessionalRole.Owner || p.Id == professionalId)
            .Select(p => p.Id)
            .ToHashSet();

        var relevantSubs = allSubs.Where(s => recipientIds.Contains(s.ProfessionalId)).ToList();
        if (relevantSubs.Count == 0) return;

        var payload     = JsonSerializer.Serialize(new { title, body, url = "/dashboard/agenda" });
        var vapidDetails = new VapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);

        using var client = new WebPushClient();

        foreach (var sub in relevantSubs)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _logger.LogInformation(
                    "Push subscription expirada. Removendo endpoint {Endpoint}.", sub.Endpoint);
                await _subRepo.DeleteByEndpointAsync(sub.Endpoint, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Falha ao enviar push notification para endpoint {Endpoint}.", sub.Endpoint);
            }
        }
    }
}
