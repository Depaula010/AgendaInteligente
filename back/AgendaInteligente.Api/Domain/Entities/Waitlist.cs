using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Representa uma entrada na lista de espera inteligente.
/// Quando um horário está lotado, o cliente pode ser enfileirado.
/// Ao surgir uma vaga, o sistema notifica os clientes em ordem de entrada.
/// Isolado por TenantId — uma entrada pertence a um único Tenant.
/// </summary>
public sealed class Waitlist
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ── Multi-tenancy (OBRIGATÓRIO) ────────────────────────────────────────────
    /// <summary>
    /// Chave estrangeira para o Tenant. Filtrada automaticamente via
    /// Global Query Filter no AppDbContext para garantir o isolamento SaaS.
    /// </summary>
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Relacionamentos ────────────────────────────────────────────────────────
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Profissional de preferência do cliente.
    /// Null significa "qualquer profissional disponível".
    /// </summary>
    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    /// <summary>Serviço desejado pelo cliente.</summary>
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    // ── Detalhes da Espera ─────────────────────────────────────────────────────
    /// <summary>Data desejada pelo cliente para ser atendido.</summary>
    public DateOnly DesiredDate { get; set; }

    /// <summary>Horário preferencial de início (opcional — null = qualquer horário).</summary>
    public TimeOnly? PreferredTime { get; set; }

    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    /// <summary>
    /// Data/hora em que a notificação de vaga foi enviada ao cliente.
    /// Null enquanto Status == Waiting.
    /// </summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>
    /// Se convertido, referência ao agendamento gerado.
    /// </summary>
    public Guid? ConvertedScheduleId { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
