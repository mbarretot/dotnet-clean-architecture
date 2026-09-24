using System.Net;
using System.Text.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.OpenApi;

[Collection(IntegrationTestCollection.Name)]
public sealed class OpenApiDocumentTests(ApiFactory factory) : IntegrationTest(factory)
{
    public static TheoryData<string, string> ReadOperations => new()
    {
        { "/api/products", "get" },
        { "/api/products/{id}", "get" },
    };

    public static TheoryData<string, string> WriteOperations => new()
    {
        { "/api/products", "post" },
        { "/api/products/{id}", "put" },
        { "/api/products/{id}", "delete" },
    };

    [Fact]
    public async Task Document_is_reachable_anonymously()
    {
        using var client = CreateAnonymousClient();

        var response = await client.GetAsync(OpenApiDocument.Path, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(ReadOperations))]
    public async Task Read_operation_declares_bearer_security_and_401_but_not_403(string path, string method)
    {
        using var document = await OpenApiDocument.FetchAsync(CreateAnonymousClient(), CancellationToken);

        var operation = document.Operation(path, method);

        operation.RequiresBearer().ShouldBeTrue();
        operation.DeclaresResponse("401").ShouldBeTrue();
        operation.DeclaresResponse("403").ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(WriteOperations))]
    public async Task Write_operation_declares_bearer_security_401_and_403(string path, string method)
    {
        using var document = await OpenApiDocument.FetchAsync(CreateAnonymousClient(), CancellationToken);

        var operation = document.Operation(path, method);

        operation.RequiresBearer().ShouldBeTrue();
        operation.DeclaresResponse("401").ShouldBeTrue();
        operation.DeclaresResponse("403").ShouldBeTrue();
    }
}

/// <summary>Minimal reader over the served OpenAPI JSON; enough to assert per-operation security and responses.</summary>
internal sealed class OpenApiDocument : IDisposable
{
    public const string Path = "/openapi/v1.json";

    private readonly JsonDocument _json;

    private OpenApiDocument(JsonDocument json) => _json = json;

    public static async Task<OpenApiDocument> FetchAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var response = await client.GetAsync(Path, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return new OpenApiDocument(await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken));
        }
    }

    public OpenApiOperation Operation(string path, string method) =>
        new(_json.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method));

    public void Dispose() => _json.Dispose();
}

internal readonly record struct OpenApiOperation(JsonElement Element)
{
    public bool RequiresBearer() =>
        Element.TryGetProperty("security", out var security)
        && security.EnumerateArray().Any(requirement => requirement.TryGetProperty("Bearer", out _));

    public bool DeclaresResponse(string statusCode) =>
        Element.TryGetProperty("responses", out var responses) && responses.TryGetProperty(statusCode, out _);
}
