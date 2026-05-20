using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default);
}
