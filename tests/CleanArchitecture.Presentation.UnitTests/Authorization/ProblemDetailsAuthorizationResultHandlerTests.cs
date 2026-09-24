using System.Security.Claims;
using System.Text.Json;
using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Authorization;

public class ProblemDetailsAuthorizationResultHandlerTests
{
    private const string BearerChallenge = "Bearer error=\"invalid_token\"";

    private static readonly AuthorizationPolicy Policy =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    [Fact]
    public async Task HandleAsync_WhenChallenged_KeepsTheSchemeChallengeAndWritesAnUnauthorizedProblem()
    {
        var context = CreateContext(new FakeAuthenticationService());

        await CreateHandler(context).HandleAsync(Next, context, Policy, PolicyAuthorizationResult.Challenge());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        context.Response.Headers.WWWAuthenticate.ToString().ShouldBe(BearerChallenge);
        context.Response.ContentType.ShouldBe("application/problem+json");
        var problem = await ReadBodyAsync(context);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status401Unauthorized);
        problem.GetProperty("title").GetString().ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task HandleAsync_WhenForbidden_WritesAForbiddenProblem()
    {
        var context = CreateContext(new FakeAuthenticationService());

        await CreateHandler(context).HandleAsync(Next, context, Policy, PolicyAuthorizationResult.Forbid());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        context.Response.ContentType.ShouldBe("application/problem+json");
        var problem = await ReadBodyAsync(context);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status403Forbidden);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
    }

    [Fact]
    public async Task HandleAsync_WhenAuthorized_InvokesTheNextMiddlewareWithoutWritingABody()
    {
        var context = CreateContext(new FakeAuthenticationService());
        var nextInvoked = false;

        await CreateHandler(context).HandleAsync(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            context,
            Policy,
            PolicyAuthorizationResult.Success());

        nextInvoked.ShouldBeTrue();
        context.Response.Body.Length.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenTheSchemeChallengeIsNotA401_LeavesTheResponseUntouched()
    {
        var context = CreateContext(new FakeAuthenticationService(challengeStatusCode: StatusCodes.Status302Found));

        await CreateHandler(context).HandleAsync(Next, context, Policy, PolicyAuthorizationResult.Challenge());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status302Found);
        context.Response.Body.Length.ShouldBe(0);
    }

    private static Task Next(HttpContext context) => Task.CompletedTask;

    private static ProblemDetailsAuthorizationResultHandler CreateHandler(HttpContext context) =>
        new(context.RequestServices.GetRequiredService<IProblemDetailsService>());

    private static DefaultHttpContext CreateContext(IAuthenticationService authenticationService)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .AddSingleton(authenticationService)
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services,
            Response = { Body = new MemoryStream() },
        };
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);

        return document.RootElement.Clone();
    }

    /// <summary>Behaves like JwtBearer: sets the status code and the challenge header without writing a body.</summary>
    private sealed class FakeAuthenticationService(int challengeStatusCode = StatusCodes.Status401Unauthorized)
        : IAuthenticationService
    {
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            context.Response.StatusCode = challengeStatusCode;
            context.Response.Headers.WWWAuthenticate = BearerChallenge;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task SignInAsync(
            HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();
    }
}
