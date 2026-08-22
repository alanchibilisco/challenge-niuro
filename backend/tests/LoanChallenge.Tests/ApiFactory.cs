using LoanChallenge.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanChallenge.Tests;

/// <summary>
/// Factory de la API con una base SQLite real en un archivo temporal (transacciones
/// reales, no el provider en memoria de EF Core) y el procesador outbox desactivado
/// para que los tests no dependan del servicio externo.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"loan-challenge-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:Enabled"] = "false",
            }));

        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<LoanDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<LoanDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    public LoanDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<LoanDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options);

    /// <summary>
    /// El fixture es compartido por todos los tests de la clase, así que se limpian
    /// las tablas al inicio de cada test para aislar el estado.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateDbContext();
        db.OutboxMessages.RemoveRange(db.OutboxMessages);
        db.Applications.RemoveRange(db.Applications);
        db.Customers.RemoveRange(db.Customers);
        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            File.Delete(_dbPath);
        }
    }
}
