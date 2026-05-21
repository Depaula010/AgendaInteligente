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
    private readonly ILogger<BotIntentDispatcherService> _logger;

    public BotIntentDispatcherService(
        ICustomerRepository customerRepo,
        IServiceCatalogRepository serviceRepo,
        IProfessionalRepository professionalRepo,
        IScheduleRepository scheduleRepo,
        IScheduleService scheduleService,
        ITenantSettingsRepository settingsRepo,
        ILogger<BotIntentDispatcherService> logger)
    {
        _customerRepo     = customerRepo;
        _serviceRepo      = serviceRepo;
        _professionalRepo = professionalRepo;
        _scheduleRepo     = scheduleRepo;
        _scheduleService  = scheduleService;
        _settingsRepo     = settingsRepo;
        _logger           = logger;
    }

    public Task<string> DispatchAsync(
        GeminiIntentResponse aiResponse,
        Guid tenantId,
        string senderPhone,
        CancellationToken ct = default)
    {
        return aiResponse.Intent switch
        {
            "schedule" => HandleScheduleAsync(aiResponse, tenantId, senderPhone, ct),
            "cancel"   => HandleCancelAsync(tenantId, senderPhone, ct),
            _          => Task.FromResult(aiResponse.ReplyMessage)
        };
    }

    // ── schedule intent ──────────────────────────────────────────────────────────

    private async Task<string> HandleScheduleAsync(
        GeminiIntentResponse aiResponse, Guid tenantId, string senderPhone, CancellationToken ct)
    {
        // Guard: date+time devem estar presentes para criar o agendamento
        if (string.IsNullOrWhiteSpace(aiResponse.Date) || string.IsNullOrWhiteSpace(aiResponse.Time))
            return aiResponse.ReplyMessage;

        // Guard: serviço deve estar presente
        if (string.IsNullOrWhiteSpace(aiResponse.Service))
            return aiResponse.ReplyMessage;

        // Parse da data/hora como UTC
        if (!DateTime.TryParseExact(
            $"{aiResponse.Date} {aiResponse.Time}",
            "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var startDateTime))
        {
            _logger.LogWarning("Falha ao parsear data/hora da IA. Date={Date}, Time={Time}", aiResponse.Date, aiResponse.Time);
            return aiResponse.ReplyMessage;
        }

        // Resolve serviço por nome (contém, case-insensitive)
        var services = await _serviceRepo.GetAllActiveAsync(ct);
        var service = services.FirstOrDefault(s =>
            s.Name.Contains(aiResponse.Service, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            _logger.LogInformation("Serviço '{Service}' não encontrado no catálogo ativo.", aiResponse.Service);
            return aiResponse.ReplyMessage;
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
            return aiResponse.ReplyMessage;
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

            return aiResponse.ReplyMessage;
        }
        catch (ScheduleConflictException ex)
        {
            _logger.LogInformation(
                "Conflito de horário via bot. Phone={Phone}, Start={Start}. Alternativas={Count}",
                senderPhone, startDateTime, ex.SuggestedAlternatives.Count);

            return await BuildConflictMessageAsync(ex, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro inesperado ao criar agendamento via bot. TenantId={TenantId}, Phone={Phone}",
                tenantId, senderPhone);

            return "Ops! Ocorreu um erro ao tentar criar seu agendamento. Por favor, tente novamente em instantes.";
        }
    }

    // ── cancel intent ────────────────────────────────────────────────────────────

    private async Task<string> HandleCancelAsync(Guid tenantId, string senderPhone, CancellationToken ct)
    {
        var customer = await _customerRepo.GetByPhoneAndTenantAsync(senderPhone, tenantId, ct);
        if (customer is null)
            return "Não encontrei nenhum agendamento pendente ou confirmado para você.";

        var upcoming = await _scheduleRepo.GetUpcomingByCustomerIdAsync(customer.Id, ct);
        if (upcoming.Count == 0)
            return "Não encontrei nenhum agendamento pendente ou confirmado para você.";

        var next        = upcoming[0];
        var serviceName = next.Service?.Name;
        await _scheduleService.UpdateStatusAsync(next.Id, ScheduleStatus.Cancelled, ct);

        _logger.LogInformation(
            "Agendamento cancelado via bot. ScheduleId={Id}, TenantId={TenantId}, Phone={Phone}",
            next.Id, tenantId, senderPhone);

        return $"Seu agendamento{(serviceName is not null ? $" de {serviceName}" : "")} do dia " +
               $"{next.StartDateTime:dd/MM/yyyy} às {next.StartDateTime:HH:mm} foi cancelado com sucesso! " +
               "Se quiser remarcar, é só me avisar.";
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private async Task<string> BuildConflictMessageAsync(ScheduleConflictException ex, CancellationToken ct)
    {
        if (ex.SuggestedAlternatives.Count == 0)
            return NoAlternativesMessage;

        var settings = await _settingsRepo.GetAsync(ct);
        var template = !string.IsNullOrWhiteSpace(settings?.ConflictMessageTemplate)
            ? settings.ConflictMessageTemplate
            : DefaultConflictTemplate;

        var alternativesText = string.Join("\n",
            ex.SuggestedAlternatives.Select(alt => $"• {alt:dd/MM/yyyy} às {alt:HH:mm}"));

        return template.Replace("{alternatives}", alternativesText);
    }
}
