using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Repositories.Interfaces;

public interface ITenantSettingsRepository
{
    /// <summary>
    /// Retorna as configurações do Tenant atual (relação 1:1) usando o filtro global de tenant.
    /// Retorna null se ainda não foram criadas.
    /// </summary>
    Task<TenantSettings?> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Retorna as configurações de um tenant específico ignorando o filtro global.
    /// Usar em serviços de background ou quando o TenantId vem de parâmetro (ex: WhatsAppSendService).
    /// </summary>
    Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Retorna todos os TenantSettings com ReminderLeadTimeHours &gt; 0 (ignorando o filtro global).
    /// Usado pelo ReminderBackgroundService para iterar os tenants que têm lembretes ativos.
    /// </summary>
    Task<IReadOnlyList<TenantSettings>> GetAllWithReminderEnabledAsync(CancellationToken ct = default);

    Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken ct = default);
    Task UpdateAsync(TenantSettings settings, CancellationToken ct = default);
}
