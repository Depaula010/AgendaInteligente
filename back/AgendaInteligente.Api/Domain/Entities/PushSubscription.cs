using AgendaInteligente.Api.Domain.Interfaces;

namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Subscription Web Push de um profissional em um dispositivo específico.
/// Cada dispositivo/browser gera um endpoint único emitido pelo serviço de push (Google/Mozilla/Apple).
/// </summary>
public sealed class PushSubscription : IMustHaveTenant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    /// <summary>URL do endpoint emitido pelo serviço de push (único por dispositivo/subscription).</summary>
    public required string Endpoint { get; set; }

    /// <summary>Chave pública P-256 ECDH do cliente, em Base64Url.</summary>
    public required string P256dh { get; set; }

    /// <summary>Segredo de autenticação HMAC do cliente, em Base64Url.</summary>
    public required string Auth { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
