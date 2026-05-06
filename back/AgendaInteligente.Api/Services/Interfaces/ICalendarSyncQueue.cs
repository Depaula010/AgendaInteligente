using AgendaInteligente.Api.Services.CalendarSync;

namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Fila em memória usada para desacoplar o <c>ScheduleService</c> (produtor)
/// do <c>GoogleCalendarSyncBackgroundService</c> (consumidor).
/// Implementação baseada em <see cref="System.Threading.Channels.Channel{T}"/>.
/// </summary>
public interface ICalendarSyncQueue
{
    /// <summary>
    /// Enfileira uma mensagem de sincronização de forma assíncrona.
    /// Retorna imediatamente — sem bloquear o fluxo HTTP.
    /// </summary>
    ValueTask EnqueueAsync(CalendarSyncMessage message, CancellationToken ct = default);

    /// <summary>
    /// Lê mensagens da fila em modo streaming (<c>IAsyncEnumerable</c>).
    /// Bloqueia de forma assíncrona quando a fila está vazia.
    /// Completa quando o <paramref name="ct"/> é cancelado (shutdown do app).
    /// </summary>
    IAsyncEnumerable<CalendarSyncMessage> DequeueAsync(CancellationToken ct);
}
