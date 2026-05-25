using AgendaInteligente.Api.Contracts.Models;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AgendaInteligente.Api.Services;

public sealed class BotIntentDispatcherService : IBotIntentDispatcherService
{
    // Usado quando TenantSettings.ConflictMessageTemplate é nulo.
    // O placeholder {alternatives} é substituído pela lista formatada de horários.
    internal const string DefaultConflictTemplate =
        "Esse horário está ocupado. Horários disponíveis:\n{alternatives}\nQual desses horários prefere?";

    private const string NoAlternativesMessage =
        "Esse horário está ocupado. Não encontrei horários disponíveis nos próximos dias. Tente outra data.";

    private readonly ICustomerRepository         _customerRepo;
    private readonly IServiceCatalogRepository   _serviceRepo;
    private readonly IProfessionalRepository     _professionalRepo;
    private readonly IScheduleRepository         _scheduleRepo;
    private readonly IScheduleService            _scheduleService;
    private readonly ITenantSettingsRepository   _settingsRepo;
    private readonly IWebPushService             _webPushService;
    private readonly ILogger<BotIntentDispatcherService> _logger;

    public BotIntentDispatcherService(
        ICustomerRepository customerRepo,
        IServiceCatalogRepository serviceRepo,
        IProfessionalRepository professionalRepo,
        IScheduleRepository scheduleRepo,
        IScheduleService scheduleService,
        ITenantSettingsRepository settingsRepo,
        IWebPushService webPushService,
        ILogger<BotIntentDispatcherService> logger)
    {
        _customerRepo     = customerRepo;
        _serviceRepo      = serviceRepo;
        _professionalRepo = professionalRepo;
        _scheduleRepo     = scheduleRepo;
        _scheduleService  = scheduleService;
        _settingsRepo     = settingsRepo;
        _webPushService   = webPushService;
        _logger           = logger;
    }

    public Task<BotReply> DispatchAsync(
        GeminiIntentResponse aiResponse,
        Guid tenantId,
        string senderPhone,
        CancellationToken ct = default)
    {
        return aiResponse.Intent switch
        {
            "schedule"   => HandleScheduleAsync(aiResponse, tenantId, senderPhone, ct),
            "cancel"     => HandleCancelAsync(tenantId, senderPhone, ct),
            "reschedule" => HandleRescheduleAsync(aiResponse, tenantId, senderPhone, ct),
            _            => Task.FromResult(BotReply.FromText(aiResponse.ReplyMessage))
        };
    }

    // ── schedule intent ──────────────────────────────────────────────────────────

    private async Task<BotReply> HandleScheduleAsync(
        GeminiIntentResponse aiResponse, Guid tenantId, string senderPhone, CancellationToken ct)
    {
        // Guard: date+time devem estar presentes para criar o agendamento
        if (string.IsNullOrWhiteSpace(aiResponse.Date) || string.IsNullOrWhiteSpace(aiResponse.Time))
            return BotReply.FromText(aiResponse.ReplyMessage);

        // Guard: serviço deve estar presente
        if (string.IsNullOrWhiteSpace(aiResponse.Service))
            return BotReply.FromText(aiResponse.ReplyMessage);

        // Parse da data/hora como UTC
        if (!DateTime.TryParseExact(
            $"{aiResponse.Date} {aiResponse.Time}",
            "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var startDateTime))
        {
            _logger.LogWarning("Falha ao parsear data/hora da IA. Date={Date}, Time={Time}", aiResponse.Date, aiResponse.Time);
            return BotReply.FromText(aiResponse.ReplyMessage);
        }

        // Resolve serviço por nome (contém, case-insensitive)
        var services = await _serviceRepo.GetAllActiveAsync(ct);
        var service = services.FirstOrDefault(s =>
            s.Name.Contains(aiResponse.Service, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            _logger.LogInformation("Serviço '{Service}' não encontrado no catálogo ativo.", aiResponse.Service);
            return BotReply.FromText(aiResponse.ReplyMessage);
        }

        // Resolve profissional por nome ou usa o primeiro disponível
        var professionals = await _professionalRepo.GetAllActiveAsync(ct);
        Professional? professional = null;
        if (!string.IsNullOrWhiteSpace(aiResponse.Professional))
        {
            professional = professionals.FirstOrDefault(p =>
                p.Name.Contains(aiResponse.Professional, StringComparison.OrdinalIgnoreCase));
        }
        professional ??= professionals.FirstOrDefault();

        if (professional is null)
        {
            _logger.LogWarning("Nenhum profissional ativo encontrado para TenantId={TenantId}.", tenantId);
            return BotReply.FromText(aiResponse.ReplyMessage);
        }

        // Find-or-create customer pelo telefone (IgnoreQueryFilters — sem JWT no webhook path)
        var customer = await _customerRepo.GetByPhoneAndTenantAsync(senderPhone, tenantId, ct)
            ?? await _customerRepo.CreateAsync(new Customer
            {
                Name        = senderPhone,
                PhoneNumber = senderPhone,
                TenantId    = tenantId
            }, ct);

        try
        {
            await _scheduleService.CreateAsync(
                customer.Id, professional.Id, service.Id, startDateTime, ct: ct);

            _logger.LogInformation(
                "Agendamento criado via bot. TenantId={TenantId}, Phone={Phone}, Service={Service}, Start={Start}",
                tenantId, senderPhone, service.Name, startDateTime);

            var pushBody = $"{customer.Name} · {service.Name} · {startDateTime:dd/MM} às {startDateTime:HH:mm}";
            await _webPushService.NotifyAsync(professional.Id, "Novo agendamento", pushBody, ct);

            return BotReply.FromText(aiResponse.ReplyMessage);
        }
        catch (ScheduleConflictException ex)
        {
            _logger.LogInformation(
                "Conflito de horário via bot. Phone={Phone}, Start={Start}. Alternativas={Count}",
                senderPhone, startDateTime, ex.SuggestedAlternatives.Count);

            return await BuildConflictReplyAsync(ex, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro inesperado ao criar agendamento via bot. TenantId={TenantId}, Phone={Phone}",
                tenantId, senderPhone);

            return BotReply.FromText("Ops! Ocorreu um erro ao tentar criar seu agendamento. Por favor, tente novamente em instantes.");
        }
    }

    // ── cancel intent ────────────────────────────────────────────────────────────

    private async Task<BotReply> HandleCancelAsync(Guid tenantId, string senderPhone, CancellationToken ct)
    {
        var customer = await _customerRepo.GetByPhoneAndTenantAsync(senderPhone, tenantId, ct);
        if (customer is null)
            return BotReply.FromText("Não encontrei nenhum agendamento pendente ou confirmado para você.");

        var upcoming = await _scheduleRepo.GetUpcomingByCustomerIdAsync(customer.Id, ct);
        if (upcoming.Count == 0)
            return BotReply.FromText("Não encontrei nenhum agendamento pendente ou confirmado para você.");

        var next        = upcoming[0];
        var serviceName = next.Service?.Name;
        await _scheduleService.UpdateStatusAsync(next.Id, ScheduleStatus.Cancelled, ct);

        _logger.LogInformation(
            "Agendamento cancelado via bot. ScheduleId={Id}, TenantId={TenantId}, Phone={Phone}",
            next.Id, tenantId, senderPhone);

        var cancelBody = $"{customer.Name}{(serviceName is not null ? $" · {serviceName}" : "")} · {next.StartDateTime:dd/MM} às {next.StartDateTime:HH:mm}";
        await _webPushService.NotifyAsync(next.ProfessionalId, "Agendamento cancelado", cancelBody, ct);

        return BotReply.FromText(
            $"Seu agendamento{(serviceName is not null ? $" de {serviceName}" : "")} do dia " +
            $"{next.StartDateTime:dd/MM/yyyy} às {next.StartDateTime:HH:mm} foi cancelado com sucesso! " +
            "Se quiser remarcar, é só me avisar.");
    }

    // ── reschedule intent ─────────────────────────────────────────────────────────

    private async Task<BotReply> HandleRescheduleAsync(
        GeminiIntentResponse aiResponse, Guid tenantId, string senderPhone, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(aiResponse.Date) || string.IsNullOrWhiteSpace(aiResponse.Time))
            return BotReply.FromText(aiResponse.ReplyMessage);

        if (!DateTime.TryParseExact(
            $"{aiResponse.Date} {aiResponse.Time}",
            "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var newStart))
        {
            _logger.LogWarning("Falha ao parsear data/hora para reagendamento. Date={Date}, Time={Time}", aiResponse.Date, aiResponse.Time);
            return BotReply.FromText(aiResponse.ReplyMessage);
        }

        var customer = await _customerRepo.GetByPhoneAndTenantAsync(senderPhone, tenantId, ct);
        if (customer is null)
            return BotReply.FromText("Não encontrei nenhum agendamento pendente ou confirmado para você.");

        var upcoming = await _scheduleRepo.GetUpcomingByCustomerIdAsync(customer.Id, ct);
        if (upcoming.Count == 0)
            return BotReply.FromText("Não encontrei nenhum agendamento pendente ou confirmado para você.");

        var existing = upcoming[0];

        // Resolve service: AI override → inherit from existing appointment
        var service = existing.Service;
        if (!string.IsNullOrWhiteSpace(aiResponse.Service))
        {
            var allServices = await _serviceRepo.GetAllActiveAsync(ct);
            service = allServices.FirstOrDefault(s =>
                s.Name.Contains(aiResponse.Service, StringComparison.OrdinalIgnoreCase))
                ?? service;
        }
        if (service is null)
        {
            _logger.LogWarning("Serviço não encontrado para reagendamento. TenantId={TenantId}", tenantId);
            return BotReply.FromText(aiResponse.ReplyMessage);
        }

        // Resolve professional: AI override → inherit from existing appointment
        var professional = existing.Professional;
        if (!string.IsNullOrWhiteSpace(aiResponse.Professional))
        {
            var allProfessionals = await _professionalRepo.GetAllActiveAsync(ct);
            professional = allProfessionals.FirstOrDefault(p =>
                p.Name.Contains(aiResponse.Professional, StringComparison.OrdinalIgnoreCase))
                ?? professional;
        }
        if (professional is null)
        {
            _logger.LogWarning("Profissional não encontrado para reagendamento. TenantId={TenantId}", tenantId);
            return BotReply.FromText(aiResponse.ReplyMessage);
        }

        // Create new appointment FIRST — only cancel old if successful
        try
        {
            await _scheduleService.CreateAsync(
                customer.Id, professional.Id, service.Id, newStart, ct: ct);
        }
        catch (ScheduleConflictException ex)
        {
            _logger.LogInformation(
                "Conflito de horário no reagendamento via bot. Phone={Phone}, NewStart={Start}. Alternativas={Count}",
                senderPhone, newStart, ex.SuggestedAlternatives.Count);
            return await BuildConflictReplyAsync(ex, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro inesperado ao criar novo agendamento no reagendamento. TenantId={TenantId}, Phone={Phone}",
                tenantId, senderPhone);
            return BotReply.FromText("Ops! Ocorreu um erro ao tentar criar seu novo agendamento. Por favor, tente novamente.");
        }

        await _scheduleService.UpdateStatusAsync(existing.Id, ScheduleStatus.Cancelled, ct);

        _logger.LogInformation(
            "Agendamento reagendado via bot. OldId={OldId}, TenantId={TenantId}, Phone={Phone}, NewStart={Start}",
            existing.Id, tenantId, senderPhone, newStart);

        return BotReply.FromText(
            $"Pronto! Seu agendamento de {service.Name} foi remarcado para " +
            $"{newStart:dd/MM/yyyy} às {newStart:HH:mm}. " +
            "Se precisar de mais alguma coisa, é só avisar!");
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private async Task<BotReply> BuildConflictReplyAsync(ScheduleConflictException ex, CancellationToken ct)
    {
        if (ex.SuggestedAlternatives.Count == 0)
            return BotReply.FromText(NoAlternativesMessage);

        var rows = ex.SuggestedAlternatives
            .Select(alt => new InteractiveSectionRow(
                RowId:       alt.ToString("yyyy-MM-dd HH:mm"),
                Title:       alt.ToString("dd/MM 'às' HH:mm"),
                Description: alt.ToString("dddd", new CultureInfo("pt-BR"))))
            .ToList();

        var payload = new InteractiveListPayload(
            Title:      "Horários disponíveis",
            Body:       "Esse horário está ocupado. Escolha uma das opções disponíveis:",
            ButtonText: "Ver horários",
            Sections:   new[] { new InteractiveSection("Próximas vagas", rows) });

        return BotReply.FromInteractiveList(payload);
    }
}
