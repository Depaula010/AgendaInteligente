using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.CalendarSync;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// BackgroundService responsável pela sincronização secundária dos agendamentos
/// com o Google Calendar. Consome mensagens da <see cref="ICalendarSyncQueue"/>
/// e invoca a <see cref="IGoogleCalendarApiService"/> de forma assíncrona,
/// fora do fluxo das requisições HTTP.
///
/// Design:
/// - Executa em loop infinito (padrão BackgroundService).
/// - Cria um <see cref="IServiceScope"/> para cada mensagem, garantindo que serviços
///   Scoped (AppDbContext, Repositories) sejam resolvidos e descartados corretamente.
/// - Erros individuais são logados e o processamento continua (resiliência).
/// </summary>
public sealed class GoogleCalendarSyncBackgroundService : BackgroundService
{
    private readonly ICalendarSyncQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GoogleCalendarSyncBackgroundService> _logger;

    public GoogleCalendarSyncBackgroundService(
        ICalendarSyncQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<GoogleCalendarSyncBackgroundService> logger)
    {
        _queue        = queue;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "GoogleCalendarSyncBackgroundService iniciado. Aguardando mensagens na fila...");

        await foreach (var message in _queue.DequeueAsync(stoppingToken))
        {
            // Um scope por mensagem garante que AppDbContext seja descartado após cada operação
            await using var scope = _scopeFactory.CreateAsyncScope();

            try
            {
                await ProcessMessageAsync(message, scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                // Loga e continua — nunca derruba o background service por uma falha isolada
                _logger.LogError(ex,
                    "Falha não tratada ao processar mensagem da fila. " +
                    "ScheduleId={ScheduleId}, Operação={Operation}",
                    message.ScheduleId, message.Operation);
            }
        }

        _logger.LogInformation("GoogleCalendarSyncBackgroundService encerrado.");
    }

    // ─── Message Processing ──────────────────────────────────────────────────

    private async Task ProcessMessageAsync(
        CalendarSyncMessage message,
        IServiceProvider services,
        CancellationToken ct)
    {
        switch (message.Operation)
        {
            case CalendarSyncOperation.Upsert:
                await HandleUpsertAsync(message, services, ct);
                break;

            case CalendarSyncOperation.Delete:
                await HandleDeleteAsync(message, services, ct);
                break;

            default:
                _logger.LogWarning(
                    "Operação desconhecida '{Operation}' ignorada para Schedule '{ScheduleId}'.",
                    message.Operation, message.ScheduleId);
                break;
        }
    }

    private async Task HandleUpsertAsync(
        CalendarSyncMessage message,
        IServiceProvider services,
        CancellationToken ct)
    {
        var scheduleRepo    = services.GetRequiredService<IScheduleRepository>();
        var googleCalendar  = services.GetRequiredService<IGoogleCalendarApiService>();

        // Carrega o Schedule com todas as navegações necessárias para montar o evento
        var schedule = await scheduleRepo.GetByIdAsync(message.ScheduleId, ct);

        if (schedule is null)
        {
            _logger.LogWarning(
                "Schedule '{ScheduleId}' não encontrado para sincronização (Upsert). Ignorando.",
                message.ScheduleId);
            return;
        }

        var professional = schedule.Professional;

        if (string.IsNullOrWhiteSpace(professional?.GoogleCalendarRefreshToken))
        {
            _logger.LogInformation(
                "Profissional '{ProfessionalId}' não possui RefreshToken do Google Calendar. " +
                "Sincronização ignorada para Schedule '{ScheduleId}'.",
                professional?.Id, message.ScheduleId);
            return;
        }

        var googleEventId = await googleCalendar.UpsertEventAsync(
            schedule, professional.GoogleCalendarRefreshToken, ct);

        if (googleEventId is not null && schedule.GoogleCalendarEventId != googleEventId)
        {
            var updated = await scheduleRepo.UpdateGoogleEventIdAsync(
                message.ScheduleId, googleEventId, ct);

            if (updated)
                _logger.LogInformation(
                    "GoogleCalendarEventId '{EventId}' persistido para Schedule '{ScheduleId}'.",
                    googleEventId, message.ScheduleId);
        }
    }

    private async Task HandleDeleteAsync(
        CalendarSyncMessage message,
        IServiceProvider services,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.GoogleCalendarEventId))
        {
            _logger.LogDebug(
                "Nenhum GoogleCalendarEventId para Schedule '{ScheduleId}'. Delete ignorado.",
                message.ScheduleId);
            return;
        }

        // Para o Delete precisamos do RefreshToken do profissional.
        // Buscamos o schedule antes de ser apagado (já passou pelo Delete no DB).
        // Caso não exista mais, usamos apenas o EventId que veio na mensagem.
        var scheduleRepo   = services.GetRequiredService<IScheduleRepository>();
        var googleCalendar = services.GetRequiredService<IGoogleCalendarApiService>();

        var schedule = await scheduleRepo.GetByIdAsync(message.ScheduleId, ct);

        var refreshToken = schedule?.Professional?.GoogleCalendarRefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogInformation(
                "RefreshToken não disponível para deleção do evento '{EventId}'. Ignorando.",
                message.GoogleCalendarEventId);
            return;
        }

        await googleCalendar.DeleteEventAsync(message.GoogleCalendarEventId, refreshToken, ct);
    }
}
