namespace AgendaInteligente.Api.Domain.Enums;

/// <summary>
/// Representa o estado de uma entrada na lista de espera.
/// </summary>
public enum WaitlistStatus
{
    /// <summary>Na fila, aguardando a abertura de uma vaga.</summary>
    Waiting = 0,

    /// <summary>
    /// O sistema notificou o cliente que surgiu uma vaga. 
    /// Aguardando resposta de confirmação.
    /// </summary>
    Notified = 1,

    /// <summary>O cliente confirmou e a vaga foi convertida em agendamento.</summary>
    Converted = 2,

    /// <summary>O cliente não respondeu ou recusou a vaga.</summary>
    Expired = 3
}
