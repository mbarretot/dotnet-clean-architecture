using System.Net;
using System.Text.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

    [Fact]
    public async Task Document_omits_the_oauth2_scheme_when_it_is_not_configured()
    {
        using var document = await OpenApiDocument.FetchAsync(CreateAnonymousClient(), CancellationToken);

        document.SecurityScheme(OAuth2SchemeId).ShouldBeNull();
        document.Operation("/api/products", "get").RequiresScheme(OAuth2SchemeId).ShouldBeFalse();
    }

    [Fact]
    public async Task Configured_oauth2_declares_the_authorization_code_flow_with_the_write_scope()
    {
        await using var host = CreateHostWithOAuth2();
        using var document = await OpenApiDocument.FetchAsync(host.CreateClient(), CancellationToken);

        var scheme = document.SecurityScheme(OAuth2SchemeId).ShouldNotBeNull();
        scheme.GetProperty("type").GetString().ShouldBe("oauth2");

        var flow = scheme.GetProperty("flows").GetProperty("authorizationCode");
        flow.GetProperty("authorizationUrl").GetString().ShouldBe(AuthorizationUrl);
        flow.GetProperty("tokenUrl").GetString().ShouldBe(TokenUrl);
        flow.GetProperty("scopes").TryGetProperty("products:write", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Configured_oauth2_is_an_alternative_to_bearer_on_protected_operations_only()
    {
        await using var host = CreateHostWithOAuth2();
        using var document = await OpenApiDocument.FetchAsync(host.CreateClient(), CancellationToken);

        var write = document.Operation("/api/products", "post");
        write.RequiresBearer().ShouldBeTrue();
        write.RequiresScheme(OAuth2SchemeId).ShouldBeTrue();

        document.Operation("/api/products", "get").RequiresScheme(OAuth2SchemeId).ShouldBeTrue();
    }

    private const string OAuth2SchemeId = "OAuth2";

    private const string AuthorizationUrl = "https://idp.example.test/authorize";

    private const string TokenUrl = "https://idp.example.test/token";

    private WebApplicationFactory<Program> CreateHostWithOAuth2() => Factory.WithWebHostBuilder(builder => builder
        .UseSetting("OpenApi:OAuth2:AuthorizationUrl", AuthorizationUrl)
        .UseSetting("OpenApi:OAuth2:TokenUrl", TokenUrl)
        .UseSetting("OpenApi:OAuth2:ClientId", "scalar"));
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

    public JsonElement? SecurityScheme(string schemeId) =>
        _json.RootElement.TryGetProperty("components", out var components)
        && components.TryGetProperty("securitySchemes", out var schemes)
        && schemes.TryGetProperty(schemeId, out var scheme)
            ? scheme
            : null;

    public void Dispose() => _json.Dispose();
}

internal readonly record struct OpenApiOperation(JsonElement Element)
{
    public bool RequiresBearer() => RequiresScheme("Bearer");

    public bool RequiresScheme(string schemeId) =>
        Element.TryGetProperty("security", out var security)
        && security.EnumerateArray().Any(requirement => requirement.TryGetProperty(schemeId, out _));

    public bool DeclaresResponse(string statusCode) =>
        Element.TryGetProperty("responses", out var responses) && responses.TryGetProperty(statusCode, out _);
}
