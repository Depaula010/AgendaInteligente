namespace AgendaInteligente.Api.Contracts.Responses;

public record ServiceCatalogResponse(
    Guid Id,
    string Name,
    int DurationMinutes,
    decimal Price,
    string? Description,
    string? CalendarColor,
    bool IsActive,
    DateTime CreatedAt
);
