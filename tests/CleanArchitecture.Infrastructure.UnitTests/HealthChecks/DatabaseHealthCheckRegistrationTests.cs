using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.HealthChecks;

public sealed class DatabaseHealthCheckRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersDatabaseCheckForReadinessOnly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.DatabaseConnectionStringName}"] = "Host=localhost;Database=test",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var databaseCheck = registrations.ShouldHaveSingleItem();
        databaseCheck.Name.ShouldBe(DependencyInjection.DatabaseHealthCheckName);
        databaseCheck.Tags.ShouldContain(DependencyInjection.ReadinessTag);
        databaseCheck.Tags.ShouldNotContain("live");
    }
}
