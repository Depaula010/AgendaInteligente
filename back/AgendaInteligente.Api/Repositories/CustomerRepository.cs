using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

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

    private IQueryable<Customer> ApplySearch(string? search) =>
        string.IsNullOrWhiteSpace(search)
            ? _db.Customers
            : _db.Customers.Where(c =>
                  EF.Functions.ILike(c.Name, $"%{search}%") ||
                  c.PhoneNumber.Contains(search));

    public async Task<IReadOnlyList<Customer>> GetPagedAsync(string? search, int skip, int take, CancellationToken ct = default)
    {
        var result = await ApplySearch(search)
            .OrderByDescending(c => c.LastVisitAt ?? c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return result;
    }

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
        => ApplySearch(search).CountAsync(ct);

    public async Task<IReadOnlyList<Customer>> GetInactiveAsync(Guid tenantId, int inactiveDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-inactiveDays);
        var result = await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId
                && (c.LastVisitAt != null ? c.LastVisitAt < cutoff : c.CreatedAt < cutoff))
            .ToListAsync(ct);
        return result;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync(ct);
    }
}
