using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Repositories.Interfaces;

/// <summary>
/// Contrato de acesso a dados para a entidade <see cref="Waitlist"/>.
/// O isolamento Multi-Tenant é garantido pelo Global Query Filter do AppDbContext.
/// </summary>
public interface IWaitlistRepository
{
    /// <summary>
    /// Adiciona uma nova entrada na lista de espera.
    /// </summary>
    Task<Waitlist> AddAsync(Waitlist entry, CancellationToken ct = default);

    /// <summary>
    /// Retorna todas as entradas pendentes (Status == Waiting) para uma data desejada específica,
    /// opcionalmente filtradas por profissional. Usada pelo trigger de cancelamento.
    /// </summary>
    Task<IReadOnlyList<Waitlist>> GetPendingByDateAsync(
        DateOnly desiredDate,
        Guid? professionalId,
        CancellationToken ct = default);

    /// <summary>
    /// Persiste alterações em uma entrada existente da lista de espera (ex: Status → Notified).
    /// </summary>
    Task UpdateAsync(Waitlist entry, CancellationToken ct = default);
}
