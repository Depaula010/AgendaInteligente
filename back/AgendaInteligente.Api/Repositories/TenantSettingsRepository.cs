using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class TenantSettingsRepository : ITenantSettingsRepository
{
    private readonly AppDbContext _db;

    public TenantSettingsRepository(AppDbContext db) => _db = db;

    public Task<TenantSettings?> GetAsync(CancellationToken ct = default)
        => _db.TenantSettings.FirstOrDefaultAsync(ct);

    public Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => _db.TenantSettings.IgnoreQueryFilters()
                             .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    public async Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken ct = default)
    {
        _db.TenantSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task UpdateAsync(TenantSettings settings, CancellationToken ct = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _db.TenantSettings.Update(settings);
        await _db.SaveChangesAsync(ct);
    }
}
