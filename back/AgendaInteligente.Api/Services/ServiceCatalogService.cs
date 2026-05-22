using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class ServiceCatalogService : IServiceCatalogService
{
    private readonly IServiceCatalogRepository _repo;
    private readonly ILogger<ServiceCatalogService> _logger;

    public ServiceCatalogService(IServiceCatalogRepository repo, ILogger<ServiceCatalogService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public Task<IReadOnlyList<Service>> GetAllActiveAsync(CancellationToken ct = default)
        => _repo.GetAllActiveAsync(ct);

    public Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);

    public Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public async Task<Service> CreateAsync(
        string name, int durationMinutes, decimal price,
        string? description = null, string? calendarColor = null,
        CancellationToken ct = default)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("A duração do serviço deve ser maior que zero.", nameof(durationMinutes));
        if (price < 0)
            throw new ArgumentException("O preço do serviço não pode ser negativo.", nameof(price));

        var service = new Service
        {
            Name            = name,
            DurationMinutes = durationMinutes,
            Price           = price,
            Description     = description,
            CalendarColor   = calendarColor
        };

        _logger.LogInformation("Criando serviço '{Name}' ({Duration}min)", name, durationMinutes);
        return await _repo.CreateAsync(service, ct);
    }

    public async Task<Service> UpdateAsync(
        Guid id, string name, int durationMinutes, decimal price,
        string? description, string? calendarColor, bool isActive,
        CancellationToken ct = default)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("A duração do serviço deve ser maior que zero.", nameof(durationMinutes));
        if (price < 0)
            throw new ArgumentException("O preço do serviço não pode ser negativo.", nameof(price));

        var service = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Serviço '{id}' não encontrado.");

        service.Name            = name;
        service.DurationMinutes = durationMinutes;
        service.Price           = price;
        service.Description     = description;
        service.CalendarColor   = calendarColor;
        service.IsActive        = isActive;

        await _repo.UpdateAsync(service, ct);
        _logger.LogInformation("Serviço '{Id}' atualizado.", id);
        return service;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Desativando serviço '{Id}'.", id);
        return _repo.DeleteAsync(id, ct);
    }
}
