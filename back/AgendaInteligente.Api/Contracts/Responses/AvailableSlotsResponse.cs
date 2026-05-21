namespace AgendaInteligente.Api.Contracts.Responses;

public sealed record AvailableSlotsResponse(
    Guid ProfessionalId,
    Guid ServiceId,
    DateOnly Date,
    int DurationMinutes,
    IReadOnlyList<DateTime> Slots);
