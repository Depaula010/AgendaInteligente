using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Domain.Interfaces;

namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Representa um profissional (barbeiro, esteticista, etc.) cadastrado no estabelecimento.
/// Pode ser Owner (dono) ou Staff (colaborador).
/// Isolado por TenantId — um profissional pertence a um único Tenant.
/// </summary>
public sealed class Professional : IMustHaveTenant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ── Multi-tenancy (OBRIGATÓRIO) ────────────────────────────────────────────
    /// <summary>
    /// Chave estrangeira para o Tenant. Filtrada automaticamente via
    /// Global Query Filter no AppDbContext para garantir o isolamento SaaS.
    /// </summary>
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Dados do Profissional ──────────────────────────────────────────────────
    public required string Name { get; set; }

    /// <summary>
    /// E-mail usado para login no PWA e para futuras integrações (ex: Google Calendar OAuth).
    /// </summary>
    public required string Email { get; set; }

    /// <summary>Senha armazenada como hash (bcrypt ou similar). Nunca em plain-text.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Define o papel do profissional: Owner (dono) ou Staff (colaborador).</summary>
    public ProfessionalRole Role { get; set; } = ProfessionalRole.Staff;

    /// <summary>Cor exibida no calendário do PWA (formato hex, ex: "#4285F4").</summary>
    public string? CalendarColor { get; set; }

    // ── Google Calendar OAuth2 ──────────────────────────────────────────────────
    /// <summary>
    /// E-mail da conta Google vinculada a este profissional para o Google Calendar.
    /// Preenchido durante o fluxo de autorização OAuth2.
    /// </summary>
    public string? GoogleCalendarEmail { get; set; }

    /// <summary>
    /// Refresh Token do OAuth2 do Google para manter o acesso ao Google Calendar sem exigir
    /// nova autorização do usuário. Deve ser armazenado de forma segura.
    /// Preenchido durante o fluxo de autorização OAuth2.
    /// </summary>
    public string? GoogleCalendarRefreshToken { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ── Navegação ──────────────────────────────────────────────────────────────
    /// <summary>Agendamentos associados a este profissional.</summary>
    public ICollection<Schedule> Schedules { get; init; } = [];

    /// <summary>Entradas na lista de espera para este profissional.</summary>
    public ICollection<Waitlist> Waitlists { get; init; } = [];
}
