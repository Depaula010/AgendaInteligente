using AgendaInteligente.Api.Contracts.Auth;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
                       .WithTags("Auth");

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            [FromServices] IAuthService authService,
            CancellationToken ct) =>
        {
            try
            {
                var response = await authService.LoginAsync(request, ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("Login")
        .AllowAnonymous();

        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            [FromServices] IPasswordResetService svc,
            CancellationToken ct) =>
        {
            await svc.ForgotPasswordAsync(request.Email, ct);
            return Results.Ok(new { message = "Se o e-mail estiver cadastrado, você receberá as instruções em breve." });
        })
        .WithName("ForgotPassword")
        .AllowAnonymous();

        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            [FromServices] IPasswordResetService svc,
            CancellationToken ct) =>
        {
            try
            {
                await svc.ResetPasswordAsync(request.Token, request.NewPassword, ct);
                return Results.Ok(new { message = "Senha redefinida com sucesso." });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ResetPassword")
        .AllowAnonymous();
    }
}
