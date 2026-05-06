namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateProfessionalRequest(
    string Name,
    string Email,
    string Password,
    string? CalendarColor = null
);
