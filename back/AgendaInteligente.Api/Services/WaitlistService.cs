using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// Implementa a lógica de negócio da Lista de Espera Inteligente (BUSINESS_RULES.md §3).
/// 
/// Quando um agendamento é cancelado, o ScheduleService chama ProcessCancellationAsync.
/// Este serviço:
///   1. Busca no repositório os clientes em espera para a data/profissional cancelados (FIFO).
///   2. Para cada cliente encontrado, atualiza o status para Notified e aciona a notificação WhatsApp.
///   3. O primeiro cliente a confirmar (fora do escopo desta classe) ganha a vaga.
/// </summary>
public sealed class WaitlistService : IWaitlistService
{
    private readonly IWaitlistRepository            _waitlistRepo;
    private readonly IWhatsAppNotificationService   _whatsAppNotifier;
    private readonly ILogger<WaitlistService>       _logger;

    public WaitlistService(
        IWaitlistRepository waitlistRepo,
        IWhatsAppNotificationService whatsAppNotifier,
        ILogger<WaitlistService> logger)
    {
        _waitlistRepo     = waitlistRepo;
        _whatsAppNotifier = whatsAppNotifier;
        _logger           = logger;
    }

    /// <inheritdoc/>
    public async Task ProcessCancellationAsync(
        Guid tenantId,
        Guid professionalId,
        DateTime freedSlotStart,
        DateTime freedSlotEnd,
        CancellationToken ct = default)
    {
        var freedDate = DateOnly.FromDateTime(freedSlotStart.Date);

        _logger.LogInformation(
            "Processando lista de espera para cancelamento: Profissional={ProfessionalId}, " +
            "Slot={SlotStart}–{SlotEnd}",
            professionalId, freedSlotStart, freedSlotEnd);

        IReadOnlyList<Domain.Entities.Waitlist> pendingEntries;

        try
        {
            pendingEntries = await _waitlistRepo.GetPendingByDateAsync(freedDate, professionalId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao buscar lista de espera para o slot {SlotStart}. " +
                "O cancelamento foi concluído, mas as notificações não foram enviadas.",
                freedSlotStart);
            return; // Falha na notificação não deve reverter o cancelamento
        }

        if (pendingEntries.Count == 0)
        {
            _logger.LogInformation(
                "Nenhum cliente na lista de espera para o slot {SlotStart}.", freedSlotStart);
            return;
        }

        _logger.LogInformation(
            "{Count} cliente(s) na lista de espera para o slot {SlotStart}. Enviando notificações.",
            pendingEntries.Count, freedSlotStart);

        foreach (var entry in pendingEntries)
        {
            try
            {
                // Atualiza o status antes de enviar para evitar notificações duplicadas
                // em caso de reprocessamento
                entry.Status     = WaitlistStatus.Notified;
                entry.NotifiedAt = DateTime.UtcNow;
                await _waitlistRepo.UpdateAsync(entry, ct);

                var customerName  = entry.Customer?.Name         ?? "Cliente";
                var customerPhone = entry.Customer?.PhoneNumber  ?? string.Empty;

                if (string.IsNullOrWhiteSpace(customerPhone))
                {
                    _logger.LogWarning(
                        "Entrada da waitlist {WaitlistId} não possui telefone do cliente. " +
                        "Notificação ignorada.", entry.Id);
                    continue;
                }

                await _whatsAppNotifier.SendWaitlistNotificationAsync(
                    tenantId,
                    customerPhone,
                    customerName,
                    freedSlotStart,
                    professionalName: "o profissional",
                    ct);

                _logger.LogInformation(
                    "Notificação de vaga enviada para o cliente {CustomerId} (fila: {WaitlistId}).",
                    entry.CustomerId, entry.Id);
            }
            catch (Exception ex)
            {
                // Falha em uma notificação não deve impedir as demais
                _logger.LogError(ex,
                    "Falha ao notificar cliente {CustomerId} (fila: {WaitlistId}). " +
                    "Continuando com os próximos da lista.",
                    entry.CustomerId, entry.Id);
            }
        }
    }
}
