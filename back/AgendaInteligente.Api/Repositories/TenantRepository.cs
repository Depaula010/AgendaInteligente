using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _db;

    public TenantRepository(AppDbContext db) => _db = db;

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => _db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }
}
