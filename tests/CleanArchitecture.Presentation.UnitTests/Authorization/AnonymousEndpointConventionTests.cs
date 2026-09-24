using CleanArchitecture.Application;
using CleanArchitecture.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Authorization;

/// <summary>
/// Maps the real HTTP surface (the same <see cref="EndpointExtensions.MapApi"/> call <c>Program.cs</c> makes) in
/// Development, where it is widest, and pins the exact set of anonymous routes. Every other endpoint is protected —
/// explicitly or by the fallback policy — so a new public endpoint fails this test unless it is deliberately allowed.
/// </summary>
public sealed class AnonymousEndpointConventionTests : IAsyncLifetime
{
    private static readonly string[] AllowedAnonymousRoutes =
    [
        "/health",
        "/alive",
        "/openapi/{documentName}.json",
        "/scalar/{documentName?}",
    ];

    /// <summary>Scalar also serves its own static assets (scripts, favicon); their names vary between versions.</summary>
    private const string ScalarAssetsPrefix = "/scalar/";

    private WebApplication _app = null!;

    public ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        // Only endpoint metadata is inspected: Infrastructure (repositories, database) is deliberately not registered.
        builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
        builder.AddDefaultHealthChecks();
        builder.Services.AddApplication();
        builder.Services.AddAuthenticationAndAuthorization();
        builder.Services.AddEndpoints(typeof(Program).Assembly);
        builder.Services.AddApiDocumentation();

        _app = builder.Build();
        _app.MapApi();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private List<RouteEndpoint> Endpoints =>
        ((IEndpointRouteBuilder)_app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

    private List<string?> AnonymousRoutes =>
        Endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

    [Fact]
    public void OnlyAllowListedRoutes_ShouldAllowAnonymousAccess()
    {
        var unexpected = AnonymousRoutes
            .Where(route => !AllowedAnonymousRoutes.Contains(route)
                && route?.StartsWith(ScalarAssetsPrefix, StringComparison.Ordinal) != true)
            .ToList();

        unexpected.ShouldBeEmpty(
            "New anonymous endpoint(s): add them to the allow-list only if they are meant to be public. "
            + string.Join(", ", unexpected));
    }

    public static TheoryData<string> AllowListedRoutes => new(AllowedAnonymousRoutes);

    [Theory]
    [MemberData(nameof(AllowListedRoutes))]
    public void AllowListedRoute_ShouldOptOutOfTheFallbackPolicy(string route) =>
        AnonymousRoutes.ShouldContain(route);

    [Fact]
    public void ProtectedSurface_ShouldIncludeTheFeatureEndpoints()
    {
        var protectedRoutes = Endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        protectedRoutes.ShouldContain("/api/products");
        protectedRoutes.ShouldContain("/api/products/{id:guid}");
    }
}
