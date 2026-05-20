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
        CancellationToken ct = default);

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
    /// <param name="professionalId">Profissional cujos slots serão avaliados.</param>
    /// <param name="serviceId">Serviço cujo DurationMinutes determina o tamanho do slot necessário.</param>
    /// <param name="requestedTime">Data/hora solicitada pelo cliente (ponto de referência da busca).</param>
    /// <param name="count">Quantidade máxima de alternativas a retornar. Padrão: 3.</param>
    /// <param name="maxSearchDays">Janela de busca em dias (D+N) se o dia solicitado estiver lotado. Padrão: 7.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<DateTime>> GetAlternativeTimesAsync(
        Guid professionalId,
        Guid serviceId,
        DateTime requestedTime,
        int count = 3,
        int maxSearchDays = 7,
        CancellationToken ct = default);
}
