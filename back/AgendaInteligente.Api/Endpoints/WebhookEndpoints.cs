using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Filters;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendaInteligente.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhooks")
            .WithTags("Webhooks")
            .AddEndpointFilter<ApiKeyAuthFilter>()
            .AddEndpointFilter<WebhookHmacFilter>();

        group.MapPost("/whatsapp/{tenantId:guid}", ProcessWhatsAppWebhookAsync)
            .WithName("ProcessWhatsAppWebhook")
            .WithSummary("Recebe mensagens do WhatsApp via bot Node.js")
            .WithDescription(
                "Endpoint protegido por X-Api-Key + HMAC. Recebe { texto, numero_remetente } do bot. " +
                "Se Redis estiver disponível: publica no stream whatsapp:inbound e retorna 202 (processamento assíncrono). " +
                "Fallback síncrono quando Redis indisponível: processa via IA e retorna { resposta }.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("webhook-per-tenant");
    }

    private static async Task<IResult> ProcessWhatsAppWebhookAsync(
        [FromRoute] Guid tenantId,
        [FromBody] BotWebhookRequest request,
        [FromServices] IWebhookService webhookService,
        [FromServices] IRedisStreamService redisStreamService,
        CancellationToken ct)
    {
        try
        {
            if (redisStreamService.IsAvailable)
            {
                await redisStreamService.PublishInboundAsync(tenantId, request.NumeroRemetente, request.Texto, ct);
                return Results.Accepted(value: new { queued = true });
            }

            // Fallback síncrono: Redis indisponível — processa e responde diretamente
            var reply = await webhookService.ProcessWhatsAppMessageAsync(tenantId, request, ct);
            return Results.Ok(new { resposta = reply.Text });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
