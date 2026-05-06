using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AgendaInteligente.Api.Services.CalendarSync;
using AgendaInteligente.Api.Services.Interfaces;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// Implementação Singleton da fila de sincronização com o Google Calendar.
/// Usa <see cref="Channel{T}"/> unbounded para garantir que nenhuma mensagem
/// seja perdida. O BackgroundService consome do outro lado do channel.
/// </summary>
public sealed class CalendarSyncQueue : ICalendarSyncQueue
{
    // Channel unbounded: o produtor nunca bloqueia; em caso de falha no consumer,
    // as mensagens ficam em memória até o próximo ciclo do BackgroundService.
    private readonly Channel<CalendarSyncMessage> _channel =
        Channel.CreateUnbounded<CalendarSyncMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,   // Apenas o BackgroundService consome
            SingleWriter = false   // Múltiplos requests HTTP produzem
        });

    /// <inheritdoc />
    public ValueTask EnqueueAsync(CalendarSyncMessage message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    /// <inheritdoc />
    public async IAsyncEnumerable<CalendarSyncMessage> DequeueAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Aguarda mensagens indefinidamente; cancela quando o app encerra
        await foreach (var message in _channel.Reader.ReadAllAsync(ct))
        {
            yield return message;
        }
    }
}
