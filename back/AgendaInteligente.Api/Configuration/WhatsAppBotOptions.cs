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
}
