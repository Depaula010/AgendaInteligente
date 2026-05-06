using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Requests;

public record UpdateScheduleStatusRequest(
    ScheduleStatus Status
);
