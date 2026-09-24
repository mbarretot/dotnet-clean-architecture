using System.Security.Claims;
using CleanArchitecture.Application;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Authorization;

public sealed class OrderEndpointAuthorizationTests : IAsyncLifetime
{
    private WebApplication _app = null!;

    public ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApplication();
        builder.Services.AddAuthenticationAndAuthorization();
        builder.Services.AddEndpoints(typeof(Program).Assembly);

        _app = builder.Build();
        _app.MapEndpoints();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private List<RouteEndpoint> OrderEndpoints =>
        ((IEndpointRouteBuilder)_app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/orders", StringComparison.Ordinal) == true)
            .ToList();

    [Fact]
    public void OrderEndpoints_ShouldExist() => OrderEndpoints.Count.ShouldBe(4);

    [Fact]
    public void EveryOrderEndpoint_ShouldRequireAuthorizationAndNotAllowAnonymous()
    {
        var unprotected = OrderEndpoints
            .Where(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0
                || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.DisplayName)
            .ToList();

        unprotected.ShouldBeEmpty(string.Join(", ", unprotected));
    }

    [Fact]
    public void ReadOrderEndpoints_ShouldOnlyRequireAnAuthenticatedUser()
    {
        var readEndpoints = OrderEndpoints.Where(IsReadOnly).ToList();

        readEndpoints.Count.ShouldBe(2);
        readEndpoints.ShouldAllBe(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .All(authorizeData => authorizeData.Policy == null && authorizeData.Roles == null));
    }

    [Fact]
    public void WriteOrderEndpoints_ShouldRequireTheOrdersWritePolicy()
    {
        var writeEndpoints = OrderEndpoints.Where(endpoint => !IsReadOnly(endpoint)).ToList();

        writeEndpoints.Count.ShouldBe(2);
        writeEndpoints.ShouldAllBe(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(authorizeData => authorizeData.Policy == AuthorizationPolicies.OrdersWrite));
    }

    [Theory]
    [InlineData("orders:write", true)]
    [InlineData("products:write", false)]
    [InlineData("orders:read", false)]
    public async Task OrdersWritePolicy_EvaluatesTheScopeClaimOfAnAuthenticatedUser(string scope, bool expected)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Scopes.ClaimType, scope)], "Bearer"));

        var result = await AuthorizeAsync(user);

        result.Succeeded.ShouldBe(expected);
    }

    [Fact]
    public async Task OrdersWritePolicy_RejectsAnonymousUserEvenWithTheScopeClaim()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Scopes.ClaimType, Scopes.OrdersWrite)]));

        var result = await AuthorizeAsync(anonymous);

        result.Succeeded.ShouldBeFalse();
    }

    private async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        return await authorizationService.AuthorizeAsync(user, AuthorizationPolicies.OrdersWrite);
    }

    private static bool IsReadOnly(RouteEndpoint endpoint) =>
        (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []).All(HttpMethods.IsGet);
}
