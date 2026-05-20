namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateBlockoutRequest(
    Guid ProfessionalId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? BlockReason,
    bool IsAllDay
);
