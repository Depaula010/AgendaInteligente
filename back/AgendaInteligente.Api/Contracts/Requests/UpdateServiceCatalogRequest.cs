namespace AgendaInteligente.Api.Contracts.Requests;

public record UpdateServiceCatalogRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    string? Description,
    string? CalendarColor,
    bool IsActive
);
