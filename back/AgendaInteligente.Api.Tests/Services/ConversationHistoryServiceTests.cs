using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class ConversationHistoryServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly ConversationHistoryService _service;

    public ConversationHistoryServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _service = new ConversationHistoryService(_cacheMock.Object, new NullLogger<ConversationHistoryService>());
    }

    // ── GetHistoryAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_WhenCacheEmpty_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetHistoryAsync(Guid.NewGuid(), "5511999999999");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenCacheHasHistory_ReturnsDeserializedList()
    {
        // Arrange
        var history = new List<MessageHistory>
        {
            new() { Role = "user",  Content = "Olá, quero agendar" },
            new() { Role = "model", Content = "Claro! Para qual dia?" }
        };
        var json = JsonSerializer.Serialize(history);
        var bytes = Encoding.UTF8.GetBytes(json);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        // Act
        var result = await _service.GetHistoryAsync(Guid.NewGuid(), "5511999999999");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("user",  result[0].Role);
        Assert.Equal("model", result[1].Role);
        Assert.Equal("Olá, quero agendar", result[0].Content);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenCacheThrows_ReturnsEmptyList()
    {
        // Arrange — Redis indisponível não deve quebrar o sistema
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis connection refused"));

        // Act
        var result = await _service.GetHistoryAsync(Guid.NewGuid(), "5511999999999");

        // Assert — degradação graciosa
        Assert.Empty(result);
    }

    // ── SaveHistoryAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveHistoryAsync_SerializesAndCallsCacheSet()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var phone = "5511999999999";
        var history = new List<MessageHistory>
        {
            new() { Role = "user", Content = "oi" }
        };
        // Chave calculada conforme a implementação: chat:{tenantId}:{phone}
        var expectedKey = $"chat:{tenantId}:{phone}";
        byte[]? capturedBytes = null;

        _cacheMock.Setup(c => c.SetAsync(
                expectedKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, b, _, _) => capturedBytes = b)
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveHistoryAsync(tenantId, phone, history);

        // Assert
        Assert.NotNull(capturedBytes);
        var json = Encoding.UTF8.GetString(capturedBytes!);
        var deserialized = JsonSerializer.Deserialize<List<MessageHistory>>(json);
        Assert.Single(deserialized!);
        Assert.Equal("oi", deserialized![0].Content);
    }

    // ── IsMessageDuplicateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task IsMessageDuplicateAsync_WhenFirstSeen_ReturnsFalseAndSetsKey()
    {
        // Arrange
        var messageId = "msg-abc-123";
        // Chave calculada conforme a implementação: debounce:{messageId}
        var expectedKey = $"debounce:{messageId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _cacheMock.Setup(c => c.SetAsync(expectedKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var isDuplicate = await _service.IsMessageDuplicateAsync(messageId);

        // Assert
        Assert.False(isDuplicate);
        _cacheMock.Verify(c => c.SetAsync(expectedKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsMessageDuplicateAsync_WhenSeenBefore_ReturnsTrue()
    {
        // Arrange — chave já existe no cache (mensagem duplicada)
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("1"));

        // Act
        var isDuplicate = await _service.IsMessageDuplicateAsync("msg-already-seen");

        // Assert
        Assert.True(isDuplicate);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsMessageDuplicateAsync_WhenCacheThrows_ReturnsFalse()
    {
        // Arrange — Redis indisponível: a mensagem deve ser processada normalmente
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis connection refused"));

        // Act
        var isDuplicate = await _service.IsMessageDuplicateAsync("msg-redis-down");

        // Assert
        Assert.False(isDuplicate);
    }
}
