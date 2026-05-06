using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface IScheduleRepository
{
    Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default);

    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retorna todos os schedules cujo intervalo [StartDateTime, EndDateTime] se sobrepõe
    /// ao intervalo fornecido para o profissional dado. Usado para detecção de conflito.
    /// </summary>
    Task<IReadOnlyList<Schedule>> GetConflictingAsync(
        Guid professionalId, DateTime start, DateTime end, CancellationToken ct = default);

    Task<Schedule> CreateAsync(Schedule schedule, CancellationToken ct = default);
    Task UpdateAsync(Schedule schedule, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, ScheduleStatus status, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Persiste o <c>GoogleCalendarEventId</c> no banco após a sincronização bem-sucedida.
    /// </summary>
    Task<bool> UpdateGoogleEventIdAsync(Guid scheduleId, string googleEventId, CancellationToken ct = default);
}
