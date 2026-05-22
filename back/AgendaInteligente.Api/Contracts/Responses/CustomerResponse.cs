namespace AgendaInteligente.Api.Contracts.Responses;

public record CustomerResponse(
    Guid Id,
    string Name,
    string PhoneNumber,
    string? Email,
    DateTime? LastVisitAt
);
