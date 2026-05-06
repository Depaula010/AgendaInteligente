using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class AiOrchestratorServiceTests
{
    [Fact]
    public async Task ProcessUserMessageAsync_ShouldReturnIntent_WhenSettingsExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new TenantSettings
        {
            TenantId = tenantId,
            GeminiApiKey = "tenant-api-key",
            GeminiModel = "gemini-2.5-flash-lite",
            BotDisplayName = "Barbearia Teste"
        };

        var tenantSettingsRepoMock = new Mock<ITenantSettingsRepository>();
        tenantSettingsRepoMock.Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var scheduleRepoMock = new Mock<IScheduleRepository>();

        var geminiServiceMock = new Mock<IGeminiService>();
        var expectedResponse = new GeminiIntentResponse { Intent = "schedule" };
        
        geminiServiceMock.Setup(s => s.ExtractIntentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>(), "tenant-api-key", "gemini-2.5-flash-lite"))
            .ReturnsAsync(expectedResponse);

        var options = Options.Create(new GeminiOptions());
        var logger = new NullLogger<AiOrchestratorService>();

        var service = new AiOrchestratorService(
            geminiServiceMock.Object, 
            tenantSettingsRepoMock.Object, 
            scheduleRepoMock.Object, 
            options, 
            logger);

        // Act
        var result = await service.ProcessUserMessageAsync(tenantId, "ola", new List<MessageHistory>());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("schedule", result.Intent);
        geminiServiceMock.Verify(s => s.ExtractIntentAsync(It.Is<string>(p => p.Contains("Barbearia Teste")), "ola", It.IsAny<List<MessageHistory>>(), "tenant-api-key", "gemini-2.5-flash-lite"), Times.Once);
    }

    [Fact]
    public async Task ProcessUserMessageAsync_ShouldThrowException_WhenSettingsNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        var tenantSettingsRepoMock = new Mock<ITenantSettingsRepository>();
        tenantSettingsRepoMock.Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettings?)null);

        var service = new AiOrchestratorService(
            new Mock<IGeminiService>().Object, 
            tenantSettingsRepoMock.Object, 
            new Mock<IScheduleRepository>().Object, 
            Options.Create(new GeminiOptions()), 
            new NullLogger<AiOrchestratorService>());

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => 
            service.ProcessUserMessageAsync(tenantId, "ola", new List<MessageHistory>()));
    }
}
