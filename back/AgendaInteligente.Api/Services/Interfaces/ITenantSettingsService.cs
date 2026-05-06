using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface ITenantSettingsService
{
    /// <summary>
    /// Retorna as configurações do Tenant atual.
    /// Se ainda não existirem, retorna null (o frontend deve guiar a criação).
    /// </summary>
    Task<TenantSettings?> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Cria as configurações iniciais do Tenant.
    /// Lança InvalidOperationException se já existirem (usar UpdateAsync em vez disso).
    /// </summary>
    Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken ct = default);

    Task<TenantSettings> UpdateAsync(TenantSettings settings, CancellationToken ct = default);
}
