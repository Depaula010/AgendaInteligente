using AgendaInteligente.Api.Models.AI;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IAiOrchestratorService
{
    /// <summary>
    /// Orquestra a geração do contexto dinâmico e chamada da IA para entender a intenção do cliente.
    /// </summary>
    Task<GeminiIntentResponse> ProcessUserMessageAsync(Guid tenantId, string userMessage, List<MessageHistory> history);
}
