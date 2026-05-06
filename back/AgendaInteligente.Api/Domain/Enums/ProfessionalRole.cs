namespace AgendaInteligente.Api.Domain.Enums;

/// <summary>
/// Define o nível de acesso e o papel de um profissional no estabelecimento (Tenant).
/// </summary>
public enum ProfessionalRole
{
    /// <summary>
    /// Profissional comum (Staff). Pode ver sua própria agenda e, 
    /// dependendo das permissões definidas pelo Owner, a agenda geral.
    /// </summary>
    Staff = 0,

    /// <summary>
    /// Dono do estabelecimento. Possui acesso total ao painel:
    /// visualização de todas as agendas, gestão de equipe, serviços e configurações.
    /// </summary>
    Owner = 1
}
