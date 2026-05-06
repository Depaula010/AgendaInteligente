namespace AgendaInteligente.Api.Domain.Exceptions;

public class GeminiIntegrationException : Exception
{
    public GeminiIntegrationException(string message) : base(message)
    {
    }

    public GeminiIntegrationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
