namespace AgendaInteligente.Api.Contracts.Reminders;

/// <summary>
/// Estado armazenado no Redis enquanto aguarda a resposta de confirmação do cliente.
/// Chave: reminder:confirm:{tenantId}:{phone}   TTL: 4 horas
/// </summary>
public sealed record PendingReminderState(
    Guid     ScheduleId,
    Guid     TenantId,
    string   CustomerPhone,
    DateTime AppointmentStart,
    string   ServiceName,
    string   ProfessionalName);
