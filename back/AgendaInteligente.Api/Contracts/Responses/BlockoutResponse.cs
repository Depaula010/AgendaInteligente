namespace AgendaInteligente.Api.Contracts.Responses;

public record BlockoutResponse(
    Guid Id,
    Guid ProfessionalId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? BlockReason,
    bool IsAllDay,
    DateTime CreatedAt
);
