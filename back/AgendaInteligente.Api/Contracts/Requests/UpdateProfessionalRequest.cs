namespace AgendaInteligente.Api.Contracts.Requests;

public record UpdateProfessionalRequest(
    string Name,
    string? CalendarColor,
    bool IsActive
);
