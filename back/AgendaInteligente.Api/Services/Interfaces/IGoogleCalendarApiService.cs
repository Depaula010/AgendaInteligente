using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Abstração da comunicação com a Google Calendar API v3.
/// Isola os detalhes do SDK do Google do restante da aplicação,
/// facilitando mocking nos testes e eventual substituição de provider.
/// </summary>
public interface IGoogleCalendarApiService
{
    /// <summary>
    /// Cria ou atualiza um evento no Google Calendar do profissional.
    /// </summary>
    /// <param name="schedule">Dados completos do agendamento (inclui navegação para Professional e Service).</param>
    /// <param name="refreshToken">Refresh Token OAuth2 do profissional para renovar o acesso.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>O ID do evento no Google Calendar (GoogleCalendarEventId), ou null em caso de falha.</returns>
    Task<string?> UpsertEventAsync(Schedule schedule, string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Remove um evento do Google Calendar do profissional.
    /// </summary>
    /// <param name="googleEventId">ID do evento no Google Calendar.</param>
    /// <param name="refreshToken">Refresh Token OAuth2 do profissional.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteEventAsync(string googleEventId, string refreshToken, CancellationToken ct = default);
}
