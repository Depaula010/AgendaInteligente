namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Contrato do serviço de Lista de Espera Inteligente.
/// Responsável por acionar as notificações proativas quando um cancelamento libera uma vaga.
/// </summary>
public interface IWaitlistService
{
    /// <summary>
    /// Verifica se há clientes na lista de espera para o slot que ficou disponível
    /// após um cancelamento e, em caso afirmativo, envia notificações proativas via WhatsApp.
    /// 
    /// O método é resiliente: exceções internas não propagam para o chamador,
    /// garantindo que uma falha na notificação não reverta o cancelamento original.
    /// </summary>
    /// <param name="professionalId">Profissional cujo horário foi cancelado.</param>
    /// <param name="freedSlotStart">Início do horário que ficou livre (UTC).</param>
    /// <param name="freedSlotEnd">Fim do horário que ficou livre (UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    Task ProcessCancellationAsync(
        Guid professionalId,
        DateTime freedSlotStart,
        DateTime freedSlotEnd,
        CancellationToken ct = default);
}
