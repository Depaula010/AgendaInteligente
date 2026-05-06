using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class TenantSettingsService : ITenantSettingsService
{
    private readonly ITenantSettingsRepository _repo;
    private readonly ILogger<TenantSettingsService> _logger;

    public TenantSettingsService(ITenantSettingsRepository repo, ILogger<TenantSettingsService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public Task<TenantSettings?> GetAsync(CancellationToken ct = default)
        => _repo.GetAsync(ct);

    public async Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken ct = default)
    {
        // Garante que não há duplicata (relação 1:1 com Tenant)
        var existing = await _repo.GetAsync(ct);
        if (existing is not null)
            throw new InvalidOperationException(
                "As configurações deste estabelecimento já foram criadas. Use o método de atualização.");

        _logger.LogInformation("Criando configurações para o Tenant.");
        return await _repo.CreateAsync(settings, ct);
    }

    public async Task<TenantSettings> UpdateAsync(TenantSettings settings, CancellationToken ct = default)
    {
        await _repo.UpdateAsync(settings, ct);
        _logger.LogInformation("Configurações do Tenant atualizadas.");
        return settings;
    }
}
