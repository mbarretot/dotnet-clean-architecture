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

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string BearerSection = "Authentication:Schemes:Bearer";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private Respawner _respawner = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

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

    public async Task ResetDatabaseAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

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
        builder.UseEnvironment("IntegrationTests");

        // UseSetting: AddInfrastructure reads the connection string before configuration callbacks run.
        builder.UseSetting(
            $"ConnectionStrings:{DependencyInjection.DatabaseConnectionStringName}", _database.GetConnectionString());

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
