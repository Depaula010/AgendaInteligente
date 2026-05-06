using AgendaInteligente.Api.Models.AI;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IGeminiService
{
    /// <summary>
    /// Extrai a intenção do cliente, respondendo também com as variáveis mapeadas e a mensagem de reply.
    /// </summary>
    /// <param name="systemPrompt">O contexto dinâmico completo gerado para este tenant.</param>
    /// <param name="userMessage">A mensagem atual do cliente.</param>
    /// <param name="history">O histórico de mensagens anteriores da conversa.</param>
    /// <param name="apiKey">Chave da API a ser usada (do Tenant ou Global).</param>
    /// <param name="model">Modelo a ser usado (ex: gemini-2.5-flash-lite).</param>
    /// <returns>Objeto contendo a intenção, datas, horários e a mensagem de resposta.</returns>
    Task<GeminiIntentResponse> ExtractIntentAsync(
        string systemPrompt, 
        string userMessage, 
        List<MessageHistory> history,
        string apiKey,
        string model);
}
