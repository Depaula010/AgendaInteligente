using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Responses;

public record ProfessionalResponse(
    Guid Id,
    string Name,
    string Email,
    ProfessionalRole Role,
    bool CanManageServices,
    string? CalendarColor,
    bool IsActive,
    DateTime CreatedAt,
    string? WorkingHoursJson = null
);
