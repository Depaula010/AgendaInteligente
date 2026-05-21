using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class ScheduleRepository : IScheduleRepository
{
    private readonly AppDbContext _db;

    public ScheduleRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => _db.Schedules
              .Include(s => s.Professional)
              .Include(s => s.Customer)
              .Include(s => s.Service)
              .Where(s => !s.IsBlocked && s.StartDateTime >= from && s.StartDateTime < to)
              .OrderBy(s => s.StartDateTime)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Schedule>)t.Result, ct);

    public Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default)
        => _db.Schedules
              .Include(s => s.Customer)
              .Include(s => s.Service)
              .Where(s => !s.IsBlocked
                       && s.ProfessionalId == professionalId
                       && s.StartDateTime >= from
                       && s.StartDateTime < to)
              .OrderBy(s => s.StartDateTime)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Schedule>)t.Result, ct);

    public Task<IReadOnlyList<Schedule>> GetBlockoutsByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default)
        => _db.Schedules
              .Where(s => s.IsBlocked
                       && s.ProfessionalId == professionalId
                       && s.StartDateTime >= from
                       && s.StartDateTime < to)
              .OrderBy(s => s.StartDateTime)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Schedule>)t.Result, ct);

    public Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Schedules
              .Include(s => s.Professional)
              .Include(s => s.Customer)
              .Include(s => s.Service)
              .FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>
    /// Retorna agendamentos que se sobrepõem ao intervalo [start, end) para o profissional.
    /// Regra de sobreposição: existingStart &lt; end AND existingEnd &gt; start.
    /// Exclui agendamentos cancelados da verificação de conflito.
    /// </summary>
    public Task<IReadOnlyList<Schedule>> GetConflictingAsync(
        Guid professionalId, DateTime start, DateTime end, CancellationToken ct = default)
        => _db.Schedules
              .Where(s => s.ProfessionalId == professionalId
                       && s.Status != ScheduleStatus.Cancelled
                       && s.StartDateTime < end
                       && s.EndDateTime > start)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Schedule>)t.Result, ct);

    public async Task<Schedule> CreateAsync(Schedule schedule, CancellationToken ct = default)
    {
        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task UpdateAsync(Schedule schedule, CancellationToken ct = default)
    {
        schedule.UpdatedAt = DateTime.UtcNow;
        _db.Schedules.Update(schedule);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, ScheduleStatus status, CancellationToken ct = default)
    {
        var rows = await _db.Schedules
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, status)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await _db.Schedules
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public Task<IReadOnlyList<Schedule>> GetUpcomingByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default)
        => _db.Schedules
              .Include(s => s.Service)
              .Include(s => s.Professional)
              .Where(s => s.CustomerId == customerId
                       && s.StartDateTime >= DateTime.UtcNow
                       && (s.Status == ScheduleStatus.Pending || s.Status == ScheduleStatus.Confirmed)
                       && !s.IsBlocked)
              .OrderBy(s => s.StartDateTime)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Schedule>)t.Result, ct);

    public Task<IReadOnlyList<Schedule>> GetUpcomingForReminderAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => _db.Schedules
              .IgnoreQueryFilters()
              .Include(s => s.Customer)
              .Include(s => s.Service)
              .Include(s => s.Professional)
              .Where(s => s.TenantId == tenantId
                       && !s.IsBlocked
                       && (s.Status == ScheduleStatus.Pending || s.Status == ScheduleStatus.Confirmed)
                       && s.StartDateTime >= from
                       && s.StartDateTime < to)
              .OrderBy(s => s.StartDateTime)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Schedule>)t.Result, ct);

    public async Task<bool> UpdateGoogleEventIdAsync(
        Guid scheduleId, string googleEventId, CancellationToken ct = default)
    {
        var rows = await _db.Schedules
            .Where(s => s.Id == scheduleId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.GoogleCalendarEventId, googleEventId)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        return rows > 0;
    }
}
