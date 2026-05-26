using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateProfessionalRequest(
    string Name,
    string Email,
    string Password,
    string? CalendarColor = null,
    ProfessionalRole? Role = null,
    bool CanManageServices = false
);
