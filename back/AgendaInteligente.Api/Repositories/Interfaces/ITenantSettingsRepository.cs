using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface ITenantSettingsRepository
{
    /// <summary>
    /// Retorna as configurações do Tenant atual (relação 1:1).
    /// Retorna null se ainda não foram criadas.
    /// </summary>
    Task<TenantSettings?> GetAsync(CancellationToken ct = default);

    Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken ct = default);
    Task UpdateAsync(TenantSettings settings, CancellationToken ct = default);
}
