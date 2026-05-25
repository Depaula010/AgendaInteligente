using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AgendaInteligente.Api.Tests.Integration;

public sealed class WebhookIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid TenantId = Guid.NewGuid();

    public WebhookIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostWebhook_WithoutApiKey_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/webhooks/whatsapp/{TenantId}",
            new { NumeroRemetente = "5511999999999", Texto = "Oi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithWrongApiKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/webhooks/whatsapp/{TenantId}");
        request.Headers.Add("X-Api-Key", "wrong-key-xyz");
        request.Content = JsonContent.Create(new { NumeroRemetente = "5511999999999", Texto = "Oi" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithValidApiKey_ReturnsAcceptedOrOk()
    {
        // Redis não disponível no test → fallback síncrono (200) ou assíncrono (202)
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/webhooks/whatsapp/{TenantId}");
        request.Headers.Add("X-Api-Key", TestWebApplicationFactory.TestApiKey);
        request.Content = JsonContent.Create(new { NumeroRemetente = "5511999999999", Texto = "Oi" });

        var response = await _client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Accepted,
            $"Esperado 200 ou 202, obtido {(int)response.StatusCode}");
    }

    [Fact]
    public async Task PostWebhook_WithValidApiKey_AndEmptyBody_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/webhooks/whatsapp/{TenantId}");
        request.Headers.Add("X-Api-Key", TestWebApplicationFactory.TestApiKey);
        request.Content = new StringContent(
            "{\"NumeroRemetente\":\"\",\"Texto\":\"\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);

        // ArgumentException no service → 400
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
