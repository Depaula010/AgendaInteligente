using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.CalendarSync;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IServiceCatalogRepository _serviceRepo;
    private readonly ICalendarSyncQueue _syncQueue;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IScheduleRepository scheduleRepo,
        IServiceCatalogRepository serviceRepo,
        ICalendarSyncQueue syncQueue,
        ILogger<ScheduleService> logger)
    {
        _scheduleRepo = scheduleRepo;
        _serviceRepo  = serviceRepo;
        _syncQueue    = syncQueue;
        _logger       = logger;
    }

    public Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => _scheduleRepo.GetByDateRangeAsync(from, to, ct);

    public Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default)
        => _scheduleRepo.GetByProfessionalAsync(professionalId, from, to, ct);

    public Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _scheduleRepo.GetByIdAsync(id, ct);

    /// <summary>
    /// Cria um agendamento após:
    ///   1. Resolver o serviço para obter a duração.
    ///   2. Calcular EndDateTime = StartDateTime + DurationMinutes.
    ///   3. Verificar conflito de horário para o profissional.
    /// </summary>
    public async Task<Schedule> CreateAsync(
        Guid customerId, Guid professionalId, Guid serviceId,
        DateTime startDateTime, string? notes = null,
        CancellationToken ct = default)
    {
        // 1. Garante que a data está em UTC
        if (startDateTime.Kind != DateTimeKind.Utc)
            startDateTime = startDateTime.ToUniversalTime();

        // 2. Resolve o serviço para obter a duração
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct)
            ?? throw new KeyNotFoundException($"Serviço '{serviceId}' não encontrado ou inativo.");

        var endDateTime = startDateTime.AddMinutes(service.DurationMinutes);

        // 3. Verifica conflito de horário (regra de negócio central)
        var conflicts = await _scheduleRepo.GetConflictingAsync(professionalId, startDateTime, endDateTime, ct);
        if (conflicts.Count > 0)
        {
            _logger.LogWarning(
                "Conflito de horário detectado para o profissional '{ProfessionalId}' em {Start}-{End}. " +
                "Agendamento(s) conflitante(s): {ConflictIds}",
                professionalId, startDateTime, endDateTime,
                string.Join(", ", conflicts.Select(c => c.Id)));

            throw new InvalidOperationException(
                $"O profissional já possui um agendamento neste horário " +
                $"({startDateTime:dd/MM/yyyy HH:mm} – {endDateTime:HH:mm}). " +
                $"Por favor, escolha outro horário.");
        }

        var schedule = new Schedule
        {
            CustomerId     = customerId,
            ProfessionalId = professionalId,
            ServiceId      = serviceId,
            StartDateTime  = startDateTime,
            EndDateTime    = endDateTime,
            Status         = ScheduleStatus.Pending,
            Notes          = notes
        };

        _logger.LogInformation(
            "Criando agendamento: Cliente={CustomerId}, Profissional={ProfessionalId}, " +
            "Serviço={ServiceId}, Início={Start}",
            customerId, professionalId, serviceId, startDateTime);

        var created = await _scheduleRepo.CreateAsync(schedule, ct);

        // Dispara sincronização assíncrona com o Google Calendar (fire-and-forget via fila)
        await _syncQueue.EnqueueAsync(
            new CalendarSyncMessage(created.Id, created.TenantId, CalendarSyncOperation.Upsert), ct);

        return created;
    }

    public async Task<Schedule> UpdateAsync(
        Guid id, DateTime startDateTime, string? notes,
        CancellationToken ct = default)
    {
        if (startDateTime.Kind != DateTimeKind.Utc)
            startDateTime = startDateTime.ToUniversalTime();

        var schedule = await _scheduleRepo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Agendamento '{id}' não encontrado.");

        var service = await _serviceRepo.GetByIdAsync(schedule.ServiceId, ct)
            ?? throw new KeyNotFoundException($"Serviço do agendamento não encontrado.");

        var endDateTime = startDateTime.AddMinutes(service.DurationMinutes);

        // Verifica conflito excluindo o próprio agendamento da verificação
        var conflicts = await _scheduleRepo.GetConflictingAsync(schedule.ProfessionalId, startDateTime, endDateTime, ct);
        var realConflicts = conflicts.Where(c => c.Id != id).ToList();
        if (realConflicts.Count > 0)
            throw new InvalidOperationException(
                $"O novo horário conflita com outro agendamento existente " +
                $"({startDateTime:dd/MM/yyyy HH:mm} – {endDateTime:HH:mm}).");

        schedule.StartDateTime = startDateTime;
        schedule.EndDateTime   = endDateTime;
        schedule.Notes         = notes;

        await _scheduleRepo.UpdateAsync(schedule, ct);
        _logger.LogInformation("Agendamento '{Id}' reagendado para {Start}.", id, startDateTime);

        // Dispara re-sincronização do evento no Google Calendar
        await _syncQueue.EnqueueAsync(
            new CalendarSyncMessage(schedule.Id, schedule.TenantId, CalendarSyncOperation.Upsert), ct);

        return schedule;
    }

    public Task<bool> UpdateStatusAsync(
        Guid id, ScheduleStatus status, CancellationToken ct = default)
    {
        _logger.LogInformation("Atualizando status do agendamento '{Id}' para {Status}.", id, status);
        return _scheduleRepo.UpdateStatusAsync(id, status, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Excluindo agendamento '{Id}'.", id);

        // Busca antes de deletar para capturar o GoogleCalendarEventId
        var schedule = await _scheduleRepo.GetByIdAsync(id, ct);
        var googleEventId = schedule?.GoogleCalendarEventId;
        var tenantId      = schedule?.TenantId ?? Guid.Empty;

        var deleted = await _scheduleRepo.DeleteAsync(id, ct);

        if (deleted && !string.IsNullOrWhiteSpace(googleEventId))
        {
            // Enfileira a remoção no Google Calendar
            await _syncQueue.EnqueueAsync(
                new CalendarSyncMessage(id, tenantId, CalendarSyncOperation.Delete, googleEventId), ct);
        }

        return deleted;
    }
}
