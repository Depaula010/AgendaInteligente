namespace AgendaInteligente.Api.Contracts.Requests;

public record UpdateScheduleRequest(
    DateTime StartDateTime,
    string? Notes = null
);
