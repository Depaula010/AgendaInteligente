using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaInteligente.Api.Contracts.Requests.Webhook;

public class WebhookMessageRequest
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    public string SenderPhone { get; set; } = string.Empty;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    [Required]
    public string MessageId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
