using AgendaInteligente.Api.Domain.Interfaces;

namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Representa um serviço oferecido pelo estabelecimento (ex: Corte, Barba, Hidratação).
/// Cada serviço define duração e preço para montar a agenda e o contexto da IA.
/// Isolado por TenantId — um serviço pertence a um único Tenant.
/// </summary>
public sealed class Service : IMustHaveTenant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ── Multi-tenancy (OBRIGATÓRIO) ────────────────────────────────────────────
    /// <summary>
    /// Chave estrangeira para o Tenant. Filtrada automaticamente via
    /// Global Query Filter no AppDbContext para garantir o isolamento SaaS.
    /// </summary>
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Dados do Serviço ───────────────────────────────────────────────────────
    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Duração do serviço em minutos. Usada para calcular o bloqueio total na agenda
    /// quando o cliente solicita múltiplos serviços.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>Preço do serviço. Precision(10,2) no banco.</summary>
    public decimal Price { get; set; }

    /// <summary>Cor exibida no calendário do PWA (formato hex, ex: "#34A853").</summary>
    public string? CalendarColor { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ── Navegação ──────────────────────────────────────────────────────────────
    /// <summary>Agendamentos que incluem este serviço.</summary>
    public ICollection<Schedule> Schedules { get; init; } = [];
}
