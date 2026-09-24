using System.Net.Http.Headers;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace CleanArchitecture.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory against a throwaway PostgreSQL container. Shared by every test through
/// <see cref="IntegrationTestCollection"/>, so the container starts and migrations run once per test run.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string BearerSection = "Authentication:Schemes:Bearer";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private Respawner _respawner = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        // Program only migrates when it is the entry assembly, so the test host must do it explicitly.
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        }

        await using var connection = await OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
    }

    /// <summary>Deletes every row written by earlier tests while keeping the migrated schema.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>A client that sends <paramref name="accessToken"/> as a bearer token, or no credentials when null.</summary>
    public HttpClient CreateClient(string? accessToken)
    {
        var client = CreateClient();

        if (accessToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": keeps developer user-secrets (e.g. dotnet user-jwts signing keys) out of the test host.
        builder.UseEnvironment("IntegrationTests");

        // UseSetting, not ConfigureAppConfiguration: AddInfrastructure reads the connection string while Program
        // registers services, before configuration callbacks added by the factory would be applied.
        builder.UseSetting(
            $"ConnectionStrings:{DependencyInjection.DatabaseConnectionStringName}", _database.GetConnectionString());

        // The same shape `dotnet user-jwts` writes: the real JwtBearer handler validates issuer, audience and signature.
        builder.UseSetting($"{BearerSection}:ValidIssuer", TestJwtTokens.Issuer);
        builder.UseSetting($"{BearerSection}:ValidAudiences:0", TestJwtTokens.Audience);
        builder.UseSetting($"{BearerSection}:SigningKeys:0:Issuer", TestJwtTokens.Issuer);
        builder.UseSetting($"{BearerSection}:SigningKeys:0:Value", TestJwtTokens.SigningKeyBase64);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();

        return connection;
    }
}
