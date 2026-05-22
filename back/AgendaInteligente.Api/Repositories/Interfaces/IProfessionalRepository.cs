using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface IProfessionalRepository
{
    Task<IReadOnlyList<Professional>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Professional>> GetAllAsync(CancellationToken ct = default);
    Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Professional?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Professional?> GetByEmailIgnoringQueryFilterAsync(string email, CancellationToken ct = default);
    Task<Professional> CreateAsync(Professional professional, CancellationToken ct = default);
    Task UpdateAsync(Professional professional, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
