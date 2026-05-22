using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ICustomerRepository repo, CancellationToken ct) =>
        {
            var customer = await repo.GetByIdAsync(id, ct);
            if (customer is null)
                return Results.NotFound();

            return Results.Ok(ToResponse(customer));
        });

        group.MapGet("/", async ([FromQuery] string? phone, ICustomerRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Results.BadRequest(new { error = "O parâmetro 'phone' é obrigatório." });

            var customer = await repo.GetByPhoneAsync(phone, ct);
            if (customer is null)
                return Results.NotFound();

            return Results.Ok(ToResponse(customer));
        });

        group.MapPost("/", async ([FromBody] CreateCustomerRequest request, ICustomerRepository repo, CancellationToken ct) =>
        {
            var existing = await repo.GetByPhoneAsync(request.PhoneNumber, ct);
            if (existing is not null)
                return Results.Ok(ToResponse(existing));

            var customer = new Customer
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
            };

            var created = await repo.CreateAsync(customer, ct);
            return Results.Created($"/api/v1/customers/{created.Id}", ToResponse(created));
        });

        // ── Listagem paginada com busca ────────────────────────────────────────
        group.MapGet("/list", async (
            [FromQuery] string? search,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ICustomerRepository repo,
            CancellationToken ct) =>
        {
            var safePage     = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 100);
            var skip         = (safePage - 1) * safePageSize;

            var items = await repo.GetPagedAsync(search, skip, safePageSize, ct);
            var total = await repo.CountAsync(search, ct);

            return Results.Ok(new
            {
                items    = items.Select(ToResponse),
                total,
                page     = safePage,
                pageSize = safePageSize,
            });
        });

        // ── Histórico de agendamentos do cliente ───────────────────────────────
        group.MapGet("/{id:guid}/schedules", async (
            Guid id,
            [FromServices] IScheduleRepository scheduleRepo,
            CancellationToken ct) =>
        {
            var schedules = await scheduleRepo.GetAllByCustomerIdAsync(id, ct);
            var response  = schedules.Select(s => new ScheduleResponse(
                s.Id, s.CustomerId!.Value, s.ProfessionalId, s.ServiceId!.Value,
                s.StartDateTime, s.EndDateTime, s.Status, s.Notes, s.CreatedAt));
            return Results.Ok(response);
        });
    }

    private static CustomerResponse ToResponse(Customer c) =>
        new(c.Id, c.Name, c.PhoneNumber, c.Email, c.LastVisitAt);
}
