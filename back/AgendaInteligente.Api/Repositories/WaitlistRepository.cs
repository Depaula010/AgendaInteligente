using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class WaitlistRepository : IWaitlistRepository
{
    private readonly AppDbContext _db;

    public WaitlistRepository(AppDbContext db) => _db = db;

    public async Task<Waitlist> AddAsync(Waitlist entry, CancellationToken ct = default)
    {
        _db.Waitlists.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    /// <summary>
    /// Busca entradas pendentes para uma data desejada, opcionalmente filtradas por profissional.
    /// A query é automaticamente filtrada pelo TenantId via Global Query Filter.
    /// Um cliente sem preferência de profissional (ProfessionalId == null) é elegível para qualquer profissional.
    /// </summary>
    public Task<IReadOnlyList<Waitlist>> GetPendingByDateAsync(
        DateOnly desiredDate,
        Guid? professionalId,
        CancellationToken ct = default)
        => _db.Waitlists
              .Include(w => w.Customer)
              .Where(w =>
                  w.Status == WaitlistStatus.Waiting &&
                  w.DesiredDate == desiredDate &&
                  // Filtra por profissional: inclui quem pediu esse profissional OU quem aceitaria qualquer um
                  (w.ProfessionalId == null || w.ProfessionalId == professionalId))
              .OrderBy(w => w.CreatedAt)   // FIFO — quem entrou primeiro tem prioridade
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<Waitlist>)t.Result, ct);

    public async Task UpdateAsync(Waitlist entry, CancellationToken ct = default)
    {
        _db.Waitlists.Update(entry);
        await _db.SaveChangesAsync(ct);
    }
}
