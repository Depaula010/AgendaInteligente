using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Responses;

public record ScheduleResponse(
    Guid Id,
    Guid CustomerId,
    Guid ProfessionalId,
    Guid ServiceId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    ScheduleStatus Status,
    string? Notes,
    DateTime CreatedAt
);
