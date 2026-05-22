using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaInteligente.Api.Repositories;

public sealed class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly AppDbContext _db;

    public PushSubscriptionRepository(AppDbContext db) => _db = db;

    public async Task UpsertAsync(PushSubscription sub, CancellationToken ct = default)
    {
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == sub.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dh         = sub.P256dh;
            existing.Auth           = sub.Auth;
            existing.ProfessionalId = sub.ProfessionalId;
        }
        else
        {
            _db.PushSubscriptions.Add(sub);
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct = default)
        => _db.PushSubscriptions
              .ToListAsync(ct)
              .ContinueWith(t => (IReadOnlyList<PushSubscription>)t.Result, ct);

    public async Task DeleteByEndpointAsync(string endpoint, CancellationToken ct = default)
    {
        await _db.PushSubscriptions
            .Where(s => s.Endpoint == endpoint)
            .ExecuteDeleteAsync(ct);
    }
}
