using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IScheduleService
{
    Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default);

    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cria um agendamento validando conflito de horários para o profissional.
    /// Lança InvalidOperationException se houver sobreposição.
    /// </summary>
    Task<Schedule> CreateAsync(
        Guid customerId, Guid professionalId, Guid serviceId,
        DateTime startDateTime, string? notes = null,
        CancellationToken ct = default);

    Task<Schedule> UpdateAsync(
        Guid id, DateTime startDateTime, string? notes,
        CancellationToken ct = default);

    Task<bool> UpdateStatusAsync(
        Guid id, ScheduleStatus status, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
