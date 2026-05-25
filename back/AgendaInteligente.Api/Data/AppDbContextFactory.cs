using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AgendaInteligente.Api.Data;

/// <summary>
/// Usado EXCLUSIVAMENTE pelo `dotnet ef` em tempo de design (add migration / update database).
/// Fornece um AppDbContext configurado sem depender do HttpContext.
///
/// A connection string é lida do appsettings.Development.json (mesma usada em runtime),
/// evitando hardcode de credenciais no código-fonte.
/// A variável de ambiente DBPASSWORD pode sobrescrever a senha para pipelines CI/CD.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Carrega configuração do appsettings + appsettings.Development.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não encontrada em appsettings.json.");

        // Permite sobrescrever apenas a senha via variável de ambiente (útil em CI/CD)
        var dbPassword = Environment.GetEnvironmentVariable("DBPASSWORD");
        if (!string.IsNullOrWhiteSpace(dbPassword))
        {
            connectionString = connectionString
                .Replace("Password=postgres", $"Password={dbPassword}", StringComparison.OrdinalIgnoreCase);
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        // NullTenantProvider → CurrentTenantId = null
        // O Global Query Filter interpreta null como "sem filtro", o que é
        // correto e seguro para execução de migrations fora do contexto HTTP.
        return new AppDbContext(options, new NullTenantProvider(), new NullEncryptionService());
    }
}

/// <summary>
/// Provider sem contexto — CurrentTenantId sempre null.
/// Usado apenas pela AppDbContextFactory (migrations).
/// </summary>
file sealed class NullTenantProvider : ITenantProvider
{
    public Guid? CurrentTenantId => null;
}
