namespace AgendaInteligente.Api.MultiTenancy;

/// <summary>
/// Abstração para resolução do TenantId no contexto da requisição atual.
/// Desacoplada do HttpContext para facilitar testes unitários.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Retorna o TenantId do contexto atual ou <c>null</c> quando não há
    /// contexto (ex: execução de migrations, background services).
    /// O Global Query Filter do AppDbContext usa esse valor para isolar os dados.
    /// </summary>
    Guid? CurrentTenantId { get; }
}
