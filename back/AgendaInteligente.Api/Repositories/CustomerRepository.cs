using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phone, ct);

    public Task<Customer?> GetByPhoneAndTenantAsync(string phone, Guid tenantId, CancellationToken ct = default)
        => _db.Customers.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.PhoneNumber == phone && c.TenantId == tenantId, ct);

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return customer;
    }
}
