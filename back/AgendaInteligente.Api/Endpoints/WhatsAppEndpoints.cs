using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Requests.WhatsApp;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AgendaInteligente.Api.Endpoints;

public static class WhatsAppEndpoints
{
    public static void MapWhatsAppEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/whatsapp")
            .RequireAuthorization("RequireOwnerRole")
            .WithTags("WhatsApp");

        group.MapPost("/send", SendMessageAsync)
            .WithName("SendWhatsAppMessage")
            .WithSummary("Envia mensagem de texto via WhatsApp")
            .WithDescription(
                "Envia uma mensagem de texto para um número de WhatsApp usando o bot Node.js (Baileys) " +
                "configurado para o tenant autenticado. Requer role Owner.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    public static async Task<IResult> SendMessageAsync(
        [FromBody] SendWhatsAppRequest request,
        [FromServices] IWhatsAppSendService sendService,
        [FromServices] ITenantProvider tenantProvider)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return Results.BadRequest(new ErrorResponse("Phone é obrigatório.", "Validation"));

        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest(new ErrorResponse("Message é obrigatório.", "Validation"));

        var tenantId = tenantProvider.CurrentTenantId;
        if (!tenantId.HasValue)
            return Results.Unauthorized();

        var sent = await sendService.SendTextMessageAsync(tenantId.Value, request.Phone, request.Message);

        if (!sent)
            return Results.Problem(
                title: "Falha ao enviar mensagem",
                detail: "O bot WhatsApp não está disponível no momento. Tente novamente em instantes.",
                statusCode: StatusCodes.Status502BadGateway);

        return Results.Ok(new SendWhatsAppResponse(true));
    }
}

public record SendWhatsAppResponse(bool Sent);
