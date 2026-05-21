using AgendaInteligente.Api.Services.Interfaces;

namespace AgendaInteligente.Api.Services;

public sealed class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory               _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope  = _scopeFactory.CreateAsyncScope();
                var reminderService    = scope.ServiceProvider.GetRequiredService<IReminderService>();
                await reminderService.ProcessRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no ReminderBackgroundService. Próxima tentativa em 1 hora.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("ReminderBackgroundService encerrado.");
    }
}
