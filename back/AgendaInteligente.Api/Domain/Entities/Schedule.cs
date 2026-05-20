using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Domain.Interfaces;

namespace AgendaInteligente.Api.Domain.Entities;

/// <summary>
/// Representa um agendamento no sistema.
/// É o núcleo do domínio: une o Cliente, o Profissional e o(s) Serviço(s).
/// Suporta agendamentos únicos e recorrentes ("infinitos").
/// Isolado por TenantId — um agendamento pertence a um único Tenant.
/// </summary>
public sealed class Schedule : IMustHaveTenant
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
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    public Guid? ServiceId { get; set; }
    public Service? Service { get; set; }

    // ── Dados do Agendamento ───────────────────────────────────────────────────
    /// <summary>Data e hora de início do agendamento (sempre em UTC).</summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Data e hora de término. Calculada como StartDateTime + Service.DurationMinutes.
    /// Armazenada explicitamente para facilitar consultas de conflito de horários.
    /// </summary>
    public DateTime EndDateTime { get; set; }

    public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

    /// <summary>Observações opcionais do cliente ou do profissional.</summary>
    public string? Notes { get; set; }

    // ── Blockout (Folgas) ──────────────────────────────────────────────────────
    /// <summary>Indica se este registro é um bloqueio de agenda (folga).</summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>Motivo da folga (ex: Férias, Feriado). Usado apenas se IsBlocked = true.</summary>
    public string? BlockReason { get; set; }

    /// <summary>Se true, o bloqueio/agendamento dura o dia todo (ignora a hora no calendário).</summary>
    public bool IsAllDay { get; set; } = false;

    // ── Recorrência ────────────────────────────────────────────────────────────
    /// <summary>Indica se este agendamento faz parte de uma série recorrente.</summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// ID da série de recorrência. Todos os agendamentos de uma mesma recorrência
    /// compartilham o mesmo RecurrenceGroupId, facilitando cancelar/remover a série inteira.
    /// </summary>
    public Guid? RecurrenceGroupId { get; set; }

    /// <summary>
    /// Regra de recorrência no formato RRULE (RFC 5545).
    /// Ex: "FREQ=WEEKLY;BYDAY=TU" para todas as terças-feiras.
    /// Nulo para agendamentos únicos.
    /// </summary>
    public string? RecurrenceRule { get; set; }

    // ── Google Calendar ────────────────────────────────────────────────────────
    /// <summary>
    /// ID do evento no Google Calendar do profissional.
    /// Preenchido pelo BackgroundService de sincronização após a criação.
    /// Null indica que o evento ainda não foi sincronizado.
    /// </summary>
    public string? GoogleCalendarEventId { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
