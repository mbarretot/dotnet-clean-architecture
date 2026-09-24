using System.Net;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Health;

[Collection(IntegrationTestCollection.Name)]
public sealed class HealthEndpointTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Theory]
    [InlineData("/alive")]
    [InlineData("/health")]
    public async Task Probe_is_anonymous_and_healthy_while_the_database_is_up(string path)
    {
        using var client = CreateAnonymousClient();

        var response = await client.GetAsync(path, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldBe("Healthy");
    }
}
