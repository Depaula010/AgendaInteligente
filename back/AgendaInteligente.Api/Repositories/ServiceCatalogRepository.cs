using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class ServiceCatalogRepository : IServiceCatalogRepository
{
    private readonly AppDbContext _db;

    public ServiceCatalogRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Service>> GetAllActiveAsync(CancellationToken ct = default)
        => _db.Services
              .Where(s => s.IsActive)
              .OrderBy(s => s.Name)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Service>)t.Result, ct);

    public Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct = default)
        => _db.Services
              .OrderBy(s => s.Name)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Service>)t.Result, ct);

    public Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Service> CreateAsync(Service service, CancellationToken ct = default)
    {
        _db.Services.Add(service);
        await _db.SaveChangesAsync(ct);
        return service;
    }

    public async Task UpdateAsync(Service service, CancellationToken ct = default)
    {
        _db.Services.Update(service);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await _db.Services
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), ct);
        return rows > 0;
    }
}
