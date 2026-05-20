namespace AgendaInteligente.Api.Contracts.Requests;

public record UpdateBlockoutRequest(
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? BlockReason,
    bool IsAllDay
);
