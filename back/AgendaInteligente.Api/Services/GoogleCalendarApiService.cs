using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// Implementação da comunicação com o Google Calendar API v3.
/// Usa o SDK oficial do Google para criar, atualizar e deletar eventos
/// no calendário do profissional autenticado via OAuth2 (Refresh Token).
/// </summary>
public sealed class GoogleCalendarApiService : IGoogleCalendarApiService
{
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarApiService> _logger;

    public GoogleCalendarApiService(
        IOptions<GoogleCalendarOptions> options,
        ILogger<GoogleCalendarApiService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc />
    public async Task<string?> UpsertEventAsync(
        Schedule schedule, string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var calendarService = BuildCalendarService(refreshToken);
            var calEvent = MapToGoogleEvent(schedule);

            Event result;

            // Se já existe um GoogleCalendarEventId, atualiza; senão, cria.
            if (!string.IsNullOrWhiteSpace(schedule.GoogleCalendarEventId))
            {
                var updateRequest = calendarService.Events.Update(
                    calEvent, _options.CalendarId, schedule.GoogleCalendarEventId);

                result = await updateRequest.ExecuteAsync(ct);
                _logger.LogInformation(
                    "Evento '{EventId}' atualizado no Google Calendar para o Schedule '{ScheduleId}'.",
                    result.Id, schedule.Id);
            }
            else
            {
                var insertRequest = calendarService.Events.Insert(calEvent, _options.CalendarId);
                result = await insertRequest.ExecuteAsync(ct);
                _logger.LogInformation(
                    "Evento '{EventId}' criado no Google Calendar para o Schedule '{ScheduleId}'.",
                    result.Id, schedule.Id);
            }

            return result.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao sincronizar Schedule '{ScheduleId}' com o Google Calendar.",
                schedule.Id);
            return null; // Não propaga — BackgroundService registra e continua
        }
    }

    /// <inheritdoc />
    public async Task DeleteEventAsync(string googleEventId, string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var calendarService = BuildCalendarService(refreshToken);
            var deleteRequest   = calendarService.Events.Delete(_options.CalendarId, googleEventId);
            await deleteRequest.ExecuteAsync(ct);

            _logger.LogInformation(
                "Evento '{EventId}' removido do Google Calendar.", googleEventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao remover evento '{EventId}' do Google Calendar.", googleEventId);
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Constrói um <see cref="CalendarService"/> autenticado com o Refresh Token do profissional.
    /// O SDK do Google cuida da renovação automática do Access Token.
    /// </summary>
    private CalendarService BuildCalendarService(string refreshToken)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId     = _options.ClientId,
                ClientSecret = _options.ClientSecret
            },
            Scopes = [CalendarService.Scope.Calendar]
        });

        var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
        var credential    = new UserCredential(flow, userId: "professional", tokenResponse);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "AgendaInteligente"
        });
    }

    /// <summary>
    /// Mapeia a entidade <see cref="Schedule"/> para o objeto <see cref="Event"/> do Google Calendar.
    /// </summary>
    private static Event MapToGoogleEvent(Schedule schedule)
    {
        if (schedule.IsBlocked)
        {
            var title = string.IsNullOrWhiteSpace(schedule.BlockReason)
                ? "🔒 Bloqueado"
                : $"🔒 Bloqueado — {schedule.BlockReason}";

            var ev = new Event
            {
                Summary     = title,
                Description = "Período bloqueado na Agenda Inteligente.",
                ColorId     = "11", // Tomato / Red no Google Calendar
            };

            if (schedule.IsAllDay)
            {
                ev.Start = new EventDateTime { Date = schedule.StartDateTime.ToString("yyyy-MM-dd") };
                ev.End   = new EventDateTime { Date = schedule.EndDateTime.ToString("yyyy-MM-dd") };
            }
            else
            {
                ev.Start = new EventDateTime { DateTimeDateTimeOffset = schedule.StartDateTime, TimeZone = "UTC" };
                ev.End   = new EventDateTime { DateTimeDateTimeOffset = schedule.EndDateTime, TimeZone = "UTC" };
            }

            return ev;
        }

        var serviceName  = schedule.Service?.Name    ?? "Serviço";
        var customerName = schedule.Customer?.Name   ?? "Cliente";
        var notes        = schedule.Notes;

        return new Event
        {
            Summary     = $"{serviceName} — {customerName}",
            Description = string.IsNullOrWhiteSpace(notes)
                            ? $"Agendamento ID: {schedule.Id}"
                            : $"{notes}\n\nAgendamento ID: {schedule.Id}",
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = schedule.StartDateTime,
                TimeZone = "UTC"
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = schedule.EndDateTime,
                TimeZone = "UTC"
            }
        };
    }
}
