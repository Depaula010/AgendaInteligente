using AgendaInteligente.Api.Services.Interfaces;

namespace AgendaInteligente.Api.Services.Redis;

/// <summary>
/// Implementação nula registrada quando Redis não está configurado.
/// IsAvailable = false faz o WebhookEndpoints cair no fallback síncrono.
/// </summary>
public sealed class NullRedisStreamService : IRedisStreamService
{
    public bool IsAvailable => false;

    public Task PublishInboundAsync(
        Guid   tenantId,
        string numeroRemetente,
        string texto,
        CancellationToken ct = default)
        => Task.CompletedTask;
}
