using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/schedules")
            .WithTags("Schedules")
            .RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Guid? professionalId,
            IScheduleService service, CancellationToken ct) =>
        {
            var fromDate = from ?? DateTime.UtcNow.Date;
            var toDate = to ?? fromDate.AddDays(7);

            var schedules = professionalId.HasValue
                ? await service.GetByProfessionalAsync(professionalId.Value, fromDate, toDate, ct)
                : await service.GetByDateRangeAsync(fromDate, toDate, ct);

            var response = schedules.Select(s => new ScheduleResponse(
                s.Id, s.CustomerId!.Value, s.ProfessionalId, s.ServiceId!.Value,
                s.StartDateTime, s.EndDateTime, s.Status, s.Notes, s.CreatedAt));

            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IScheduleService service, CancellationToken ct) =>
        {
            var schedule = await service.GetByIdAsync(id, ct);
            if (schedule is null)
                return Results.NotFound();

            var response = new ScheduleResponse(
                schedule.Id, schedule.CustomerId!.Value, schedule.ProfessionalId, schedule.ServiceId!.Value,
                schedule.StartDateTime, schedule.EndDateTime, schedule.Status, schedule.Notes, schedule.CreatedAt);

            return Results.Ok(response);
        });

        group.MapPost("/", async ([FromBody] CreateScheduleRequest request, IScheduleService service, CancellationToken ct) =>
        {
            try
            {
                var schedule = await service.CreateAsync(
                    request.CustomerId, request.ProfessionalId, request.ServiceId,
                    request.StartDateTime, request.Notes, ct);

                var response = new ScheduleResponse(
                    schedule.Id, schedule.CustomerId!.Value, schedule.ProfessionalId, schedule.ServiceId!.Value,
                    schedule.StartDateTime, schedule.EndDateTime, schedule.Status, schedule.Notes, schedule.CreatedAt);

                return Results.Created($"/api/v1/schedules/{schedule.Id}", response);
            }
            catch (ScheduleConflictException ex)
            {
                // HTTP 409 com lista de alternativas tipada — expõe SuggestedAlternatives ao cliente/bot
                return Results.Conflict(new
                {
                    error = ex.Message,
                    suggestedAlternatives = ex.SuggestedAlternatives
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateScheduleRequest request, IScheduleService service, CancellationToken ct) =>
        {
            try
            {
                var schedule = await service.UpdateAsync(
                    id, request.StartDateTime, request.Notes, ct);

                var response = new ScheduleResponse(
                    schedule.Id, schedule.CustomerId!.Value, schedule.ProfessionalId, schedule.ServiceId!.Value,
                    schedule.StartDateTime, schedule.EndDateTime, schedule.Status, schedule.Notes, schedule.CreatedAt);

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] UpdateScheduleStatusRequest request, IScheduleService service, CancellationToken ct) =>
        {
            var success = await service.UpdateStatusAsync(id, request.Status, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IScheduleService service, CancellationToken ct) =>
        {
            var success = await service.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });

        // ── Recurring schedules (B41) ─────────────────────────────────────────
        group.MapPost("/recurring", async (
            [FromBody] CreateRecurringScheduleRequest request,
            IScheduleService service,
            CancellationToken ct) =>
        {
            try
            {
                var created = await service.CreateRecurringAsync(
                    request.CustomerId, request.ProfessionalId, request.ServiceId,
                    request.StartDateTime, request.RepeatWeeklyCount, request.Notes, ct);

                var response = created.Select(s => new ScheduleResponse(
                    s.Id, s.CustomerId!.Value, s.ProfessionalId, s.ServiceId!.Value,
                    s.StartDateTime, s.EndDateTime, s.Status, s.Notes, s.CreatedAt));

                return Results.Ok(response);
            }
            catch (ScheduleConflictException ex)
            {
                return Results.Conflict(new
                {
                    error              = ex.Message,
                    conflictingDates   = ex.SuggestedAlternatives
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateRecurringSchedule")
        .WithSummary("Cria agendamentos semanais recorrentes")
        .Produces<IEnumerable<ScheduleResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // ── Available Slots (B26) ──────────────────────────────────────────────
        group.MapGet("/available", GetAvailableSlotsAsync)
            .WithName("GetAvailableSlots")
            .WithSummary("Retorna slots livres do dia para um profissional e serviço")
            .WithDescription(
                "Varre o horário comercial (08h–18h UTC) com granularidade de 30 min, " +
                "descartando slots com conflito de agendamento/folga e horários já passados. " +
                "Usado pelo dashboard para exibir o seletor de horários e pela IA para sugerir alternativas.")
            .Produces<AvailableSlotsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Blockouts (Folgas) ─────────────────────────────────────────────────
        var blockGroup = app.MapGroup("/api/v1/schedules/block")
            .WithTags("Schedules (Blockouts)")
            .RequireAuthorization();

        blockGroup.MapGet("/", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Guid professionalId,
            IScheduleService service, CancellationToken ct) =>
        {
            var fromDate = from ?? DateTime.UtcNow.Date;
            var toDate = to ?? fromDate.AddDays(7);

            var blockouts = await service.GetBlockoutsByProfessionalAsync(professionalId, fromDate, toDate, ct);
            var response = blockouts.Select(b => new BlockoutResponse(
                b.Id, b.ProfessionalId, b.StartDateTime, b.EndDateTime,
                b.BlockReason, b.IsAllDay, b.CreatedAt));

            return Results.Ok(response);
        });

        blockGroup.MapPost("/", async ([FromBody] CreateBlockoutRequest request, IScheduleService service, CancellationToken ct) =>
        {
            try
            {
                var blockout = await service.CreateBlockoutAsync(
                    request.ProfessionalId, request.StartDateTime, request.EndDateTime,
                    request.BlockReason, request.IsAllDay, ct);

                var response = new BlockoutResponse(
                    blockout.Id, blockout.ProfessionalId, blockout.StartDateTime, blockout.EndDateTime,
                    blockout.BlockReason, blockout.IsAllDay, blockout.CreatedAt);

                return Results.Created($"/api/v1/schedules/block/{blockout.Id}", response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization("RequireOwnerRole");

        blockGroup.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateBlockoutRequest request, IScheduleService service, CancellationToken ct) =>
        {
            try
            {
                var blockout = await service.UpdateBlockoutAsync(
                    id, request.StartDateTime, request.EndDateTime,
                    request.BlockReason, request.IsAllDay, ct);

                var response = new BlockoutResponse(
                    blockout.Id, blockout.ProfessionalId, blockout.StartDateTime, blockout.EndDateTime,
                    blockout.BlockReason, blockout.IsAllDay, blockout.CreatedAt);

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("RequireOwnerRole");

        blockGroup.MapDelete("/{id:guid}", async (Guid id, IScheduleService service, CancellationToken ct) =>
        {
            var success = await service.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("RequireOwnerRole");
    }

    // ── B26 handler ──────────────────────────────────────────────────────────────

    public static async Task<IResult> GetAvailableSlotsAsync(
        [FromQuery] Guid professionalId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly date,
        [FromServices] IScheduleService scheduleService,
        [FromServices] IServiceCatalogService catalogService,
        CancellationToken ct)
    {
        if (professionalId == Guid.Empty)
            return Results.BadRequest(new { error = "professionalId é obrigatório." });

        if (serviceId == Guid.Empty)
            return Results.BadRequest(new { error = "serviceId é obrigatório." });

        try
        {
            // Resolve duração antes de buscar slots — GetAvailableSlotsAsync também valida, mas
            // precisamos de DurationMinutes no response para o cliente exibir os horários.
            var svc = await catalogService.GetByIdAsync(serviceId, ct);
            if (svc is null)
                return Results.NotFound(new { error = $"Serviço '{serviceId}' não encontrado ou inativo." });

            var slots = await scheduleService.GetAvailableSlotsAsync(professionalId, serviceId, date, ct);

            return Results.Ok(new AvailableSlotsResponse(professionalId, serviceId, date, svc.DurationMinutes, slots));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}
