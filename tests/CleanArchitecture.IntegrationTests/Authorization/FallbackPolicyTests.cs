using System.Net;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.IntegrationTests.OpenApi;
using CleanArchitecture.Presentation.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Authorization;

/// <summary>
/// Proves the fallback policy protects an endpoint that declares no authorization metadata at all. The endpoint is
/// registered only in this test host, through the same <see cref="IEndpoint"/> discovery the production endpoints use.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class FallbackPolicyTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Endpoint_without_authorization_metadata_rejects_anonymous_callers()
    {
        await using var host = CreateHostWithUnannotatedEndpoint();
        using var client = host.CreateClient();

        var response = await client.GetAsync(UnannotatedEndpoint.Route, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ShouldContain(header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task Endpoint_without_authorization_metadata_accepts_an_authenticated_caller()
    {
        await using var host = CreateHostWithUnannotatedEndpoint();
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestJwtTokens.Create(ReaderSubject));

        var response = await client.GetAsync(UnannotatedEndpoint.Route, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Endpoint_without_authorization_metadata_is_documented_as_protected()
    {
        await using var host = CreateHostWithUnannotatedEndpoint();
        using var document = await OpenApiDocument.FetchAsync(host.CreateClient(), CancellationToken);

        var operation = document.Operation(UnannotatedEndpoint.Route, "get");

        operation.RequiresBearer().ShouldBeTrue();
        operation.DeclaresResponse("401").ShouldBeTrue();
        operation.DeclaresResponse("403").ShouldBeFalse();
        operation.ResponseSchemaReference("401", "application/problem+json")
            .ShouldBe("#/components/schemas/ProblemDetails");
        document.Schema("ProblemDetails").ShouldNotBeNull();
    }

    private WebApplicationFactory<Program> CreateHostWithUnannotatedEndpoint() => Factory.WithWebHostBuilder(builder =>
        builder.ConfigureTestServices(services => services.AddTransient<IEndpoint, UnannotatedEndpoint>()));

    private sealed class UnannotatedEndpoint : IEndpoint
    {
        public const string Route = "/test/unannotated";

        public void MapEndpoint(IEndpointRouteBuilder endpoints) =>
            endpoints.MapGet(Route, () => TypedResults.Ok("reached"));
    }
}
