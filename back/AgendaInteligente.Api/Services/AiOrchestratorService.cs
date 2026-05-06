using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace AgendaInteligente.Api.Services;

public sealed class AiOrchestratorService : IAiOrchestratorService
{
    private readonly IGeminiService _geminiService;
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ILogger<AiOrchestratorService> _logger;
    private readonly GeminiOptions _geminiOptions;

    public AiOrchestratorService(
        IGeminiService geminiService,
        ITenantSettingsRepository tenantSettingsRepository,
        IScheduleRepository scheduleRepository,
        IOptions<GeminiOptions> geminiOptions,
        ILogger<AiOrchestratorService> logger)
    {
        _geminiService = geminiService;
        _tenantSettingsRepository = tenantSettingsRepository;
        _scheduleRepository = scheduleRepository;
        _logger = logger;
        _geminiOptions = geminiOptions.Value;
    }

    public async Task<GeminiIntentResponse> ProcessUserMessageAsync(Guid tenantId, string userMessage, List<MessageHistory> history)
    {
        var settings = await _tenantSettingsRepository.GetAsync();
        
        if (settings == null)
            throw new BusinessException("Configurações do estabelecimento não encontradas. O tenant pode estar inativo.");

        // Define a API Key (Tenant Key -> Global Key fallback)
        var apiKey = string.IsNullOrWhiteSpace(settings.GeminiApiKey) 
            ? _geminiOptions.GlobalApiKey 
            : settings.GeminiApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BusinessException("API Key do Gemini não está configurada para este estabelecimento e não há fallback global.");

        // Montagem do Contexto Dinâmico
        var sb = new StringBuilder();
        sb.AppendLine($"Você é um assistente virtual de agendamento por WhatsApp da empresa '{settings.BotDisplayName ?? "Barbearia"}'.");
        sb.AppendLine("Sua função é conversar com o cliente de forma educada, curta (estilo WhatsApp) e objetiva, extraindo a sua intenção de agendamento.");
        sb.AppendLine();
        sb.AppendLine("### REGRAS DO ESTABELECIMENTO");
        sb.AppendLine($"- Horários de funcionamento (JSON): {settings.WorkingHoursJson}");
        sb.AppendLine($"- Dias de folga/feriados (JSON): {settings.DaysOffJson}");
        sb.AppendLine();
        sb.AppendLine("### SUA MISSÃO");
        sb.AppendLine("Analise o histórico e a mensagem atual do cliente. Retorne no JSON estruturado:");
        sb.AppendLine("1. intent: 'schedule' (quer marcar), 'cancel' (quer cancelar), 'reschedule' (remarcar), 'check' (dúvidas de agenda) ou 'general'.");
        sb.AppendLine("2. date: A data desejada (YYYY-MM-DD), se houver.");
        sb.AppendLine("3. time: A hora desejada (HH:MM), se houver.");
        sb.AppendLine("4. service: O serviço solicitado (ex: Corte, Barba), se houver.");
        sb.AppendLine("5. professional: O profissional solicitado, se houver.");
        sb.AppendLine("6. reply_message: A mensagem exata que você quer que o Bot envie de volta para o cliente neste momento.");
        sb.AppendLine();
        sb.AppendLine("Seja amigável na sua reply_message e procure tirar apenas UMA dúvida por vez. Se não souber a intenção, use intent 'general' e converse normalmente.");

        var systemPrompt = sb.ToString();

        _logger.LogInformation("Gerado Contexto Dinâmico para Tenant {TenantId}. Chamando Gemini.", tenantId);

        var result = await _geminiService.ExtractIntentAsync(systemPrompt, userMessage, history, apiKey, settings.GeminiModel);

        return result;
    }
}
