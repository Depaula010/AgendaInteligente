using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Responses;

public record ProfessionalResponse(
    Guid Id,
    string Name,
    string Email,
    ProfessionalRole Role,
    string? CalendarColor,
    bool IsActive,
    DateTime CreatedAt
);
