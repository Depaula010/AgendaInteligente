namespace AgendaInteligente.Api.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um slot de horário solicitado está ocupado.
/// Encapsula uma lista de horários alternativos disponíveis próximos ao solicitado,
/// permitindo que a camada de apresentação (endpoint / bot) ofereça sugestões ativas ao cliente.
/// </summary>
public sealed class ScheduleConflictException : Exception
{
    /// <summary>
    /// Horários disponíveis (UTC) mais próximos ao slot solicitado, sugeridos como alternativas.
    /// A lista pode estar vazia se nenhuma alternativa for encontrada dentro da janela de busca.
    /// </summary>
    public IReadOnlyList<DateTime> SuggestedAlternatives { get; }

    public ScheduleConflictException(
        string message,
        IReadOnlyList<DateTime> suggestedAlternatives)
        : base(message)
    {
        SuggestedAlternatives = suggestedAlternatives;
    }

    public ScheduleConflictException(
        string message,
        IReadOnlyList<DateTime> suggestedAlternatives,
        Exception innerException)
        : base(message, innerException)
    {
        SuggestedAlternatives = suggestedAlternatives;
    }
}
