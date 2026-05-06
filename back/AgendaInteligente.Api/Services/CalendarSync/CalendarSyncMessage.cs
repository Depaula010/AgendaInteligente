namespace AgendaInteligente.Api.Services.CalendarSync;

/// <summary>
/// Operação a ser executada na sincronização com o Google Calendar.
/// </summary>
public static class CalendarSyncOperation
{
    public const string Upsert = "Upsert";
    public const string Delete = "Delete";
}

/// <summary>
/// Mensagem enfileirada pelo ScheduleService para ser processada
/// de forma assíncrona pelo <see cref="GoogleCalendarSyncBackgroundService"/>.
/// </summary>
/// <param name="ScheduleId">ID do agendamento a sincronizar.</param>
/// <param name="TenantId">Tenant ao qual o agendamento pertence.</param>
/// <param name="Operation">Operação: "Upsert" (criar/atualizar) ou "Delete" (remover).</param>
/// <param name="GoogleCalendarEventId">
/// Preenchido apenas para operações de Delete. Indica qual evento remover no Google Calendar.
/// </param>
public sealed record CalendarSyncMessage(
    Guid ScheduleId,
    Guid TenantId,
    string Operation,
    string? GoogleCalendarEventId = null);
