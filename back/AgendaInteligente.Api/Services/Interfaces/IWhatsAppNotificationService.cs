namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Abstração para envio de mensagens proativas via WhatsApp.
/// Permite que a camada de serviço dispare notificações sem conhecer os detalhes
/// do transporte (Node.js / Baileys). A implementação atual usa apenas logs;
/// a integração real com o bot Node.js será plugada nesta interface futuramente.
/// </summary>
public interface IWhatsAppNotificationService
{
    /// <summary>
    /// Envia uma notificação de vaga disponível ao cliente que estava na lista de espera.
    /// </summary>
    /// <param name="customerPhone">Número de telefone do destinatário (formato E.164, ex: +5511999999999).</param>
    /// <param name="customerName">Nome do cliente para personalizar a mensagem.</param>
    /// <param name="availableSlot">Data/hora do horário que ficou disponível (UTC).</param>
    /// <param name="professionalName">Nome do profissional que atenderá.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendWaitlistNotificationAsync(
        Guid tenantId,
        string customerPhone,
        string customerName,
        DateTime availableSlot,
        string professionalName,
        CancellationToken ct = default);
}
