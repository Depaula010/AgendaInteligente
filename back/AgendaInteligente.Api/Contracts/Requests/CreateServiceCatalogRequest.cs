namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateServiceCatalogRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    string? Description = null,
    string? CalendarColor = null
);
