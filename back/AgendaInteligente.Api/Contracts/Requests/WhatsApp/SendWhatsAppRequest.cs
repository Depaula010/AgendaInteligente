using System.ComponentModel.DataAnnotations;

namespace AgendaInteligente.Api.Contracts.Requests.WhatsApp;

public sealed class SendWhatsAppRequest
{
    /// <summary>Número de destino em formato E.164 (ex: 5511999999999).</summary>
    [Required]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Texto da mensagem a enviar.</summary>
    [Required]
    public string Message { get; set; } = string.Empty;
}
