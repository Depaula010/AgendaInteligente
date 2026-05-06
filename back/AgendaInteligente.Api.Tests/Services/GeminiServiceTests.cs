using System.Net;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class GeminiServiceTests
{
    [Fact]
    public async Task ExtractIntentAsync_ShouldReturnIntentResponse_WhenApiReturnsSuccess()
    {
        // Arrange
        var jsonResponse = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": "{\"intent\":\"schedule\",\"date\":\"2026-10-15\",\"time\":\"14:00\",\"service\":\"Corte\",\"professional\":null,\"reply_message\":\"Perfeito, para qual dia você deseja?\"}"
                            }
                        ]
                    }
                }
            ]
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var logger = new NullLogger<GeminiService>();
        var service = new GeminiService(httpClient, logger);

        var history = new List<MessageHistory>();

        // Act
        var result = await service.ExtractIntentAsync("system_prompt", "quero cortar o cabelo amanhã", history, "fake-api-key", "gemini-2.5-flash-lite");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("schedule", result.Intent);
        Assert.Equal("2026-10-15", result.Date);
        Assert.Equal("14:00", result.Time);
        Assert.Equal("Corte", result.Service);
        Assert.Null(result.Professional);
    }

    [Fact]
    public async Task ExtractIntentAsync_ShouldThrowException_WhenApiReturnsError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("Rate Limit Exceeded")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var logger = new NullLogger<GeminiService>();
        var service = new GeminiService(httpClient, logger);

        var history = new List<MessageHistory>();

        // Act & Assert
        await Assert.ThrowsAsync<GeminiIntegrationException>(() => 
            service.ExtractIntentAsync("system_prompt", "teste", history, "fake-api-key", "gemini-2.5-flash-lite"));
    }
}
