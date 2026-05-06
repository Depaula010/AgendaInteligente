using System.Text.Json.Serialization;

namespace AgendaInteligente.Api.Models.AI;

/// <summary>
/// Histórico de mensagens do chat do cliente
/// </summary>
public class MessageHistory
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // "user" ou "model"
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Estrutura da resposta esperada pela extração de intenções via Gemini
/// </summary>
public class GeminiIntentResponse
{
    /// <summary>
    /// Intenção identificada. Valores possíveis: "schedule" (agendar), "cancel" (cancelar), "reschedule" (reagendar), "check" (consultar horários/disponibilidade), "general" (dúvida geral)
    /// </summary>
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = "general";

    /// <summary>
    /// Data desejada extraída (formato YYYY-MM-DD). Nulo se não houver.
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// Hora desejada extraída (formato HH:MM). Nulo se não houver.
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// Nome do serviço extraído da mensagem. Nulo se não houver.
    /// </summary>
    [JsonPropertyName("service")]
    public string? Service { get; set; }
    
    /// <summary>
    /// Profissional desejado extraído da mensagem. Nulo se não houver.
    /// </summary>
    [JsonPropertyName("professional")]
    public string? Professional { get; set; }

    /// <summary>
    /// A resposta ou pergunta que a IA deseja repassar ao usuário final.
    /// Ex: "Para qual dia você deseja agendar o corte de cabelo?"
    /// </summary>
    [JsonPropertyName("reply_message")]
    public string ReplyMessage { get; set; } = string.Empty;
}
