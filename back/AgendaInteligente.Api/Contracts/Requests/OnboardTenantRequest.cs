namespace AgendaInteligente.Api.Contracts.Requests;

public sealed record OnboardTenantRequest(
    string TenantName,
    string Slug,
    string OwnerName,
    string OwnerEmail,
    string OwnerPassword
);
