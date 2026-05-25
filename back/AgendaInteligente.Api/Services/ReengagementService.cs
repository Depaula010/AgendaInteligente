using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class ReengagementService : IReengagementService
{
    private readonly ITenantSettingsRepository  _settingsRepo;
    private readonly ICustomerRepository        _customerRepo;
    private readonly IWhatsAppSendService       _sendService;
    private readonly IDistributedCache          _cache;
    private readonly TimeProvider               _time;
    private readonly ILogger<ReengagementService> _logger;

    public ReengagementService(
        ITenantSettingsRepository settingsRepo,
        ICustomerRepository customerRepo,
        IWhatsAppSendService sendService,
        IDistributedCache cache,
        ILogger<ReengagementService> logger,
        TimeProvider? timeProvider = null)
    {
        _settingsRepo = settingsRepo;
        _customerRepo = customerRepo;
        _sendService  = sendService;
        _cache        = cache;
        _time         = timeProvider ?? TimeProvider.System;
        _logger       = logger;
    }

    public async Task ProcessAllTenantsAsync(CancellationToken ct = default)
    {
        var allSettings = await _settingsRepo.GetAllWithReengagementEnabledAsync(ct);

        if (allSettings.Count == 0)
        {
            _logger.LogDebug("Reengajamento: nenhum tenant com reengajamento habilitado.");
            return;
        }

        _logger.LogInformation("Reengajamento: processando {Count} tenant(s).", allSettings.Count);

        foreach (var settings in allSettings)
        {
            try
            {
                await ProcessTenantAsync(settings, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar reengajamento do TenantId={TenantId}.", settings.TenantId);
            }
        }
    }

    // ── Privados ──────────────────────────────────────────────────────────────────

    private async Task ProcessTenantAsync(TenantSettings settings, CancellationToken ct)
    {
        var customers = await _customerRepo.GetInactiveAsync(
            settings.TenantId, settings.ReengagementInactiveDays, ct);

        if (customers.Count == 0)
        {
            _logger.LogDebug("Reengajamento TenantId={TenantId}: nenhum cliente inativo.", settings.TenantId);
            return;
        }

        _logger.LogInformation(
            "Reengajamento TenantId={TenantId}: {Count} cliente(s) inativo(s) encontrado(s).",
            settings.TenantId, customers.Count);

        foreach (var customer in customers)
            await TrySendAsync(settings, customer, ct);
    }

    private async Task TrySendAsync(TenantSettings settings, Customer customer, CancellationToken ct)
    {
        var dedupKey = $"reeng:{settings.TenantId}:{customer.Id}";
        if (await _cache.GetStringAsync(dedupKey, ct) is not null)
        {
            _logger.LogDebug(
                "Reengajamento já enviado (dedup). TenantId={TenantId}, CustomerId={Id}",
                settings.TenantId, customer.Id);
            return;
        }

        var now      = _time.GetUtcNow().UtcDateTime;
        var lastSeen = customer.LastVisitAt ?? customer.CreatedAt;
        var days     = (int)(now - lastSeen).TotalDays;

        var message = BuildMessage(customer.Name, days);
        var sent    = await _sendService.SendTextMessageAsync(settings.TenantId, customer.PhoneNumber, message, ct);

        if (!sent)
        {
            _logger.LogWarning(
                "Falha ao enviar reengajamento. TenantId={TenantId}, CustomerId={Id}",
                settings.TenantId, customer.Id);
            return;
        }

        var ttl = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(settings.ReengagementInactiveDays)
        };
        await _cache.SetStringAsync(dedupKey, "1", ttl, ct);

        _logger.LogInformation(
            "Reengajamento enviado. TenantId={TenantId}, CustomerId={Id}, Phone={Phone}",
            settings.TenantId, customer.Id, customer.PhoneNumber);
    }

    internal static string BuildMessage(string customerName, int daysSince)
        => $"Ola, {customerName}! Faz {daysSince} dias que nao te vemos por aqui. " +
           "Que tal agendar um horario com a gente?";
}
