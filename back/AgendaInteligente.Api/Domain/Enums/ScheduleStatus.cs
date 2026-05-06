namespace AgendaInteligente.Api.Domain.Enums;

/// <summary>
/// Representa o status do ciclo de vida de um agendamento.
/// </summary>
public enum ScheduleStatus
{
    /// <summary>Aguardando confirmação do cliente ou do profissional.</summary>
    Pending = 0,

    /// <summary>Confirmado por ambas as partes ou automaticamente.</summary>
    Confirmed = 1,

    /// <summary>Cancelado pelo cliente ou pelo profissional.</summary>
    Cancelled = 2,

    /// <summary>Concluído — o atendimento foi realizado com sucesso.</summary>
    Completed = 3,

    /// <summary>No-show — o cliente não compareceu ao horário marcado.</summary>
    NoShow = 4
}
