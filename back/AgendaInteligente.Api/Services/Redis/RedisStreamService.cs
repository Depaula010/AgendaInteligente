using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AgendaInteligente.Api.Services.Redis;

public sealed class RedisStreamService : IRedisStreamService
{
    private readonly IConnectionMultiplexer          _redis;
    private readonly RedisStreamOptions              _opts;
    private readonly ILogger<RedisStreamService>     _logger;

    public bool IsAvailable => true;

    public RedisStreamService(
        IConnectionMultiplexer      redis,
        IOptions<RedisStreamOptions> opts,
        ILogger<RedisStreamService>  logger)
    {
        _redis  = redis;
        _opts   = opts.Value;
        _logger = logger;
    }

    public async Task PublishInboundAsync(
        Guid   tenantId,
        string numeroRemetente,
        string texto,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        var entryId = await db.StreamAddAsync(
            _opts.InboundStream,
            [
                new NameValueEntry("tenant_id",        tenantId.ToString()),
                new NameValueEntry("numero_remetente",  numeroRemetente),
                new NameValueEntry("texto",             texto),
                new NameValueEntry("timestamp",         DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()),
            ]);

        _logger.LogDebug(
            "[STREAM] Publicado em {Stream} — id={EntryId} tenant={TenantId} phone={Phone}",
            _opts.InboundStream, entryId, tenantId, numeroRemetente);
    }
}
