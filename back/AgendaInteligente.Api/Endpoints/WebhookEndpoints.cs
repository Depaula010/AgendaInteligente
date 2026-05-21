using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Filters;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhooks")
            .WithTags("Webhooks")
            .AddEndpointFilter<ApiKeyAuthFilter>();

        group.MapPost("/whatsapp/{tenantId:guid}", ProcessWhatsAppWebhookAsync)
            .WithName("ProcessWhatsAppWebhook")
            .WithSummary("Recebe mensagens do WhatsApp via bot Node.js")
            .WithDescription(
                "Endpoint protegido por X-Api-Key. Recebe { texto, numero_remetente } do bot, " +
                "processa via IA e retorna { resposta } para o bot encaminhar ao usuário. " +
                "O tenantId é embutido na URL de webhook configurada durante a criação da sessão.")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> ProcessWhatsAppWebhookAsync(
        [FromRoute] Guid tenantId,
        [FromBody] BotWebhookRequest request,
        [FromServices] IWebhookService webhookService,
        CancellationToken ct)
    {
        try
        {
            var reply = await webhookService.ProcessWhatsAppMessageAsync(tenantId, request, ct);
            return Results.Ok(new { resposta = reply });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
