using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.CalendarSync;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class GoogleCalendarSyncBackgroundServiceTests
{
    private readonly Mock<ICalendarSyncQueue> _queueMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IScheduleRepository> _scheduleRepoMock = new();
    private readonly Mock<IGoogleCalendarApiService> _googleApiMock = new();

    public GoogleCalendarSyncBackgroundServiceTests()
    {
        _scopeFactoryMock.Setup(s => s.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IScheduleRepository)))
            .Returns(_scheduleRepoMock.Object);
            
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IGoogleCalendarApiService)))
            .Returns(_googleApiMock.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_Upsert_CallsGoogleApiAndUpdatesEventId()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var message = new CalendarSyncMessage(scheduleId, tenantId, CalendarSyncOperation.Upsert);

        // Configurar a fila para retornar esta mensagem e depois encerrar
        _queueMock.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => GetSingleMessageAsync(message, ct));

        var schedule = new Schedule 
        { 
            Id = scheduleId,
            TenantId = tenantId,
            Professional = new Professional 
            { 
                Name = "John", 
                Email = "john@example.com", 
                PasswordHash = "hash",
                GoogleCalendarRefreshToken = "refresh-token" 
            }
        };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        _googleApiMock.Setup(api => api.UpsertEventAsync(schedule, "refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-google-event-id");

        var sut = new GoogleCalendarSyncBackgroundService(
            _queueMock.Object, 
            _scopeFactoryMock.Object, 
            NullLogger<GoogleCalendarSyncBackgroundService>.Instance);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.StartAsync(cts.Token);
        
        // Dá um pequeno tempo para o processamento assíncrono antes de cancelar
        await Task.Delay(100, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        _googleApiMock.Verify(api => api.UpsertEventAsync(schedule, "refresh-token", It.IsAny<CancellationToken>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.UpdateGoogleEventIdAsync(scheduleId, "new-google-event-id", It.IsAny<CancellationToken>()), Times.Once);
    }

    private async IAsyncEnumerable<CalendarSyncMessage> GetSingleMessageAsync(
        CalendarSyncMessage message, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return message;
        try 
        {
            await Task.Delay(Timeout.Infinite, ct); // Keep open until cancellation
        }
        catch (TaskCanceledException) 
        {
            // Expected
        }
    }
}
