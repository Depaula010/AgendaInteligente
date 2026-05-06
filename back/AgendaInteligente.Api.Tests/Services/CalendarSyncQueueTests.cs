using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.CalendarSync;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class CalendarSyncQueueTests
{
    [Fact]
    public async Task EnqueueAsync_ShouldAddMessageToQueue_And_DequeueShouldRetrieveIt()
    {
        // Arrange
        var queue = new CalendarSyncQueue();
        var scheduleId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var message = new CalendarSyncMessage(scheduleId, tenantId, CalendarSyncOperation.Upsert);

        // Act
        await queue.EnqueueAsync(message);

        // Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        
        await foreach (var retrieved in queue.DequeueAsync(cts.Token))
        {
            Assert.Equal(scheduleId, retrieved.ScheduleId);
            Assert.Equal(tenantId, retrieved.TenantId);
            Assert.Equal(CalendarSyncOperation.Upsert, retrieved.Operation);
            break; // Stop after first message
        }
    }
}
