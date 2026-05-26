using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IScheduleService
{
    Task<IReadOnlyList<Schedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetBlockoutsByProfessionalAsync(
        Guid professionalId, DateTime from, DateTime to, CancellationToken ct = default);

    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cria um agendamento validando conflito de horários para o profissional.
    /// Lança InvalidOperationException se houver sobreposição.
    /// </summary>
    Task<Schedule> CreateAsync(
        Guid customerId, Guid professionalId, Guid serviceId,
        DateTime startDateTime, string? notes = null,
        Guid? tenantId = null, CancellationToken ct = default);

    Task<Schedule> UpdateAsync(
        Guid id, DateTime startDateTime, string? notes,
        CancellationToken ct = default);

    Task<Schedule> CreateBlockoutAsync(
        Guid professionalId, DateTime startDateTime, DateTime endDateTime,
        string? blockReason, bool isAllDay, CancellationToken ct = default);

    Task<Schedule> UpdateBlockoutAsync(
        Guid id, DateTime startDateTime, DateTime endDateTime,
        string? blockReason, bool isAllDay, CancellationToken ct = default);

    Task<bool> UpdateStatusAsync(
        Guid id, ScheduleStatus status, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Busca horários disponíveis próximos ao slot solicitado para um dado profissional/serviço.
    /// A busca considera a duração do serviço e varre a janela de dias configurada.
    /// Retorna os <paramref name="count"/> slots mais próximos (antes e depois) disponíveis.
    /// </summary>
    Task<IReadOnlyList<DateTime>> GetAlternativeTimesAsync(
        Guid professionalId,
        Guid serviceId,
        DateTime requestedTime,
        int count = 3,
        int maxSearchDays = 7,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna todos os slots livres do dia para um profissional e serviço.
    /// Respeita a duração do serviço e o horário comercial (08h–18h UTC).
    /// Usado pelo dashboard e pela IA para listar opções de horário.
    /// </summary>
    Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(
        Guid professionalId,
        Guid serviceId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Cria N agendamentos semanais consecutivos em uma única transação.
    /// Valida todos os conflitos antes de persistir qualquer registro.
    /// Lança <see cref="Domain.Exceptions.ScheduleConflictException"/> com as datas conflitantes se houver sobreposição.
    /// Limite: 52 ocorrências por série.
    /// </summary>
    Task<IReadOnlyList<Schedule>> CreateRecurringAsync(
        Guid customerId, Guid professionalId, Guid serviceId,
        DateTime firstStart, int repeatWeeklyCount, string? notes = null,
        CancellationToken ct = default);
}
