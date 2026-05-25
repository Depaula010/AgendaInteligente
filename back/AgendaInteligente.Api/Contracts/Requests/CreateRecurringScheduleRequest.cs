namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateRecurringScheduleRequest(
    Guid     CustomerId,
    Guid     ProfessionalId,
    Guid     ServiceId,
    DateTime StartDateTime,
    int      RepeatWeeklyCount,
    string?  Notes = null
);
