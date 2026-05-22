using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<Service>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct = default);
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Service> CreateAsync(
        string name, int durationMinutes, decimal price,
        string? description = null, string? calendarColor = null,
        CancellationToken ct = default);
    Task<Service> UpdateAsync(
        Guid id, string name, int durationMinutes, decimal price,
        string? description, string? calendarColor, bool isActive,
        CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
