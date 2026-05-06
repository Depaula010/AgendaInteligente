namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Representa um estabelecimento/empresa no SaaS (barbearia, clínica, etc.).
/// É a entidade raiz do modelo multi-tenant.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nome comercial do estabelecimento.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Identificador único amigável para URLs (ex: "barbearia-do-ze").
    /// Usado para resolver o TenantId a partir de subdomínios ou rotas.
    /// </summary>
    public required string Slug { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ── Navegação ──────────────────────────────────────────────────────────────
    public ICollection<Customer> Customers { get; init; } = [];
    public ICollection<Professional> Professionals { get; init; } = [];
    public ICollection<Service> Services { get; init; } = [];
    public ICollection<Schedule> Schedules { get; init; } = [];
    public ICollection<Waitlist> Waitlists { get; init; } = [];

    /// <summary>Configurações do estabelecimento (relação 1:1).</summary>
    public TenantSettings? Settings { get; set; }
}
