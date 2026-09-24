using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Extensions;

/// <summary>Container Apps probes hit /alive and /health in Production, so the endpoints must exist outside Development.</summary>
public sealed class ServiceDefaultsHealthEndpointsTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task MapDefaultEndpoints_MapsHealthEndpointsInEveryEnvironment(string environmentName)
    {
        await using var app = BuildApp(environmentName);

        var routes = GetRouteEndpoints(app).Select(endpoint => endpoint.RoutePattern.RawText).ToList();

        routes.ShouldContain("/health");
        routes.ShouldContain("/alive");
    }

    [Fact]
    public async Task Alive_IgnoresChecksNotTaggedLive()
    {
        await using var app = BuildApp("Production", registerUnhealthyDependency: true);

        var statusCode = await InvokeAsync(app, "/alive");

        statusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Health_IncludesDependencyChecks()
    {
        await using var app = BuildApp("Production", registerUnhealthyDependency: true);

        var statusCode = await InvokeAsync(app, "/health");

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }

    private static WebApplication BuildApp(string environmentName, bool registerUnhealthyDependency = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        builder.AddDefaultHealthChecks();

        if (registerUnhealthyDependency)
        {
            builder.Services.AddHealthChecks()
                .AddCheck("dependency", () => HealthCheckResult.Unhealthy(), ["ready"]);
        }

        var app = builder.Build();
        app.MapDefaultEndpoints();
        return app;
    }

    private static IEnumerable<RouteEndpoint> GetRouteEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>();

    private static async Task<int> InvokeAsync(WebApplication app, string path)
    {
        var endpoint = GetRouteEndpoints(app).Single(endpoint => endpoint.RoutePattern.RawText == path);

        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        return context.Response.StatusCode;
    }
}
