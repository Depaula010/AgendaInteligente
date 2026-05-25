using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly IWhatsAppSendService               _sendService;
    private readonly ILogger<WhatsAppNotificationService> _logger;

    public WhatsAppNotificationService(
        IWhatsAppSendService sendService,
        ILogger<WhatsAppNotificationService> logger)
    {
        _sendService = sendService;
        _logger      = logger;
    }

    public async Task SendWaitlistNotificationAsync(
        Guid tenantId,
        string customerPhone,
        string customerName,
        DateTime availableSlot,
        string professionalName,
        CancellationToken ct = default)
    {
        var message =
            $"Olá, {customerName}! 🎉 Surgiu uma vaga em " +
            $"{availableSlot:dd/MM/yyyy} às {availableSlot:HH:mm} " +
            $"com {professionalName}. " +
            $"Você ainda tem interesse? Responda *SIM* para confirmar ou *NÃO* para desistir.";

        _logger.LogInformation(
            "Enviando notificação de vaga da waitlist para {Phone} (Tenant={TenantId}).",
            customerPhone, tenantId);

        var sent = await _sendService.SendTextMessageAsync(tenantId, customerPhone, message, ct);

        if (!sent)
            _logger.LogWarning(
                "Falha ao enviar notificação de waitlist para {Phone} (Tenant={TenantId}). " +
                "BotSessionId pode não estar configurado.",
                customerPhone, tenantId);
    }
}
