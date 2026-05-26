using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface IServiceCatalogRepository
{
    Task<IReadOnlyList<Service>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetAllActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct = default);
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Service> CreateAsync(Service service, CancellationToken ct = default);
    Task UpdateAsync(Service service, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
