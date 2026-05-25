using System.Net;
using System.Net.Http.Json;
using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using Xunit;

namespace AgendaInteligente.Api.Tests.Integration;

public sealed class OnboardingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OnboardingIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostOnboarding_WithValidPayload_Returns201AndResponse()
    {
        var suffix  = Guid.NewGuid().ToString("N")[..8];
        var request = new OnboardTenantRequest(
            TenantName: "Barbearia Teste",
            Slug: $"barbearia-{suffix}",
            OwnerName: "Dono Teste",
            OwnerEmail: $"dono_{suffix}@teste.com",
            OwnerPassword: "Senha@123");

        var response = await _client.PostAsJsonAsync("/api/v1/onboarding", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OnboardTenantResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.TenantId);
        Assert.NotEqual(Guid.Empty, body.ProfessionalId);
        Assert.Contains(suffix, body.Slug);
    }

    [Fact]
    public async Task PostOnboarding_WithDuplicateSlug_Returns409()
    {
        var slug = $"slug-dup-{Guid.NewGuid():N}"[..30];

        await _client.PostAsJsonAsync("/api/v1/onboarding",
            new OnboardTenantRequest("A", slug, "A", $"a_{Guid.NewGuid():N}@t.com", "Senha@123"));

        var response = await _client.PostAsJsonAsync("/api/v1/onboarding",
            new OnboardTenantRequest("B", slug, "B", $"b_{Guid.NewGuid():N}@t.com", "Senha@123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostOnboarding_WithEmptyBody_Returns400()
    {
        var response = await _client.PostAsync(
            "/api/v1/onboarding",
            new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOnboarding_WithMissingRequiredFields_Returns400()
    {
        // Slug vazio — OnboardingService retorna erro de validação
        var request = new OnboardTenantRequest(
            TenantName: "Tenant OK",
            Slug: "",
            OwnerName: "Dono",
            OwnerEmail: "dono@test.com",
            OwnerPassword: "Senha@123");

        var response = await _client.PostAsJsonAsync("/api/v1/onboarding", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
