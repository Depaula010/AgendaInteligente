using System.Net.Http.Json;
using System.Text.Json;
using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgendaInteligente.Api.Services;

public sealed class WhatsAppSessionService : IWhatsAppSessionService
{
    private readonly IHttpClientFactory              _httpClientFactory;
    private readonly WhatsAppBotOptions              _options;
    private readonly ITenantSettingsRepository       _settingsRepo;
    private readonly ITenantProvider                 _tenantProvider;
    private readonly ILogger<WhatsAppSessionService> _logger;

    public WhatsAppSessionService(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppBotOptions> options,
        ITenantSettingsRepository settingsRepo,
        ITenantProvider tenantProvider,
        ILogger<WhatsAppSessionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options           = options.Value;
        _settingsRepo      = settingsRepo;
        _tenantProvider    = tenantProvider;
        _logger            = logger;
    }

    public async Task<ServiceResult<WhatsAppSessionResponse>> CreateAndConnectAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.CurrentTenantId;
        if (!tenantId.HasValue)
            return ServiceResult<WhatsAppSessionResponse>.Fail("TenantId não resolvido.");

        if (string.IsNullOrWhiteSpace(_options.BotUrl))
            return ServiceResult<WhatsAppSessionResponse>.Fail("Bot não configurado (BotUrl ausente).");

        var settings = await _settingsRepo.GetAsync(ct);
        if (settings is null)
            return ServiceResult<WhatsAppSessionResponse>.Fail("Configurações do tenant não encontradas.");

        var httpClient = CreateBotClient();

        // Sessão já existe — apenas reconecta para renovar o QR
        if (settings.BotSessionId.HasValue)
        {
            var sessionId = settings.BotSessionId.Value.ToString();
            var ok = await ConnectSessionAsync(httpClient, sessionId, ct);
            if (!ok)
                return ServiceResult<WhatsAppSessionResponse>.Fail("Falha ao reconectar sessão existente no bot.");

            return ServiceResult<WhatsAppSessionResponse>.Success(
                new WhatsAppSessionResponse(sessionId, "connecting", null));
        }

        // Cria nova sessão no bot
        var sessionName   = $"tenant-{tenantId.Value}";
        var webhookUrl    = $"{_options.WebhookBackendUrl}/api/v1/webhooks/whatsapp/{tenantId.Value}";
        var createPayload = new
        {
            session_name          = sessionName,
            webhook_url           = webhookUrl,
            webhook_signature_key = _options.WebhookSignatureKey
        };

        HttpResponseMessage createResponse;
        try
        {
            createResponse = await httpClient.PostAsJsonAsync($"{_options.BotUrl}/api/v1/sessions", createPayload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar sessão no bot. TenantId={TenantId}", tenantId.Value);
            return ServiceResult<WhatsAppSessionResponse>.Fail("Erro de comunicação com o bot.");
        }

        if (!createResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Bot recusou a criação da sessão. Status={Status}, TenantId={TenantId}",
                (int)createResponse.StatusCode, tenantId.Value);
            return ServiceResult<WhatsAppSessionResponse>.Fail("Bot recusou a criação da sessão.");
        }

        var createBody   = await createResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var newSessionId = createBody.GetProperty("data").GetProperty("session_id").GetString()
                          ?? throw new InvalidOperationException("Bot não retornou session_id.");

        // Persiste o BotSessionId antes de conectar (evita sessão órfã)
        settings.BotSessionId = Guid.Parse(newSessionId);
        await _settingsRepo.UpdateAsync(settings, ct);

        var connected = await ConnectSessionAsync(httpClient, newSessionId, ct);
        if (!connected)
            return ServiceResult<WhatsAppSessionResponse>.Fail("Sessão criada mas falha ao iniciar conexão QR.");

        _logger.LogInformation(
            "Sessão WhatsApp criada e conectando. TenantId={TenantId}, SessionId={SessionId}",
            tenantId.Value, newSessionId);

        return ServiceResult<WhatsAppSessionResponse>.Success(
            new WhatsAppSessionResponse(newSessionId, "connecting", null));
    }

    public async Task<ServiceResult<WhatsAppSessionStatusResponse>> GetStatusAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUrl))
            return ServiceResult<WhatsAppSessionStatusResponse>.Fail("Bot não configurado (BotUrl ausente).");

        var settings = await _settingsRepo.GetAsync(ct);
        if (settings is null)
            return ServiceResult<WhatsAppSessionStatusResponse>.Fail("Configurações do tenant não encontradas.");

        if (!settings.BotSessionId.HasValue)
            return ServiceResult<WhatsAppSessionStatusResponse>.Success(
                new WhatsAppSessionStatusResponse("not_configured", false, null));

        var httpClient = CreateBotClient();
        var sessionId  = settings.BotSessionId.Value.ToString();

        HttpResponseMessage statusResponse;
        try
        {
            statusResponse = await httpClient.GetAsync($"{_options.BotUrl}/api/v1/sessions/{sessionId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar status da sessão. SessionId={SessionId}", sessionId);
            return ServiceResult<WhatsAppSessionStatusResponse>.Fail("Erro de comunicação com o bot.");
        }

        if (!statusResponse.IsSuccessStatusCode)
            return ServiceResult<WhatsAppSessionStatusResponse>.Fail("Falha ao consultar sessão no bot.");

        var statusBody  = await statusResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var botStatus   = statusBody.GetProperty("data").GetProperty("status").GetString() ?? "unknown";
        var isConnected = botStatus == "connected";

        // Busca QR code quando ainda não conectado
        string? qrCode = null;
        if (!isConnected)
        {
            try
            {
                var qrResponse = await httpClient.GetAsync($"{_options.BotUrl}/api/v1/sessions/{sessionId}/qr", ct);
                if (qrResponse.IsSuccessStatusCode)
                {
                    var qrBody   = await qrResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
                    var qrStatus = qrBody.GetProperty("status").GetString();
                    if (qrStatus == "success")
                        qrCode = qrBody.GetProperty("data").GetProperty("qr_code").GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar QR code. SessionId={SessionId}", sessionId);
            }
        }

        return ServiceResult<WhatsAppSessionStatusResponse>.Success(
            new WhatsAppSessionStatusResponse(botStatus, isConnected, qrCode));
    }

    public async Task<ServiceResult<bool>> ReconnectAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUrl))
            return ServiceResult<bool>.Fail("Bot não configurado (BotUrl ausente).");

        var settings = await _settingsRepo.GetAsync(ct);
        if (settings?.BotSessionId is null)
            return ServiceResult<bool>.Fail("Nenhuma sessão WhatsApp configurada para este tenant.");

        var httpClient = CreateBotClient();
        var sessionId  = settings.BotSessionId.Value.ToString();

        try
        {
            var response = await httpClient.PostAsync(
                $"{_options.BotUrl}/api/v1/sessions/{sessionId}/reconnect",
                content: null,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Bot recusou reconexão. Status={Status}, SessionId={SessionId}",
                    (int)response.StatusCode, sessionId);
                return ServiceResult<bool>.Fail("Bot recusou a reconexão.");
            }

            _logger.LogInformation("Reconexão manual iniciada. SessionId={SessionId}", sessionId);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar reconexão no bot. SessionId={SessionId}", sessionId);
            return ServiceResult<bool>.Fail("Erro de comunicação com o bot.");
        }
    }

    public async Task<ServiceResult<WhatsAppSessionStatsResponse>> GetStatsAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUrl))
            return ServiceResult<WhatsAppSessionStatsResponse>.Fail("Bot não configurado (BotUrl ausente).");

        var settings = await _settingsRepo.GetAsync(ct);
        if (settings?.BotSessionId is null)
            return ServiceResult<WhatsAppSessionStatsResponse>.Success(
                new WhatsAppSessionStatsResponse("", false, 0, 0, 0, 0, 0, null, null));

        var httpClient = CreateBotClient();
        var sessionId  = settings.BotSessionId.Value.ToString();

        try
        {
            var response = await httpClient.GetAsync(
                $"{_options.BotUrl}/api/v1/sessions/{sessionId}/stats", ct);

            if (!response.IsSuccessStatusCode)
                return ServiceResult<WhatsAppSessionStatsResponse>.Fail("Falha ao consultar stats no bot.");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var d    = body.GetProperty("data");

            var stats = new WhatsAppSessionStatsResponse(
                SessionId:           d.GetProperty("session_id").GetString() ?? sessionId,
                IsActive:            d.GetProperty("is_active").GetBoolean(),
                MessagesReceived:    d.GetProperty("messages_received").GetInt32(),
                MessagesSent:        d.GetProperty("messages_sent").GetInt32(),
                WebhookErrors:       d.GetProperty("webhook_errors").GetInt32(),
                CircuitBreakerTrips: d.GetProperty("circuit_breaker_trips").GetInt32(),
                ReconnectCount:      d.GetProperty("reconnect_count").GetInt32(),
                ConnectedAt:         d.GetProperty("connected_at").ValueKind == JsonValueKind.Null
                                        ? null
                                        : d.GetProperty("connected_at").GetString(),
                UptimeSeconds:       d.GetProperty("uptime_seconds").ValueKind == JsonValueKind.Null
                                        ? null
                                        : d.GetProperty("uptime_seconds").GetInt32()
            );

            return ServiceResult<WhatsAppSessionStatsResponse>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar stats no bot. SessionId={SessionId}", sessionId);
            return ServiceResult<WhatsAppSessionStatsResponse>.Fail("Erro de comunicação com o bot.");
        }
    }

    private HttpClient CreateBotClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", _options.BotApiKey);
        return client;
    }

    private async Task<bool> ConnectSessionAsync(HttpClient httpClient, string sessionId, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.PostAsync(
                $"{_options.BotUrl}/api/v1/sessions/{sessionId}/connect",
                content: null,
                ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar sessão. SessionId={SessionId}", sessionId);
            return false;
        }
    }
}
