using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Requests;

public record UpdateProfessionalRequest(
    string Name,
    string? CalendarColor,
    bool IsActive,
    ProfessionalRole? Role = null,
    bool? CanManageServices = null
);
