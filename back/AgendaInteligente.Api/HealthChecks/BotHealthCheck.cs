using AgendaInteligente.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AgendaInteligente.Api.HealthChecks;

public sealed class BotHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory   _httpClientFactory;
    private readonly WhatsAppBotOptions   _options;

    public BotHealthCheck(IHttpClientFactory httpClientFactory, IOptions<WhatsAppBotOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options           = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUrl))
            return HealthCheckResult.Degraded("BotUrl não configurado — bot WhatsApp desabilitado.");

        try
        {
            var client   = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{_options.BotUrl}/ping", ct);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Bot WhatsApp respondendo.")
                : HealthCheckResult.Degraded($"Bot respondeu com HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Bot WhatsApp inacessível.", ex);
        }
    }
}
