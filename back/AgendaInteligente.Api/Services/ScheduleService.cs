using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.CalendarSync;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class ScheduleService : IScheduleService
{
    // Horário comercial assumido para busca de alternativas (UTC offset 0; timezone é responsabilidade da UI)
    private const int WorkDayStartHour = 8;
    private const int WorkDayEndHour   = 18;

    private readonly IScheduleRepository       _scheduleRepo;
    private readonly IServiceCatalogRepository _serviceRepo;
    private readonly ICalendarSyncQueue        _syncQueue;
    private readonly IWaitlistService          _waitlistService;
    private readonly ILogger<ScheduleService>  _logger;

    public ScheduleService(
        IScheduleRepository scheduleRepo,
        IServiceCatalogRepository serviceRepo,
        ICalendarSyncQueue syncQueue,
        IWaitlistService waitlistService,
        ILogger<ScheduleService> logger)
    {
        _scheduleRepo    = scheduleRepo;
        _serviceRepo     = serviceRepo;
        _syncQueue       = syncQueue;
        _waitlistService = waitlistService;
        _logger          = logger;
    }

    public Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => _scheduleRepo.GetByDateRangeAsync(from, to, ct);

    public Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default)
        => _scheduleRepo.GetByProfessionalAsync(professionalId, from, to, ct);

    public Task<IReadOnlyList<Schedule>> GetBlockoutsByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default)
        => _scheduleRepo.GetBlockoutsByProfessionalAsync(professionalId, from, to, ct);

    public Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _scheduleRepo.GetByIdAsync(id, ct);

    /// <summary>
    /// Cria um agendamento após:
    ///   1. Resolver o serviço para obter a duração.
    ///   2. Calcular EndDateTime = StartDateTime + DurationMinutes.
    ///   3. Verificar conflito de horário para o profissional.
    ///
    /// Se houver conflito, lança <see cref="ScheduleConflictException"/> contendo
    /// a lista de horários alternativos disponíveis próximos ao slot solicitado.
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
            var isBlockout = conflicts.Any(c => c.IsBlocked);
            var reason = isBlockout
                ? (conflicts.First(c => c.IsBlocked).BlockReason ?? "uma folga/bloqueio")
                : "um agendamento";

            _logger.LogWarning(
                "Conflito de horário detectado para o profissional '{ProfessionalId}' em {Start}-{End}. " +
                "Motivo: {Reason}. Agendamento(s) conflitante(s): {ConflictIds}",
                professionalId, startDateTime, endDateTime, reason,
                string.Join(", ", conflicts.Select(c => c.Id)));

            // Busca alternativas próximas para oferecer ao cliente
            var alternatives = await GetAlternativeTimesAsync(
                professionalId, serviceId, startDateTime, count: 3, maxSearchDays: 7, ct);

            throw new ScheduleConflictException(
                $"O profissional já possui {reason} neste horário " +
                $"({startDateTime:dd/MM/yyyy HH:mm} – {endDateTime:HH:mm}). " +
                $"Por favor, escolha outro horário.",
                alternatives);
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

        var service = await _serviceRepo.GetByIdAsync(schedule.ServiceId!.Value, ct)
            ?? throw new KeyNotFoundException($"Serviço do agendamento não encontrado.");

        var endDateTime = startDateTime.AddMinutes(service.DurationMinutes);

        // Verifica conflito excluindo o próprio agendamento da verificação
        var conflicts = await _scheduleRepo.GetConflictingAsync(schedule.ProfessionalId, startDateTime, endDateTime, ct);
        var realConflicts = conflicts.Where(c => c.Id != id).ToList();
        if (realConflicts.Count > 0)
        {
            var isBlockout = realConflicts.Any(c => c.IsBlocked);
            var reason = isBlockout
                ? (realConflicts.First(c => c.IsBlocked).BlockReason ?? "uma folga/bloqueio")
                : "outro agendamento existente";

            throw new InvalidOperationException(
                $"O novo horário conflita com {reason} " +
                $"({startDateTime:dd/MM/yyyy HH:mm} – {endDateTime:HH:mm}).");
        }

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

    public async Task<Schedule> CreateBlockoutAsync(
        Guid professionalId, DateTime startDateTime, DateTime endDateTime,
        string? blockReason, bool isAllDay, CancellationToken ct = default)
    {
        if (startDateTime.Kind != DateTimeKind.Utc)
            startDateTime = startDateTime.ToUniversalTime();

        if (endDateTime.Kind != DateTimeKind.Utc)
            endDateTime = endDateTime.ToUniversalTime();

        if (endDateTime <= startDateTime)
            throw new ArgumentException("A data de término deve ser posterior à data de início.");

        var conflicts = await _scheduleRepo.GetConflictingAsync(professionalId, startDateTime, endDateTime, ct);
        if (conflicts.Count > 0)
        {
            throw new InvalidOperationException(
                "Não é possível criar uma folga neste horário pois já existem agendamentos ou folgas registrados.");
        }

        var blockout = new Schedule
        {
            ProfessionalId = professionalId,
            StartDateTime  = startDateTime,
            EndDateTime    = endDateTime,
            Status         = ScheduleStatus.Confirmed,
            IsBlocked      = true,
            BlockReason    = blockReason,
            IsAllDay       = isAllDay
        };

        var created = await _scheduleRepo.CreateAsync(blockout, ct);

        await _syncQueue.EnqueueAsync(
            new CalendarSyncMessage(created.Id, created.TenantId, CalendarSyncOperation.Upsert), ct);

        return created;
    }

    public async Task<Schedule> UpdateBlockoutAsync(
        Guid id, DateTime startDateTime, DateTime endDateTime,
        string? blockReason, bool isAllDay, CancellationToken ct = default)
    {
        if (startDateTime.Kind != DateTimeKind.Utc)
            startDateTime = startDateTime.ToUniversalTime();

        if (endDateTime.Kind != DateTimeKind.Utc)
            endDateTime = endDateTime.ToUniversalTime();

        var blockout = await _scheduleRepo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Bloqueio '{id}' não encontrado.");

        if (!blockout.IsBlocked)
            throw new InvalidOperationException("O registro informado não é um bloqueio (folga).");

        var conflicts = await _scheduleRepo.GetConflictingAsync(blockout.ProfessionalId, startDateTime, endDateTime, ct);
        var realConflicts = conflicts.Where(c => c.Id != id).ToList();
        if (realConflicts.Count > 0)
        {
            throw new InvalidOperationException(
                "Não é possível alterar para este horário pois há conflito com outros agendamentos/folgas.");
        }

        blockout.StartDateTime = startDateTime;
        blockout.EndDateTime   = endDateTime;
        blockout.BlockReason   = blockReason;
        blockout.IsAllDay      = isAllDay;

        await _scheduleRepo.UpdateAsync(blockout, ct);

        await _syncQueue.EnqueueAsync(
            new CalendarSyncMessage(blockout.Id, blockout.TenantId, CalendarSyncOperation.Upsert), ct);

        return blockout;
    }

    public async Task<bool> UpdateStatusAsync(
        Guid id, ScheduleStatus status, CancellationToken ct = default)
    {
        _logger.LogInformation("Atualizando status do agendamento '{Id}' para {Status}.", id, status);

        // Busca antes de atualizar para capturar dados do slot caso precise acionar a waitlist
        var schedule = status == ScheduleStatus.Cancelled
            ? await _scheduleRepo.GetByIdAsync(id, ct)
            : null;

        var updated = await _scheduleRepo.UpdateStatusAsync(id, status, ct);

        // Se o agendamento foi cancelado, aciona a lista de espera de forma resiliente
        if (updated && status == ScheduleStatus.Cancelled && schedule is not null)
        {
            try
            {
                await _waitlistService.ProcessCancellationAsync(
                    schedule.ProfessionalId,
                    schedule.StartDateTime,
                    schedule.EndDateTime,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao processar lista de espera após cancelamento do agendamento '{Id}'. " +
                    "O cancelamento foi concluído.", id);
            }
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Excluindo agendamento '{Id}'.", id);

        // Busca antes de deletar para capturar GoogleCalendarEventId e dados do slot para a waitlist
        var schedule      = await _scheduleRepo.GetByIdAsync(id, ct);
        var googleEventId = schedule?.GoogleCalendarEventId;
        var tenantId      = schedule?.TenantId ?? Guid.Empty;

        var deleted = await _scheduleRepo.DeleteAsync(id, ct);

        if (deleted)
        {
            // Enfileira a remoção no Google Calendar (se houver evento sincronizado)
            if (!string.IsNullOrWhiteSpace(googleEventId))
            {
                await _syncQueue.EnqueueAsync(
                    new CalendarSyncMessage(id, tenantId, CalendarSyncOperation.Delete, googleEventId), ct);
            }

            // Aciona a lista de espera de forma resiliente — falha aqui não desfaz o delete
            if (schedule is not null)
            {
                try
                {
                    await _waitlistService.ProcessCancellationAsync(
                        schedule.ProfessionalId,
                        schedule.StartDateTime,
                        schedule.EndDateTime,
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Falha ao processar lista de espera após exclusão do agendamento '{Id}'. " +
                        "A exclusão foi concluída.", id);
                }
            }
        }

        return deleted;
    }

    /// <summary>
    /// Busca os <paramref name="count"/> horários disponíveis mais próximos ao slot solicitado,
    /// varrendo o mesmo dia e (se necessário) os dias seguintes dentro da janela de <paramref name="maxSearchDays"/> dias.
    ///
    /// Algoritmo:
    ///   1. Para cada dia da janela de busca (D, D+1, ..., D+N), gera candidatos de hora em hora
    ///      dentro do horário comercial (08:00–18:00 UTC).
    ///   2. Para cada candidato, verifica se o slot [candidato, candidato+duração] está livre.
    ///   3. Candidatos do mesmo dia são ordenados por proximidade ao horário solicitado;
    ///      os de dias futuros são ordenados cronologicamente.
    ///   4. Coleta até atingir <paramref name="count"/> alternativas.
    /// </summary>
    public async Task<IReadOnlyList<DateTime>> GetAlternativeTimesAsync(
        Guid professionalId,
        Guid serviceId,
        DateTime requestedTime,
        int count        = 3,
        int maxSearchDays = 7,
        CancellationToken ct = default)
    {
        if (requestedTime.Kind != DateTimeKind.Utc)
            requestedTime = requestedTime.ToUniversalTime();

        var service = await _serviceRepo.GetByIdAsync(serviceId, ct)
            ?? throw new KeyNotFoundException($"Serviço '{serviceId}' não encontrado ou inativo.");

        var durationMinutes = service.DurationMinutes;
        var alternatives    = new List<DateTime>();
        var requestedDate   = requestedTime.Date;

        for (var dayOffset = 0; dayOffset <= maxSearchDays && alternatives.Count < count; dayOffset++)
        {
            var targetDate = requestedDate.AddDays(dayOffset);

            // Gera todos os candidatos do dia dentro do horário comercial
            var candidates = GenerateDayCandidates(targetDate, durationMinutes);

            // Para o dia solicitado: ordena por proximidade ao horário pedido (antes e depois)
            // Para dias futuros: mantém ordem cronológica (mais cedo primeiro)
            var orderedCandidates = dayOffset == 0
                ? candidates.OrderBy(c => Math.Abs((c - requestedTime).TotalMinutes))
                : candidates.AsEnumerable();

            foreach (var candidate in orderedCandidates)
            {
                if (alternatives.Count >= count) break;

                // Não sugerimos horários no passado
                if (candidate < DateTime.UtcNow) continue;

                var candidateEnd = candidate.AddMinutes(durationMinutes);
                var conflicts    = await _scheduleRepo.GetConflictingAsync(professionalId, candidate, candidateEnd, ct);

                if (conflicts.Count == 0)
                    alternatives.Add(candidate);
            }
        }

        _logger.LogInformation(
            "Busca de alternativas para profissional '{ProfessionalId}' em {RequestedTime}: " +
            "{FoundCount} alternativa(s) encontrada(s) em janela de {MaxDays} dia(s).",
            professionalId, requestedTime, alternatives.Count, maxSearchDays);

        return alternatives.AsReadOnly();
    }

    // ── Helpers privados ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gera todos os candidatos de horário para um dado dia, de WorkDayStartHour até
    /// WorkDayEndHour − duração do serviço, com granularidade de 30 minutos.
    /// </summary>
    private static IEnumerable<DateTime> GenerateDayCandidates(DateTime date, int durationMinutes)
    {
        const int granularityMinutes = 30;

        var dayStart = DateTime.SpecifyKind(date.Date.AddHours(WorkDayStartHour), DateTimeKind.Utc);
        var dayEnd   = DateTime.SpecifyKind(date.Date.AddHours(WorkDayEndHour), DateTimeKind.Utc);

        // O slot candidato deve terminar antes ou exatamente no fim do horário comercial
        var lastPossibleStart = dayEnd.AddMinutes(-durationMinutes);

        for (var slot = dayStart; slot <= lastPossibleStart; slot = slot.AddMinutes(granularityMinutes))
            yield return slot;
    }
}

