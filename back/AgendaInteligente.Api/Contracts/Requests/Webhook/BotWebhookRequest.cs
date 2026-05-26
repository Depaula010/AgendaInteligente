using System.Text.Json.Serialization;

namespace AgendaInteligente.Api.Contracts.Requests.Webhook;

public sealed class BotWebhookRequest
{
    [JsonPropertyName("texto")]
    public string Texto { get; set; } = string.Empty;

    [JsonPropertyName("numero_remetente")]
    public string NumeroRemetente { get; set; } = string.Empty;
}
