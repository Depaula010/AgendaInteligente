using AgendaInteligente.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AgendaInteligente.Api.Tests.Filters;

public class ApiKeyAuthFilterTests
{
    private readonly Mock<ILogger<ApiKeyAuthFilter>> _loggerMock;
    private readonly DefaultHttpContext _httpContext;
    private readonly EndpointFilterInvocationContext _context;

    public ApiKeyAuthFilterTests()
    {
        _loggerMock = new Mock<ILogger<ApiKeyAuthFilter>>();
        _httpContext = new DefaultHttpContext();
        _context = new DefaultEndpointFilterInvocationContext(_httpContext);
    }

    private ApiKeyAuthFilter CreateFilter(string? configuredKey)
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"WebhookSettings:ApiKey", configuredKey}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return new ApiKeyAuthFilter(configuration, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithValidApiKey_ReturnsNext()
    {
        // Arrange
        var validKey = "secret123";
        var filter = CreateFilter(validKey);
        _httpContext.Request.Headers["X-Api-Key"] = validKey;

        var nextCalled = false;
        EndpointFilterDelegate next = (ctx) =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        // Act
        var result = await filter.InvokeAsync(_context, next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithoutApiKeyHeader_ReturnsUnauthorized()
    {
        // Arrange
        var filter = CreateFilter("secret123");
        // No header added

        EndpointFilterDelegate next = (ctx) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(_context, next);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var filter = CreateFilter("secret123");
        _httpContext.Request.Headers["X-Api-Key"] = "wrong-key";

        EndpointFilterDelegate next = (ctx) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(_context, next);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }
}
