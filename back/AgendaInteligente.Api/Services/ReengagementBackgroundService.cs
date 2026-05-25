using AgendaInteligente.Api.Services.Interfaces;

namespace AgendaInteligente.Api.Services;

public sealed class ReengagementBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory                    _scopeFactory;
    private readonly ILogger<ReengagementBackgroundService> _logger;

    public ReengagementBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReengagementBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReengagementBackgroundService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IReengagementService>();
                await svc.ProcessAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no ReengagementBackgroundService. Próxima tentativa em 7 dias.");
            }

            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }

        _logger.LogInformation("ReengagementBackgroundService encerrado.");
    }
}
