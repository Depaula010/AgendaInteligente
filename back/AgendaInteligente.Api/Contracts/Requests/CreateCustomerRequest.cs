namespace AgendaInteligente.Api.Contracts.Requests;

public record CreateCustomerRequest(
    string Name,
    string PhoneNumber,
    string? Email
);
