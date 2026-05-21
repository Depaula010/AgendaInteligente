using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface IOnboardingRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Persiste Tenant, Professional (Owner) e TenantSettings em um único SaveChangesAsync —
    /// atomicamente, aproveitando a transação implícita do EF Core por unidade de trabalho.
    /// </summary>
    Task<(Tenant Tenant, Professional Professional, TenantSettings Settings)> CreateOnboardingAsync(
        Tenant tenant,
        Professional professional,
        TenantSettings settings,
        CancellationToken ct = default);
}
