using System.Security.Cryptography;
using System.Text;
using AgendaInteligente.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AgendaInteligente.Api.Filters;

/// <summary>
/// Valida a assinatura HMAC-SHA256 enviada pelo bot Node.js no header X-Webhook-Signature.
/// O bot gera a assinatura com a chave compartilhada em WebhookSignatureKey (B36).
/// Se a chave não estiver configurada, a validação é ignorada (modo desenvolvimento).
/// </summary>
public sealed class WebhookHmacFilter : IEndpointFilter
{
    private const string SignatureHeader = "X-Webhook-Signature";

    private readonly string _signatureKey;
    private readonly ILogger<WebhookHmacFilter> _logger;

    public WebhookHmacFilter(IOptions<WhatsAppBotOptions> options, ILogger<WebhookHmacFilter> logger)
    {
        _signatureKey = options.Value.WebhookSignatureKey;
        _logger       = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Sem chave configurada: pular validação (ambiente de desenvolvimento)
        if (string.IsNullOrWhiteSpace(_signatureKey))
            return await next(context);

        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(SignatureHeader, out var receivedSignature) ||
            string.IsNullOrWhiteSpace(receivedSignature))
        {
            _logger.LogWarning("Webhook recebido sem header {Header}.", SignatureHeader);
            return Results.Unauthorized();
        }

        // Habilita leitura múltipla do body (necessário porque o model binder também o lê)
        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        var expectedSignature = ComputeHmac(body, _signatureKey);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(receivedSignature.ToString()),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            _logger.LogWarning("Assinatura HMAC do webhook inválida. Header recebido não confere.");
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static string ComputeHmac(string body, string key)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash      = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
