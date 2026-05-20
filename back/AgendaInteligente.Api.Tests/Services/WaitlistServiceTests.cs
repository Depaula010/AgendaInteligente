using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class WaitlistServiceTests
{
    // ── Fixtures ───────────────────────────────────────────────────────────────
    private readonly Mock<IWaitlistRepository>          _waitlistRepoMock  = new();
    private readonly Mock<IWhatsAppNotificationService> _whatsAppMock      = new();
    private readonly WaitlistService                    _sut;

    private static readonly Guid TenantId      = Guid.NewGuid();
    private static readonly Guid ProfessionalId = Guid.NewGuid();
    private static readonly Guid CustomerId     = Guid.NewGuid();

    public WaitlistServiceTests()
    {
        _sut = new WaitlistService(
            _waitlistRepoMock.Object,
            _whatsAppMock.Object,
            NullLogger<WaitlistService>.Instance);
    }

    // ── Helper para montar entradas da Waitlist ─────────────────────────────

    private static Waitlist BuildWaitlistEntry(
        Guid? professionalId = null,
        string phone = "+5511999999999",
        string name  = "João")
        => new()
        {
            Id             = Guid.NewGuid(),
            TenantId       = TenantId,
            CustomerId     = CustomerId,
            ProfessionalId = professionalId ?? ProfessionalId,
            ServiceId      = Guid.NewGuid(),
            DesiredDate    = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Status         = WaitlistStatus.Waiting,
            CreatedAt      = DateTime.UtcNow,
            Customer       = new Customer { Id = CustomerId, Name = name, PhoneNumber = phone, TenantId = TenantId }
        };

    // ── Caminho Feliz: há clientes na fila, envia notificação ─────────────────

    [Fact]
    public async Task ProcessCancellationAsync_WithPendingEntries_NotifiesAllClients()
    {
        // Arrange
        var freedStart = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freedEnd   = freedStart.AddHours(1);

        var entries = new List<Waitlist>
        {
            BuildWaitlistEntry(),
            BuildWaitlistEntry(phone: "+5511888888888", name: "Maria")
        };

        _waitlistRepoMock
            .Setup(r => r.GetPendingByDateAsync(
                DateOnly.FromDateTime(freedStart.Date),
                ProfessionalId,
                default))
            .ReturnsAsync(entries);

        _waitlistRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Waitlist>(), default))
            .Returns(Task.CompletedTask);

        _whatsAppMock
            .Setup(w => w.SendWaitlistNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act — não deve lançar exceção
        await _sut.ProcessCancellationAsync(ProfessionalId, freedStart, freedEnd);

        // Assert — notificação enviada para os 2 clientes
        _whatsAppMock.Verify(w => w.SendWaitlistNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            freedStart, It.IsAny<string>(), default),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessCancellationAsync_WithPendingEntries_UpdatesStatusToNotified()
    {
        // Arrange
        var freedStart = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freedEnd   = freedStart.AddHours(1);
        var entry      = BuildWaitlistEntry();

        _waitlistRepoMock
            .Setup(r => r.GetPendingByDateAsync(
                DateOnly.FromDateTime(freedStart.Date), ProfessionalId, default))
            .ReturnsAsync(new List<Waitlist> { entry });

        _waitlistRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Waitlist>(), default))
            .Returns(Task.CompletedTask);

        _whatsAppMock
            .Setup(w => w.SendWaitlistNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessCancellationAsync(ProfessionalId, freedStart, freedEnd);

        // Assert — status deve ter sido alterado para Notified antes do envio
        _waitlistRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Waitlist>(w => w.Status == WaitlistStatus.Notified && w.NotifiedAt.HasValue),
            default), Times.Once);
    }

    // ── Caminho Feliz: fila vazia, nada acontece ──────────────────────────────

    [Fact]
    public async Task ProcessCancellationAsync_WithEmptyWaitlist_DoesNotSendNotifications()
    {
        // Arrange
        var freedStart = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freedEnd   = freedStart.AddHours(1);

        _waitlistRepoMock
            .Setup(r => r.GetPendingByDateAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), default))
            .ReturnsAsync(new List<Waitlist>());

        // Act
        await _sut.ProcessCancellationAsync(ProfessionalId, freedStart, freedEnd);

        // Assert — nenhuma notificação enviada e nenhum update feito
        _whatsAppMock.Verify(w => w.SendWaitlistNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), default), Times.Never);

        _waitlistRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Waitlist>(), default), Times.Never);
    }

    // ── Caminho Triste: repositório lança exceção ─────────────────────────────

    [Fact]
    public async Task ProcessCancellationAsync_WhenRepositoryThrows_DoesNotPropagateException()
    {
        // Arrange — repositório lança exceção (ex: banco indisponível)
        var freedStart = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freedEnd   = freedStart.AddHours(1);

        _waitlistRepoMock
            .Setup(r => r.GetPendingByDateAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), default))
            .ThrowsAsync(new Exception("Banco indisponível"));

        // Act — deve engolir a exceção sem propagá-la (cancelamento não deve ser revertido)
        var exception = await Record.ExceptionAsync(
            () => _sut.ProcessCancellationAsync(ProfessionalId, freedStart, freedEnd));

        // Assert — nenhuma exceção propagada
        Assert.Null(exception);
    }

    // ── Caminho Triste: cliente sem telefone ──────────────────────────────────

    [Fact]
    public async Task ProcessCancellationAsync_WhenClientHasNoPhone_SkipsNotificationForThatClient()
    {
        // Arrange — um cliente sem telefone, outro com telefone
        var freedStart   = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freedEnd     = freedStart.AddHours(1);
        var entryNoPhone = BuildWaitlistEntry(phone: "");          // sem telefone — deve ser ignorado
        var entryOk      = BuildWaitlistEntry(phone: "+5511777777777", name: "Ana");

        _waitlistRepoMock
            .Setup(r => r.GetPendingByDateAsync(
                DateOnly.FromDateTime(freedStart.Date), ProfessionalId, default))
            .ReturnsAsync(new List<Waitlist> { entryNoPhone, entryOk });

        _waitlistRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Waitlist>(), default))
            .Returns(Task.CompletedTask);

        _whatsAppMock
            .Setup(w => w.SendWaitlistNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessCancellationAsync(ProfessionalId, freedStart, freedEnd);

        // Assert — notificação enviada apenas para o cliente com telefone
        _whatsAppMock.Verify(w => w.SendWaitlistNotificationAsync(
            "+5511777777777", It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), default), Times.Once);

        _whatsAppMock.Verify(w => w.SendWaitlistNotificationAsync(
            "", It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), default), Times.Never);
    }

    // ── Caminho Triste: falha no WhatsApp não impede os demais ───────────────

    [Fact]
    public async Task ProcessCancellationAsync_WhenWhatsAppThrowsForFirstClient_ContinuesToNextClient()
    {
        // Arrange — WhatsApp falha no primeiro cliente, deve continuar e notificar o segundo
        var freedStart = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freedEnd   = freedStart.AddHours(1);

        var entry1 = BuildWaitlistEntry(phone: "+5511111111111", name: "Pedro");
        var entry2 = BuildWaitlistEntry(phone: "+5511222222222", name: "Luísa");

        _waitlistRepoMock
            .Setup(r => r.GetPendingByDateAsync(
                DateOnly.FromDateTime(freedStart.Date), ProfessionalId, default))
            .ReturnsAsync(new List<Waitlist> { entry1, entry2 });

        _waitlistRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Waitlist>(), default))
            .Returns(Task.CompletedTask);

        // Primeiro cliente: falha no envio; segundo: sucesso
        _whatsAppMock
            .SetupSequence(w => w.SendWaitlistNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("Timeout no WhatsApp"))
            .Returns(Task.CompletedTask);

        // Act — não deve propagar a exceção
        var exception = await Record.ExceptionAsync(
            () => _sut.ProcessCancellationAsync(ProfessionalId, freedStart, freedEnd));

        // Assert
        Assert.Null(exception);

        // Ambos os updates foram feitos (status mudou antes do envio)
        _waitlistRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Waitlist>(), default), Times.Exactly(2));

        // Tentativa de envio foi feita para os dois
        _whatsAppMock.Verify(w => w.SendWaitlistNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), default), Times.Exactly(2));
    }
}
