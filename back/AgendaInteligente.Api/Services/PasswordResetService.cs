using System.Net;
using System.Net.Mail;
using System.Text;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly DistributedCacheEntryOptions TokenTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };

    private const string CachePrefix = "pwd-reset:";
    private const int MinPasswordLength = 6;

    private readonly IProfessionalRepository _professionalRepo;
    private readonly IDistributedCache       _cache;
    private readonly IConfiguration         _config;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IProfessionalRepository professionalRepo,
        IDistributedCache cache,
        IConfiguration config,
        ILogger<PasswordResetService> logger)
    {
        _professionalRepo = professionalRepo;
        _cache            = cache;
        _config           = config;
        _logger           = logger;
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        // Sempre retorna 200 para não vazar se o e-mail existe (anti-enumeração)
        var professional = await _professionalRepo.GetByEmailIgnoringQueryFilterAsync(email, ct);
        if (professional is null)
        {
            _logger.LogInformation("Forgot-password solicitado para e-mail não cadastrado: {Email}.", email);
            return;
        }

        var token    = Guid.NewGuid().ToString("N"); // 32 chars hex sem hífens
        var cacheKey = $"{CachePrefix}{token}";

        await _cache.SetStringAsync(cacheKey, professional.Id.ToString(), TokenTtl, ct);

        var appUrl  = _config.GetValue<string>("AppUrl") ?? "http://localhost:5173";
        var link    = $"{appUrl}/redefinir-senha?token={token}";

        await SendResetEmailAsync(email, professional.Name, link);

        _logger.LogInformation(
            "Token de reset gerado para profissional {ProfessionalId}. TTL=1h.", professional.Id);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token inválido.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
            throw new ArgumentException($"A senha deve ter no mínimo {MinPasswordLength} caracteres.");

        var cacheKey = $"{CachePrefix}{token}";
        var value    = await _cache.GetStringAsync(cacheKey, ct);

        if (value is null || !Guid.TryParse(value, out var professionalId))
            throw new ArgumentException("Token inválido ou expirado.");

        var professional = await _professionalRepo.GetByIdIgnoringQueryFilterAsync(professionalId, ct)
            ?? throw new ArgumentException("Profissional não encontrado.");

        professional.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _professionalRepo.UpdateAsync(professional, ct);

        // Invalida o token imediatamente após o uso
        await _cache.RemoveAsync(cacheKey, ct);

        _logger.LogInformation(
            "Senha redefinida com sucesso para profissional {ProfessionalId}.", professionalId);
    }

    private async Task SendResetEmailAsync(string toEmail, string toName, string link)
    {
        var host     = _config.GetValue<string>("Smtp:Host");
        var port     = _config.GetValue<int>("Smtp:Port", 587);
        var user     = _config.GetValue<string>("Smtp:User");
        var password = _config.GetValue<string>("Smtp:Password");
        var from     = _config.GetValue<string>("Smtp:FromAddress") ?? "noreply@agendainteligente.com.br";
        var fromName = _config.GetValue<string>("Smtp:FromName") ?? "Agenda Inteligente";

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("[EMAIL-STUB] SMTP não configurado. Link de reset para {Email}: {Link}", toEmail, link);
            return;
        }

        var body = new StringBuilder()
            .AppendLine($"Olá, {toName}!")
            .AppendLine()
            .AppendLine("Recebemos uma solicitação para redefinir sua senha.")
            .AppendLine()
            .AppendLine($"Clique no link abaixo para criar uma nova senha (válido por 1 hora):")
            .AppendLine()
            .AppendLine(link)
            .AppendLine()
            .AppendLine("Se você não solicitou isso, ignore este e-mail.")
            .ToString();

        using var client = new SmtpClient(host, port)
        {
            Credentials        = new NetworkCredential(user, password),
            EnableSsl          = port != 25,
            DeliveryMethod     = SmtpDeliveryMethod.Network,
        };

        using var message = new MailMessage(
            new MailAddress(from, fromName),
            new MailAddress(toEmail, toName))
        {
            Subject    = "Redefinição de senha — Agenda Inteligente",
            Body       = body,
            IsBodyHtml = false,
        };

        await client.SendMailAsync(message);
        _logger.LogInformation("E-mail de reset enviado para {Email}.", toEmail);
    }
}
