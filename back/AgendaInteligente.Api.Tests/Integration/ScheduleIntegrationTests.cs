using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AgendaInteligente.Api.Tests.Integration;

public sealed class ScheduleIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ScheduleIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSchedules_WithoutJwt_Returns401()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/schedules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_WithValidJwt_Returns200()
    {
        var token  = GenerateJwtToken(Guid.NewGuid());
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/schedules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_WithExpiredJwt_Returns401()
    {
        var token  = GenerateJwtToken(Guid.NewGuid(), expiresMinutes: -1);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/schedules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_WithWrongIssuer_Returns401()
    {
        var token  = GenerateJwtToken(Guid.NewGuid(), issuer: "wrong-issuer");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/schedules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Helper ───────────────────────────────────────────────────────────────────

    private static string GenerateJwtToken(
        Guid    tenantId,
        string  role          = "Owner",
        int     expiresMinutes = 60,
        string? issuer        = null,
        string? audience      = null)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestWebApplicationFactory.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("role",      role),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer   ?? TestWebApplicationFactory.JwtIssuer,
            audience:           audience ?? TestWebApplicationFactory.JwtAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
