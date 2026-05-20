using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgendaInteligente.Api.Services;

public sealed class ConversationHistoryService : IConversationHistoryService
{
    private static readonly TimeSpan HistoryTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan DebounceTtl = TimeSpan.FromSeconds(30);

    private readonly IDistributedCache _cache;
    private readonly ILogger<ConversationHistoryService> _logger;

    public ConversationHistoryService(IDistributedCache cache, ILogger<ConversationHistoryService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<MessageHistory>> GetHistoryAsync(Guid tenantId, string phone, CancellationToken ct = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(HistoryKey(tenantId, phone), ct);
            if (string.IsNullOrEmpty(json))
                return [];

            return JsonSerializer.Deserialize<List<MessageHistory>>(json) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis indisponível ao ler histórico. TenantId={TenantId}, Phone={Phone}. Retornando histórico vazio.", tenantId, phone);
            return [];
        }
    }

    public async Task SaveHistoryAsync(Guid tenantId, string phone, List<MessageHistory> history, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(history);
            await _cache.SetStringAsync(
                HistoryKey(tenantId, phone),
                json,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = HistoryTtl },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis indisponível ao salvar histórico. TenantId={TenantId}, Phone={Phone}.", tenantId, phone);
        }
    }

    public async Task<bool> IsMessageDuplicateAsync(string messageId, CancellationToken ct = default)
    {
        try
        {
            var existing = await _cache.GetStringAsync(DebounceKey(messageId), ct);
            if (existing is not null)
                return true;

            await _cache.SetStringAsync(
                DebounceKey(messageId),
                "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DebounceTtl },
                ct);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis indisponível ao verificar debounce. MessageId={MessageId}. Processando normalmente.", messageId);
            return false;
        }
    }

    internal static string HistoryKey(Guid tenantId, string phone) => $"chat:{tenantId}:{phone}";
    internal static string DebounceKey(string messageId) => $"debounce:{messageId}";
}
