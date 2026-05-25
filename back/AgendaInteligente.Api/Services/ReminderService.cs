using System.Text.Json;
using AgendaInteligente.Api.Contracts.Reminders;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class ReminderService : IReminderService
{
    private static readonly DistributedCacheEntryOptions SentTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48) };

    private static readonly DistributedCacheEntryOptions ConfirmTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4) };

    private readonly ITenantSettingsRepository _settingsRepo;
    private readonly IScheduleRepository       _scheduleRepo;
    private readonly IWhatsAppSendService      _sendService;
    private readonly IDistributedCache         _cache;
    private readonly TimeProvider              _time;
    private readonly ILogger<ReminderService>  _logger;

    public ReminderService(
        ITenantSettingsRepository settingsRepo,
        IScheduleRepository scheduleRepo,
        IWhatsAppSendService sendService,
        IDistributedCache cache,
        ILogger<ReminderService> logger,
        TimeProvider? timeProvider = null)
    {
        _settingsRepo = settingsRepo;
        _scheduleRepo = scheduleRepo;
        _sendService  = sendService;
        _cache        = cache;
        _time         = timeProvider ?? TimeProvider.System;
        _logger       = logger;
    }

    public async Task ProcessRemindersAsync(CancellationToken ct = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        // Horário de silêncio: 22h–07h UTC
        if (now.Hour < 7 || now.Hour >= 22)
        {
            _logger.LogDebug("Lembretes ignorados: horário de silêncio ({Hour}h UTC).", now.Hour);
            return;
        }

        var allSettings = await _settingsRepo.GetAllWithReminderEnabledAsync(ct);

        if (allSettings.Count == 0)
        {
            _logger.LogDebug("Nenhum tenant com lembretes habilitados.");
            return;
        }

        _logger.LogInformation("Processando lembretes para {Count} tenant(s).", allSettings.Count);

        foreach (var settings in allSettings)
        {
            try
            {
                await ProcessTenantAsync(settings.TenantId, settings.ReminderLeadTimeHours, now, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lembretes do TenantId={TenantId}.", settings.TenantId);
            }
        }
    }

    // ── Privados ──────────────────────────────────────────────────────────────────

    private async Task ProcessTenantAsync(
        Guid tenantId, int leadTimeHours, DateTime now, CancellationToken ct)
    {
        var from = now.AddHours(leadTimeHours).AddMinutes(-30);
        var to   = now.AddHours(leadTimeHours).AddMinutes(30);

        var schedules = await _scheduleRepo.GetUpcomingForReminderAsync(tenantId, from, to, ct);
        if (schedules.Count == 0) return;

        _logger.LogInformation(
            "TenantId={TenantId}: {Count} agendamento(s) na janela de lembrete.",
            tenantId, schedules.Count);

        var settings = await _settingsRepo.GetByTenantIdAsync(tenantId, ct);
        var tz       = GetTenantTimeZone(settings);

        foreach (var schedule in schedules)
            await TrySendAsync(tenantId, schedule, tz, ct);
    }

    private async Task TrySendAsync(Guid tenantId, Schedule schedule, TimeZoneInfo tz, CancellationToken ct)
    {
        var phone = schedule.Customer?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phone))
        {
            _logger.LogWarning("Lembrete ignorado: Customer sem telefone. ScheduleId={Id}", schedule.Id);
            return;
        }

        // Dedup: já enviado nas últimas 48h?
        var sentKey = $"reminder:sent:{schedule.Id}";
        if (await _cache.GetStringAsync(sentKey, ct) is not null)
        {
            _logger.LogDebug("Lembrete já enviado. ScheduleId={Id}", schedule.Id);
            return;
        }

        var customerName     = schedule.Customer!.Name ?? phone;
        var serviceName      = schedule.Service?.Name      ?? "serviço";
        var professionalName = schedule.Professional?.Name ?? "profissional";
        var startLocal       = TimeZoneInfo.ConvertTimeFromUtc(schedule.StartDateTime, tz);

        var message = BuildMessage(customerName, serviceName, professionalName, startLocal);
        var sent    = await _sendService.SendTextMessageAsync(tenantId, phone, message, ct);

        if (!sent)
        {
            _logger.LogWarning(
                "Falha ao enviar lembrete. TenantId={TenantId}, ScheduleId={Id}", tenantId, schedule.Id);
            return;
        }

        await _cache.SetStringAsync(sentKey, "1", SentTtl, ct);

        var state      = new PendingReminderState(schedule.Id, tenantId, phone, startLocal, serviceName, professionalName);
        var confirmKey = $"reminder:confirm:{tenantId}:{phone}";
        await _cache.SetStringAsync(confirmKey, JsonSerializer.Serialize(state), ConfirmTtl, ct);

        _logger.LogInformation(
            "Lembrete enviado. TenantId={TenantId}, ScheduleId={Id}, Phone={Phone}",
            tenantId, schedule.Id, phone);
    }

    internal static string BuildMessage(
        string customerName, string serviceName, string professionalName, DateTime start)
        => $"Ola, {customerName}!\n\n" +
           $"Lembrete: voce tem um agendamento de {serviceName} com {professionalName} " +
           $"em {start:dd/MM/yyyy} as {start:HH:mm}.\n\n" +
           "Confirme sua presenca:\n" +
           "1 - Confirmar\n" +
           "2 - Remarcar\n" +
           "3 - Cancelar";

    private static TimeZoneInfo GetTenantTimeZone(TenantSettings? settings)
    {
        var id = settings?.TimeZoneId;
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
