using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class ProfessionalRepository : IProfessionalRepository
{
    private readonly AppDbContext _db;

    public ProfessionalRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Professional>> GetAllActiveAsync(CancellationToken ct = default)
        => _db.Professionals
              .Where(p => p.IsActive)
              .OrderBy(p => p.Name)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Professional>)t.Result, ct);

    public Task<IReadOnlyList<Professional>> GetAllAsync(CancellationToken ct = default)
        => _db.Professionals
              .OrderBy(p => p.Name)
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Professional>)t.Result, ct);

    public Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Professionals.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Professional?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Professionals.FirstOrDefaultAsync(
               p => p.Email.ToLower() == email.ToLower(), ct);

    public Task<Professional?> GetByEmailIgnoringQueryFilterAsync(string email, CancellationToken ct = default)
        => _db.Professionals
              .IgnoreQueryFilters()
              .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower(), ct);

    public Task<Professional?> GetByIdIgnoringQueryFilterAsync(Guid id, CancellationToken ct = default)
        => _db.Professionals
              .IgnoreQueryFilters()
              .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Professional> CreateAsync(Professional professional, CancellationToken ct = default)
    {
        _db.Professionals.Add(professional);
        await _db.SaveChangesAsync(ct);
        return professional;
    }

    public async Task UpdateAsync(Professional professional, CancellationToken ct = default)
    {
        _db.Professionals.Update(professional);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await _db.Professionals
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), ct);
        return rows > 0;
    }
}
