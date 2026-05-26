namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateRecurringScheduleRequest(
    Guid     CustomerId,
    Guid     ProfessionalId,
    Guid     ServiceId,
    DateTime StartDateTime,
    string   RepeatType,    // "weekly" | "monthly"
    int?     RepeatCount,   // null = indefinite (2 years)
    string?  Notes = null
);
