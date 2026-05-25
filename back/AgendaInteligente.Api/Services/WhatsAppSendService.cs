using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Contracts.Models;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AgendaInteligente.Api.Services;

public sealed class WhatsAppSendService : IWhatsAppSendService
{
    private readonly IHttpClientFactory           _httpClientFactory;
    private readonly WhatsAppBotOptions           _options;
    private readonly ITenantSettingsRepository    _settingsRepo;
    private readonly ILogger<WhatsAppSendService> _logger;

    public WhatsAppSendService(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppBotOptions> options,
        ITenantSettingsRepository settingsRepo,
        ILogger<WhatsAppSendService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options           = options.Value;
        _settingsRepo      = settingsRepo;
        _logger            = logger;
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

        var settings = await _settingsRepo.GetByTenantIdAsync(tenantId, ct);
        if (settings?.BotSessionId is null)
        {
            _logger.LogWarning(
                "BotSessionId não configurado para o tenant. TenantId={TenantId}", tenantId);
            return false;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.BotApiKey);

            var payload  = new { numero = phone, mensagem = message };
            var response = await httpClient.PostAsJsonAsync(
                $"{_options.BotUrl}/api/v1/sessions/{settings.BotSessionId}/send-message",
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

    public async Task<bool> SendInteractiveListAsync(Guid tenantId, string phone, InteractiveListPayload payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUrl))
        {
            _logger.LogInformation(
                "[WHATSAPP-STUB] BotUrl não configurado (interactive). TenantId={TenantId}, Phone={Phone}",
                tenantId, phone);
            return true;
        }

        var settings = await _settingsRepo.GetByTenantIdAsync(tenantId, ct);
        if (settings?.BotSessionId is null)
        {
            _logger.LogWarning(
                "BotSessionId não configurado para o tenant. TenantId={TenantId}", tenantId);
            return false;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.BotApiKey);

            var botPayload = new
            {
                numero = phone,
                interactiveList = new
                {
                    title      = payload.Title,
                    body       = payload.Body,
                    buttonText = payload.ButtonText,
                    sections   = payload.Sections.Select(s => new
                    {
                        title = s.Title,
                        rows  = s.Rows.Select(r => new
                        {
                            rowId       = r.RowId,
                            title       = r.Title,
                            description = r.Description ?? string.Empty
                        })
                    })
                }
            };

            var response = await httpClient.PostAsJsonAsync(
                $"{_options.BotUrl}/api/v1/sessions/{settings.BotSessionId}/send-interactive",
                botPayload,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Falha ao enviar lista interativa ao bot. Status={StatusCode}, TenantId={TenantId}, Phone={Phone}",
                    (int)response.StatusCode, tenantId, phone);
                return false;
            }

            _logger.LogInformation(
                "Lista interativa enviada ao bot. TenantId={TenantId}, Phone={Phone}",
                tenantId, phone);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar bot Node.js (interactive). TenantId={TenantId}, Phone={Phone}.", tenantId, phone);
            return false;
        }
    }
}
