namespace AgendaInteligente.Api.Configuration;

public sealed class WhatsAppBotOptions
{
    public const string SectionName = "WhatsAppBot";

    /// <summary>
    /// URL base do serviço Node.js (Baileys).
    /// Ex: http://localhost:3000
    /// Quando vazio, o sistema entra em modo stub e registra as mensagens via ILogger.
    /// </summary>
    public string BotUrl { get; set; } = string.Empty;

    /// <summary>
    /// API Key para autenticar chamadas ao bot Node.js (cabeçalho X-Api-Key).
    /// </summary>
    public string BotApiKey { get; set; } = string.Empty;

    /// <summary>
    /// URL pública base do backend, usada como webhook_url ao criar sessão no bot.
    /// Ex: https://api.meusite.com
    /// </summary>
    public string WebhookBackendUrl { get; set; } = string.Empty;

    /// <summary>
    /// Chave de assinatura compartilhada com o bot para validar webhooks recebidos (webhook_signature_key).
    /// </summary>
    public string WebhookSignatureKey { get; set; } = string.Empty;
}
