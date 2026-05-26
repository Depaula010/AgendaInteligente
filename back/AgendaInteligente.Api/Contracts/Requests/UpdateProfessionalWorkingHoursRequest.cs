namespace AgendaInteligente.Api.Contracts.Requests;

/// <summary>
/// Payload para atualizar os horários individuais de um profissional.
/// Enviar WorkingHoursJson = null remove o override e volta a usar o padrão do tenant.
/// </summary>
public record UpdateProfessionalWorkingHoursRequest(string? WorkingHoursJson);
