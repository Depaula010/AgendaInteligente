namespace AgendaInteligente.Api.Contracts.Responses;

/// <summary>Representação pública de um Tenant retornada pelos endpoints.</summary>
public sealed record TenantResponse(
    Guid     Id,
    string   Name,
    string   Slug,
    bool     IsActive,
    DateTime CreatedAt
);
