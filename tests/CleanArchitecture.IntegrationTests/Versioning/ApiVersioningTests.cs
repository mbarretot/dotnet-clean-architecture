using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Versioning;

[Collection(IntegrationTestCollection.Name)]
public sealed class ApiVersioningTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/products?api-version=1.0")]
    [InlineData("/api/products?api-version=1")]
    public async Task Version_1_is_served_with_or_without_an_explicit_version(string uri)
    {
        using var client = CreateReaderClient();

        var response = await client.GetAsync(uri, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("api-supported-versions").ShouldBe(["1.0"]);
    }

    [Fact]
    public async Task Version_can_be_selected_with_a_header()
    {
        using var client = CreateReaderClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add("X-Api-Version", "1.0");

        var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unsupported_version_returns_bad_request_problem()
    {
        using var client = CreateReaderClient();

        var response = await client.GetAsync("/api/products?api-version=2.0", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Type.ShouldBe("https://docs.api-versioning.org/problems#unsupported");
    }
}
