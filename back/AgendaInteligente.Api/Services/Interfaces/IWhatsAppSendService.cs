namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Canal único de saída para envio de mensagens de texto via WhatsApp.
/// Toda resposta ao cliente DEVE passar por este serviço — nunca chame o bot Node.js diretamente
/// de outro caminho (ARCHITECTURE.md §3.2).
/// </summary>
public interface IWhatsAppSendService
{
    /// <summary>
    /// Envia uma mensagem de texto para o número informado via bot Node.js (Baileys).
    /// Quando o bot não estiver configurado (BotUrl vazio), registra via ILogger (modo stub).
    /// </summary>
    /// <param name="tenantId">Identificador do tenant (sessão do bot).</param>
    /// <param name="phone">Número de destino em formato E.164 (ex: 5511999999999).</param>
    /// <param name="message">Texto da mensagem a enviar.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True se o envio foi bem-sucedido (ou stub); false em caso de falha HTTP.</returns>
    Task<bool> SendTextMessageAsync(Guid tenantId, string phone, string message, CancellationToken ct = default);
}
