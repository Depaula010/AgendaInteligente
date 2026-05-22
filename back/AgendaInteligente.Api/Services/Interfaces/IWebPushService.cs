namespace AgendaInteligente.Api.Services.Interfaces;

public interface IWebPushService
{
    /// <summary>
    /// Envia uma push notification para o profissional do agendamento e todos os owners do tenant.
    /// Falhas individuais são absorvidas — nunca propaga exceção para o chamador.
    /// </summary>
    Task NotifyAsync(Guid professionalId, string title, string body, CancellationToken ct = default);
}
