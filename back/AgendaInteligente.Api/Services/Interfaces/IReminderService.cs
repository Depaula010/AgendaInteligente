namespace AgendaInteligente.Api.Services.Interfaces;

public interface IReminderService
{
    /// <summary>
    /// Processa lembretes de todos os tenants com ReminderLeadTimeHours configurado.
    /// Invocado pelo ReminderBackgroundService a cada hora.
    /// </summary>
    Task ProcessRemindersAsync(CancellationToken ct = default);
}
