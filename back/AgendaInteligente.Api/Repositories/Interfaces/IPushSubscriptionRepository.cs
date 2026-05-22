using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface IPushSubscriptionRepository
{
    /// <summary>Insere ou atualiza a subscription pelo endpoint (globalmente único).</summary>
    Task UpsertAsync(PushSubscription sub, CancellationToken ct = default);

    /// <summary>Retorna todas as subscriptions do tenant atual (filtrado via Global Query Filter).</summary>
    Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Remove a subscription com o endpoint especificado.</summary>
    Task DeleteByEndpointAsync(string endpoint, CancellationToken ct = default);
}
