using AgendaInteligente.Api.Models.AI;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IConversationHistoryService
{
    /// <summary>
    /// Retorna o histórico de mensagens de uma conversa.
    /// Chave Redis: chat:{tenantId}:{phone}
    /// Retorna lista vazia se não houver histórico ou se o Redis estiver indisponível.
    /// </summary>
    Task<List<MessageHistory>> GetHistoryAsync(Guid tenantId, string phone, CancellationToken ct = default);

    /// <summary>
    /// Persiste o histórico atualizado de uma conversa (TTL: 24h).
    /// </summary>
    Task SaveHistoryAsync(Guid tenantId, string phone, List<MessageHistory> history, CancellationToken ct = default);

    /// <summary>
    /// Verifica se uma mensagem já foi processada recentemente (debounce anti-duplicata).
    /// Chave Redis: debounce:{messageId} — TTL: 30 segundos.
    /// Retorna false e registra a chave quando é a primeira vez que a mensagem é vista.
    /// Retorna true quando a mensagem já existe no cache (duplicata).
    /// Em caso de falha do Redis, retorna false para não bloquear o processamento.
    /// </summary>
    Task<bool> IsMessageDuplicateAsync(string messageId, CancellationToken ct = default);
}
