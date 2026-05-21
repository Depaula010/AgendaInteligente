using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default);

    /// <summary>
    /// Lookup por telefone com tenantId explícito — usa IgnoreQueryFilters para funcionar
    /// em contextos sem JWT (ex: caminho do webhook).
    /// </summary>
    Task<Customer?> GetByPhoneAndTenantAsync(string phone, Guid tenantId, CancellationToken ct = default);

    Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default);
}
