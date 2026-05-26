using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AgendaInteligente.Api.Filters;

public class ApiKeyAuthFilter : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthFilter> _logger;

    public ApiKeyAuthFilter(IConfiguration configuration, ILogger<ApiKeyAuthFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("Tentativa de acesso ao Webhook sem a chave X-Api-Key.");
            return Results.Unauthorized();
        }

        var configuredApiKey = _configuration.GetValue<string>("WebhookSettings:ApiKey");

        if (string.IsNullOrEmpty(configuredApiKey) || !string.Equals(extractedApiKey.ToString(), configuredApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Tentativa de acesso ao Webhook com a chave X-Api-Key inválida.");
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
