using System.Net;
using System.Net.Http.Json;
using System.Text;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Authorization;

/// <summary>
/// 401 and 403 produced by the real JwtBearer handler and the real policies are RFC 9457 problem responses, shaped
/// like every other error the API returns, without dropping the <c>WWW-Authenticate</c> challenge.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthorizationProblemDetailsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string ProblemJson = "application/problem+json";

    public static TheoryData<string> ReadAndWriteEndpoints => ["GET /api/products", "POST /api/products"];

    public static TheoryData<string> WriteEndpoints => ["POST /api/products", "DELETE /api/products/{id}"];

    [Theory]
    [MemberData(nameof(ReadAndWriteEndpoints))]
    public async Task Request_without_token_returns_unauthorized_problem_with_bearer_challenge(string endpoint)
    {
        using var client = CreateAnonymousClient();

        var response = await SendAsync(client, endpoint, Guid.NewGuid());

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
        response.Headers.WwwAuthenticate.ShouldContain(header => header.Scheme == "Bearer");
    }

    [Theory]
    [MemberData(nameof(ReadAndWriteEndpoints))]
    public async Task Request_with_invalid_token_returns_unauthorized_problem_without_leaking_validation_details(
        string endpoint)
    {
        var untrustedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("an-attacker-controlled-key-0123456789abcdef"));
        using var client = Factory.CreateClient(TestJwtTokens.CreateSignedWith(untrustedKey, "attacker", Scopes.ProductsWrite));

        var response = await SendAsync(client, endpoint, Guid.NewGuid());

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
        var challenge = response.Headers.WwwAuthenticate.ShouldHaveSingleItem();
        challenge.Scheme.ShouldBe("Bearer");
        challenge.Parameter.ShouldNotBeNull().ShouldContain("error=\"invalid_token\"");

        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        body.ShouldNotContain("IDX", Case.Sensitive);
        body.ShouldNotContain("signature", Case.Insensitive);
    }

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task Write_without_required_scope_returns_forbidden_problem(string endpoint)
    {
        using var writer = CreateWriterClient();
        var id = await CreateProductAsync(writer, NewProduct());
        using var reader = CreateReaderClient();

        var response = await SendAsync(reader, endpoint, id);

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    [Fact]
    public async Task Authorization_problems_share_the_shape_of_other_problem_responses()
    {
        using var reader = CreateReaderClient();
        using var anonymous = CreateAnonymousClient();

        using var notFoundResponse = await reader.GetAsync($"/api/products/{Guid.NewGuid()}", CancellationToken);
        using var unauthorizedResponse = await anonymous.GetAsync("/api/products", CancellationToken);
        var notFound = await notFoundResponse.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        var unauthorized = await unauthorizedResponse.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        notFound.ShouldNotBeNull();
        unauthorized.ShouldNotBeNull();
        unauthorized.Type.ShouldNotBeNullOrWhiteSpace();
        (unauthorized.Instance is null).ShouldBe(notFound.Instance is null);
        unauthorized.Extensions.Keys.ShouldBe(notFound.Extensions.Keys, ignoreOrder: true);
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string title)
    {
        response.StatusCode.ShouldBe(statusCode);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe((int)statusCode);
        problem.Title.ShouldBe(title);
        problem.Type.ShouldNotBeNullOrWhiteSpace();
        problem.Extensions.ShouldContainKey("traceId");
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid id)
    {
        var (method, template) = (endpoint.Split(' ')[0], endpoint.Split(' ')[1]);
        var uri = template.Replace("{id}", id.ToString(), StringComparison.Ordinal);

        return method switch
        {
            "GET" => client.GetAsync(uri, CancellationToken),
            "POST" => client.PostAsJsonAsync(uri, NewProduct("Other", "SKU-OTHER"), CancellationToken),
            "DELETE" => client.DeleteAsync(uri, CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unsupported HTTP method."),
        };
    }
}
