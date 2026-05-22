using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface IScheduleRepository
{
    Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetBlockoutsByProfessionalAsync(
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

    /// <summary>
    /// Retorna os agendamentos futuros pendentes de um cliente, ordenados por data de início.
    /// Usado pelo bot para localizar o próximo agendamento quando o cliente solicita cancelamento.
    /// </summary>
    Task<IReadOnlyList<Schedule>> GetUpcomingByCustomerIdAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Retorna agendamentos de um tenant específico (sem filtro global) na janela de tempo informada,
    /// com Customer/Service/Professional incluídos. Usado pelo ReminderBackgroundService.
    /// </summary>
    Task<IReadOnlyList<Schedule>> GetUpcomingForReminderAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Retorna todos os agendamentos de um cliente, ordenados por data decrescente.
    /// Usado pelo dashboard para exibir o histórico de visitas.
    /// </summary>
    Task<IReadOnlyList<Schedule>> GetAllByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
}
