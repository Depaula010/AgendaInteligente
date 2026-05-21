namespace AgendaInteligente.Api.Contracts.Responses;

public sealed record OnboardTenantResponse(
    Guid TenantId,
    Guid ProfessionalId,
    string Slug
);
