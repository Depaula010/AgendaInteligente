using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AgendaInteligente.Api.Services;

public sealed class WhatsAppSendService : IWhatsAppSendService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppBotOptions _options;
    private readonly ILogger<WhatsAppSendService> _logger;

    public WhatsAppSendService(
        HttpClient httpClient,
        IOptions<WhatsAppBotOptions> options,
        ILogger<WhatsAppSendService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendTextMessageAsync(Guid tenantId, string phone, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUrl))
        {
            _logger.LogInformation(
                "[WHATSAPP-STUB] BotUrl não configurado. TenantId={TenantId}, Phone={Phone}, Mensagem: {Message}",
                tenantId, phone, message);
            return true;
        }

        try
        {
            var payload = new { tenantId, phone, message };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.BotUrl}/api/v1/whatsapp/send",
                payload,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Falha ao enviar mensagem ao bot. Status={StatusCode}, TenantId={TenantId}, Phone={Phone}",
                    (int)response.StatusCode, tenantId, phone);
                return false;
            }

            _logger.LogInformation(
                "Mensagem enviada ao bot com sucesso. TenantId={TenantId}, Phone={Phone}",
                tenantId, phone);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar bot Node.js. TenantId={TenantId}, Phone={Phone}.", tenantId, phone);
            return false;
        }
    }
}
