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
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IServiceCatalogRepository _serviceCatalogRepository;
    private readonly ILogger<AiOrchestratorService> _logger;
    private readonly GeminiOptions _geminiOptions;

    public AiOrchestratorService(
        IGeminiService geminiService,
        ITenantSettingsRepository tenantSettingsRepository,
        IScheduleRepository scheduleRepository,
        IProfessionalRepository professionalRepository,
        IServiceCatalogRepository serviceCatalogRepository,
        IOptions<GeminiOptions> geminiOptions,
        ILogger<AiOrchestratorService> logger)
    {
        _geminiService = geminiService;
        _tenantSettingsRepository = tenantSettingsRepository;
        _scheduleRepository = scheduleRepository;
        _professionalRepository = professionalRepository;
        _serviceCatalogRepository = serviceCatalogRepository;
        _logger = logger;
        _geminiOptions = geminiOptions.Value;
    }

    public async Task<GeminiIntentResponse> ProcessUserMessageAsync(Guid tenantId, string userMessage, List<MessageHistory> history)
    {
        var settings = await _tenantSettingsRepository.GetByTenantIdAsync(tenantId);
        
        if (settings == null)
            throw new BusinessException("Configurações do estabelecimento não encontradas. O tenant pode estar inativo.");

        // Define a API Key (Tenant Key -> Global Key fallback)
        var apiKey = string.IsNullOrWhiteSpace(settings.GeminiApiKey) 
            ? _geminiOptions.GlobalApiKey 
            : settings.GeminiApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BusinessException("API Key do Gemini não está configurada para este estabelecimento e não há fallback global.");

        // Busca serviços e profissionais ativos para incluir no contexto
        var services      = await _serviceCatalogRepository.GetAllActiveByTenantAsync(tenantId);
        var professionals = await _professionalRepository.GetAllActiveByTenantAsync(tenantId);
        var professionalNames = professionals.Select(p => p.Name).ToList();

        // Data atual no fuso do tenant para o AI resolver referências relativas ("amanhã", "hoje")
        var tz   = TenantTimeZoneHelper.GetTimeZone(settings);
        var hoje = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        // Montagem do Contexto Dinâmico
        var sb = new StringBuilder();
        sb.AppendLine($"Você é um assistente virtual de agendamento por WhatsApp da empresa '{settings.BotDisplayName ?? "Barbearia"}'.");
        sb.AppendLine("Sua função é conversar com o cliente de forma educada, curta (estilo WhatsApp) e objetiva, extraindo a sua intenção de agendamento.");
        sb.AppendLine();
        sb.AppendLine("### DATA E HORA ATUAL");
        sb.AppendLine($"- Hoje é {hoje:dddd, dd/MM/yyyy} (horário local do estabelecimento).");
        sb.AppendLine("- Use esta data como referência para resolver expressões como 'hoje', 'amanhã', 'próxima semana', 'segunda-feira', etc.");
        sb.AppendLine("- Sempre retorne o campo 'date' no formato YYYY-MM-DD. Nunca deixe 'date' nulo se o cliente mencionou uma data relativa.");
        sb.AppendLine();
        sb.AppendLine("### REGRAS DO ESTABELECIMENTO");
        sb.AppendLine($"- Horários de funcionamento (JSON): {settings.WorkingHoursJson}");
        sb.AppendLine($"- Dias de folga/feriados (JSON): {settings.DaysOffJson}");
        sb.AppendLine();

        if (services.Count > 0)
        {
            sb.AppendLine("### SERVIÇOS DISPONÍVEIS");
            foreach (var svc in services)
                sb.AppendLine($"- {svc.Name} ({svc.DurationMinutes} min)");
            sb.AppendLine("- Retorne o campo 'service' com o nome EXATO de um dos serviços listados acima.");
            sb.AppendLine("- Se o cliente mencionou um serviço que corresponde a um da lista, use o nome exato. Se não mencionou nenhum → null.");
            sb.AppendLine();
        }

        if (professionalNames.Count == 1)
        {
            sb.AppendLine("### PROFISSIONAL");
            sb.AppendLine($"- Único profissional disponível: {professionalNames[0]}. Coloque sempre este nome no campo 'professional'.");
        }
        else if (professionalNames.Count > 1)
        {
            sb.AppendLine("### PROFISSIONAIS DISPONÍVEIS");
            sb.AppendLine($"- {string.Join(", ", professionalNames)}");
            sb.AppendLine("- Só preencha o campo 'professional' quando o cliente tiver escolhido explicitamente um nome. Caso contrário, deixe nulo.");
            sb.AppendLine("- O sistema verificará a disponibilidade de cada profissional e informará o cliente — você não precisa perguntar qual profissional está livre.");
        }

        sb.AppendLine();
        sb.AppendLine("### SUA MISSÃO");
        sb.AppendLine("Analise TODO o histórico da conversa + a mensagem atual. Retorne JSON com os campos abaixo.");
        sb.AppendLine("IMPORTANTE: preserve informações já dadas em mensagens anteriores (ex: se o serviço foi mencionado antes, mantenha-o).");
        sb.AppendLine();
        sb.AppendLine("#### CAMPO: intent");
        sb.AppendLine("- 'schedule'   → cliente quer marcar/agendar");
        sb.AppendLine("- 'cancel'     → cliente quer cancelar");
        sb.AppendLine("- 'reschedule' → cliente quer remarcar");
        sb.AppendLine("- 'check'      → cliente quer saber disponibilidade ou confirmar horário");
        sb.AppendLine("- 'general'    → qualquer outra coisa");
        sb.AppendLine();
        sb.AppendLine("#### CAMPO: date  (formato YYYY-MM-DD  |  null se não mencionado)");
        sb.AppendLine($"- 'hoje'                  → {hoje:yyyy-MM-dd}");
        sb.AppendLine($"- 'amanhã'                → {hoje.AddDays(1):yyyy-MM-dd}");
        sb.AppendLine($"- 'depois de amanhã'      → {hoje.AddDays(2):yyyy-MM-dd}");
        sb.AppendLine("- '26/05', 'dia 26', '26 de maio' → 2026-05-26");
        sb.AppendLine("- 'próxima segunda', 'segunda que vem' → calcule a data da próxima segunda-feira");
        sb.AppendLine("- Se não mencionou data → null  (NUNCA string vazia)");
        sb.AppendLine();
        sb.AppendLine("#### CAMPO: time  (formato HH:MM em 24h  |  null se não mencionado)");
        sb.AppendLine("- '14h', '14 horas', '14:00'     → '14:00'");
        sb.AppendLine("- '2 da tarde', 'duas da tarde'  → '14:00'");
        sb.AppendLine("- '9h', '9 da manhã', '09:00'    → '09:00'");
        sb.AppendLine("- 'meio-dia', '12h'              → '12:00'");
        sb.AppendLine("- 'meia-noite'                   → '00:00'");
        sb.AppendLine("- Se não mencionou hora → null  (NUNCA string vazia)");
        sb.AppendLine();
        sb.AppendLine("#### CAMPO: service  (nome do serviço  |  null se não mencionado)");
        sb.AppendLine("- Preserve do histórico se já foi dito antes.");
        sb.AppendLine("- Se o bot apresentou uma lista numerada no histórico e o cliente respondeu com um número (ex: '1', '2'), identifique o serviço correspondente pela posição na lista do histórico.");
        sb.AppendLine("- Se não mencionou → null  (NUNCA string vazia)");
        sb.AppendLine();
        sb.AppendLine("#### CAMPO: professional  (nome exato da lista de profissionais  |  null enquanto não escolhido)");
        sb.AppendLine("- Se o cliente disse 'Pedro' e existe 'Pedro Paulo' → retorne 'Pedro Paulo'.");
        sb.AppendLine("- Se o bot apresentou uma lista numerada de profissionais no histórico e o cliente respondeu com um número, identifique o profissional correspondente pela posição na lista.");
        sb.AppendLine("- Só preencha quando o cliente escolheu explicitamente. Caso contrário → null.");
        sb.AppendLine();
        sb.AppendLine("#### CAMPO: reply_message");
        sb.AppendLine("- A mensagem que o bot enviará ao cliente. Seja curto e amigável (estilo WhatsApp).");
        sb.AppendLine("- Tire apenas UMA dúvida por vez. Se não souber a intenção, use intent='general' e converse normalmente.");

        var systemPrompt = sb.ToString();

        _logger.LogInformation("Gerado Contexto Dinâmico para Tenant {TenantId}. Chamando Gemini.", tenantId);

        var result = await _geminiService.ExtractIntentAsync(systemPrompt, userMessage, history, apiKey, settings.GeminiModel);

        return result;
    }
}
