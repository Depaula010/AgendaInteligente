using AgendaInteligente.Api.Domain.Interfaces;

namespace AgendaInteligente.Api.Domain.Entities;


/// <summary>
/// Armazena as configurações operacionais de um estabelecimento (Tenant).
/// Relação 1:1 com Tenant. Usado pelo Gemini para montar o prompt dinâmico.
/// </summary>
public sealed class TenantSettings : IMustHaveTenant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ── Multi-tenancy: relação 1:1 com Tenant ─────────────────────────────────
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Horários de Funcionamento ──────────────────────────────────────────────
    /// <summary>
    /// Horários de funcionamento armazenados como JSON.
    /// Formato esperado: array de objetos { DayOfWeek, OpenTime, CloseTime }.
    /// Ex: [{"dayOfWeek":1,"openTime":"09:00","closeTime":"18:00"},...]
    /// Simplifica o parse pelo Gemini e evita joins complexos na primeira versão.
    /// </summary>
    public string WorkingHoursJson { get; set; } = "[]";

    /// <summary>
    /// Dias de folga/feriados locais armazenados como JSON.
    /// Formato esperado: array de datas ISO 8601.
    /// Ex: ["2026-01-01","2026-12-25"]
    /// </summary>
    public string DaysOffJson { get; set; } = "[]";

    // ── Lembretes e Confirmações ───────────────────────────────────────────────
    /// <summary>
    /// Antecedência (em horas) para o envio do lembrete de confirmação via WhatsApp.
    /// Ex: 24 significa enviar 24h antes do horário agendado. 0 desativa o lembrete.
    /// </summary>
    public int ReminderLeadTimeHours { get; set; } = 24;

    // ── Reengajamento ──────────────────────────────────────────────────────────
    /// <summary>
    /// Número de dias sem agendamento para disparar a mensagem de reengajamento.
    /// Ex: 30 significa enviar uma mensagem proativa se o cliente não agendar em 30 dias.
    /// 0 desativa o reengajamento.
    /// </summary>
    public int ReengagementInactiveDays { get; set; } = 30;

    // ── Identidade Visual ──────────────────────────────────────────────────────
    /// <summary>
    /// Nome exibido nas mensagens do bot para o cliente.
    /// Ex: "Barbearia do Zé" ou "Studio Ana Lima".
    /// </summary>
    public string? BotDisplayName { get; set; }

    /// <summary>
    /// Número do WhatsApp Business vinculado ao Tenant.
    /// Formato E.164, ex: "+5511999998888".
    /// </summary>
    public string? WhatsAppPhoneNumber { get; set; }

    // ── Mensagens do Bot ───────────────────────────────────────────────────────
    /// <summary>
    /// Template da mensagem enviada ao cliente quando o horário solicitado está ocupado.
    /// Use o placeholder <c>{alternatives}</c> para injetar os horários alternativos formatados.
    /// Ex: "Esse horário está ocupado. Veja as opções:\n{alternatives}\nQual prefere?"
    /// Quando nulo, o sistema usa o template padrão definido em <see cref="BotIntentDispatcherService"/>.
    /// </summary>
    public string? ConflictMessageTemplate { get; set; }

    // ── Integração WhatsApp Bot ────────────────────────────────────────────────
    /// <summary>
    /// ID da sessão no bot Node.js (Baileys). Null até o WhatsApp ser vinculado via B24.
    /// </summary>
    public Guid? BotSessionId { get; set; }

    // ── Configurações de IA (Gemini) ───────────────────────────────────────────
    /// <summary>
    /// Chave da API do Gemini. Se nula, o sistema pode tentar usar uma chave global (fallback).
    /// </summary>
    public string? GeminiApiKey { get; set; }

    /// <summary>
    /// Modelo do Gemini a ser utilizado para as extrações de intenção.
    /// Ex: "gemini-2.5-flash-lite".
    /// </summary>
    public string GeminiModel { get; set; } = "gemini-2.5-flash-lite";

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
