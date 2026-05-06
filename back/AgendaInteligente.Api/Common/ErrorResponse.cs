namespace AgendaInteligente.Api.Common;

public class ErrorResponse
{
    public string Message { get; }
    public string ErrorCode { get; }
    public string? Details { get; }

    public ErrorResponse(string message, string errorCode, string? details = null)
    {
        Message = message;
        ErrorCode = errorCode;
        Details = details;
    }
}
