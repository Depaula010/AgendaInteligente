using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Requests.WhatsApp;
using AgendaInteligente.Api.Endpoints;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Endpoints;

public sealed class WhatsAppEndpointsTests
{
    private readonly Mock<IWhatsAppSendService> _sendMock;
    private readonly Mock<ITenantProvider>      _tenantMock;
    private static readonly Guid TenantId = Guid.NewGuid();

    public WhatsAppEndpointsTests()
    {
        _sendMock   = new Mock<IWhatsAppSendService>();
        _tenantMock = new Mock<ITenantProvider>();

        _tenantMock.Setup(t => t.CurrentTenantId).Returns(TenantId);
        _sendMock.Setup(s => s.SendTextMessageAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    // ── validação de input ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessageAsync_WithEmptyPhone_ReturnsBadRequest(string phone)
    {
        var request = new SendWhatsAppRequest { Phone = phone, Message = "Olá!" };

        var result = await WhatsAppEndpoints.SendMessageAsync(
            request, _sendMock.Object, _tenantMock.Object);

        var bad = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal(400, bad.StatusCode);
        Assert.Equal("Validation", bad.Value!.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessageAsync_WithEmptyMessage_ReturnsBadRequest(string message)
    {
        var request = new SendWhatsAppRequest { Phone = "5511999999999", Message = message };

        var result = await WhatsAppEndpoints.SendMessageAsync(
            request, _sendMock.Object, _tenantMock.Object);

        var bad = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal(400, bad.StatusCode);
        Assert.Equal("Validation", bad.Value!.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNoTenant_ReturnsUnauthorized()
    {
        _tenantMock.Setup(t => t.CurrentTenantId).Returns((Guid?)null);

        var result = await WhatsAppEndpoints.SendMessageAsync(
            ValidRequest(), _sendMock.Object, _tenantMock.Object);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    // ── sucesso ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_WithValidRequest_CallsSendServiceAndReturnsOk()
    {
        var request = ValidRequest();

        var result = await WhatsAppEndpoints.SendMessageAsync(
            request, _sendMock.Object, _tenantMock.Object);

        var ok = Assert.IsType<Ok<SendWhatsAppResponse>>(result);
        Assert.True(ok.Value!.Sent);
        _sendMock.Verify(s => s.SendTextMessageAsync(
            TenantId,
            request.Phone,
            request.Message,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_PassesTenantIdFromProvider()
    {
        Guid? capturedTenantId = null;
        _sendMock.Setup(s => s.SendTextMessageAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, CancellationToken>((t, _, _, _) => capturedTenantId = t)
            .ReturnsAsync(true);

        await WhatsAppEndpoints.SendMessageAsync(ValidRequest(), _sendMock.Object, _tenantMock.Object);

        Assert.Equal(TenantId, capturedTenantId);
    }

    // ── bot indisponível ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_WhenBotUnavailable_Returns502()
    {
        _sendMock.Setup(s => s.SendTextMessageAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await WhatsAppEndpoints.SendMessageAsync(
            ValidRequest(), _sendMock.Object, _tenantMock.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static SendWhatsAppRequest ValidRequest() => new()
    {
        Phone   = "5511999999999",
        Message = "Olá! Seu agendamento está confirmado."
    };
}
