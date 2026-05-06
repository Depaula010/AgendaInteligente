namespace AgendaInteligente.Api.Contracts.Requests;

/// <summary>Payload para criar um novo estabelecimento no SaaS.</summary>
public sealed record CreateTenantRequest(
    string Name,

    /// <summary>
    /// Identificador único amigável para URL (ex: "barbearia-do-ze").
    /// Será normalizado para lowercase pelo serviço.
    /// </summary>
    string Slug
);
