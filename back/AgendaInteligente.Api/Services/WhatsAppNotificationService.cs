using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// Implementação stub de notificação via WhatsApp.
/// Registra a mensagem no log estruturado (Serilog/ILogger) para que a integração
/// possa ser validada sem o serviço Node.js ativo.
/// 
/// Quando o bot Node.js estiver configurado, substituir este log por uma chamada
/// HTTP POST para /api/v1/sessions/{id}/send-message, conforme ARCHITECTURE.md §3.2.
/// </summary>
public sealed class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly ILogger<WhatsAppNotificationService> _logger;

    public WhatsAppNotificationService(ILogger<WhatsAppNotificationService> logger)
        => _logger = logger;

    public Task SendWaitlistNotificationAsync(
        string customerPhone,
        string customerName,
        DateTime availableSlot,
        string professionalName,
        CancellationToken ct = default)
    {
        // Formata a mensagem exatamente como descrito em BUSINESS_RULES.md §3
        var message = $"Olá, {customerName}! Surgiu uma vaga agora em " +
                      $"{availableSlot:dd/MM/yyyy} às {availableSlot:HH:mm} UTC " +
                      $"com {professionalName}. " +
                      $"Você ainda tem interesse? Responda SIM para confirmar.";

        _logger.LogInformation(
            "[WHATSAPP-STUB] Notificação de vaga enviada para {Phone}. Mensagem: {Message}",
            customerPhone, message);

        // TODO: Substituir por chamada HTTP ao bot Node.js quando disponível.
        return Task.CompletedTask;
    }
}
