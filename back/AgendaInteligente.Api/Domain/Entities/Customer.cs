namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Representa o cliente final de um estabelecimento.
/// Sempre isolado por TenantId — um cliente pertence a um único Tenant.
/// </summary>
public sealed class Customer
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ── Multi-tenancy (OBRIGATÓRIO) ────────────────────────────────────────────
    /// <summary>
    /// Chave estrangeira para o Tenant. Filtrada automaticamente via
    /// Global Query Filter no AppDbContext para garantir o isolamento SaaS.
    /// </summary>
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Dados do Cliente ───────────────────────────────────────────────────────
    public required string Name { get; set; }

    /// <summary>Número WhatsApp no formato E.164 (ex: +5511999998888).</summary>
    public required string PhoneNumber { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Data da última visita — usado para gatilhos de reengajamento.</summary>
    public DateTime? LastVisitAt { get; set; }
}
