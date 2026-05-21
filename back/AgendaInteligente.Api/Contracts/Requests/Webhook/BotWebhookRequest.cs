namespace AgendaInteligente.Api.Contracts.Requests.Webhook;

public sealed class BotWebhookRequest
{
    public string Texto { get; set; } = string.Empty;
    public string NumeroRemetente { get; set; } = string.Empty;
}
