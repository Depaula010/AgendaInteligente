namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateScheduleRequest(
    Guid CustomerId,
    Guid ProfessionalId,
    Guid ServiceId,
    DateTime StartDateTime,
    string? Notes = null
);
