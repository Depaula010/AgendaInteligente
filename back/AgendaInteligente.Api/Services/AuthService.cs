using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Auth;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AgendaInteligente.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly IProfessionalRepository _professionalRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IProfessionalRepository professionalRepository,
        IOptions<JwtSettings> jwtOptions)
    {
        _professionalRepository = professionalRepository;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // 1. Buscar usuário ignorando o filtro de TenantId (pois ainda não sabemos o tenant dele)
        var professional = await _professionalRepository.GetByEmailIgnoringQueryFilterAsync(request.Email, ct);

        if (professional is null || !professional.IsActive)
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        // 2. Verificar a senha (BCrypt)
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, professional.PasswordHash);
        
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        // 3. Gerar Token JWT
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, professional.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, professional.Email),
            new(JwtRegisteredClaimNames.Name, professional.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", professional.TenantId.ToString()),
            new("role", professional.Role.ToString()),
            new("can_manage_services", professional.CanManageServices ? "true" : "false")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new AuthResponse(
            Token: tokenString,
            Id: professional.Id,
            Name: professional.Name,
            Email: professional.Email,
            Role: professional.Role,
            TenantId: professional.TenantId
        );
    }
}
