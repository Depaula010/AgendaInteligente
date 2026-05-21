using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class OnboardingRepository : IOnboardingRepository
{
    private readonly AppDbContext _db;

    public OnboardingRepository(AppDbContext db) => _db = db;

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => _db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task<(Tenant Tenant, Professional Professional, TenantSettings Settings)> CreateOnboardingAsync(
        Tenant tenant,
        Professional professional,
        TenantSettings settings,
        CancellationToken ct = default)
    {
        // EF Core encapsula um único SaveChangesAsync em transação implícita —
        // os três INSERTs são atômicos sem abrir uma transação explícita.
        _db.Tenants.Add(tenant);
        _db.Professionals.Add(professional);
        _db.TenantSettings.Add(settings);

        await _db.SaveChangesAsync(ct);

        return (tenant, professional, settings);
    }
}
