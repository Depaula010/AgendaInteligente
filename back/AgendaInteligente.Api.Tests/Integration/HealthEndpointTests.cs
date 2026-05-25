using System.Net;
using Xunit;

namespace AgendaInteligente.Api.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body);
        Assert.Contains("timestamp", body);
    }

    [Fact]
    public async Task GetHealthDetailed_ReturnsJsonWithChecksField()
    {
        var response = await _client.GetAsync("/health/detailed");

        // Pode ser 200 (healthy) ou 503 (degraded) dependendo da infra — só verificamos o contrato da resposta
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Esperado 200 ou 503, obtido {(int)response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\"", body);
        Assert.Contains("\"checks\"", body);
        Assert.Contains("\"timestamp\"", body);
    }
}
