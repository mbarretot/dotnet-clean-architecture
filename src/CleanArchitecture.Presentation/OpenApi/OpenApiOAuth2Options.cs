namespace CleanArchitecture.Presentation.OpenApi;

/// <summary>
/// Optional OAuth 2.0 Authorization Code flow advertised in the OpenAPI document so Scalar can sign a developer in.
/// Provider-agnostic: the URLs are the ones the developer's browser opens. Nothing is advertised unless every value is set.
/// </summary>
public sealed class OpenApiOAuth2Options
{
    public const string SectionName = "OpenApi:OAuth2";

    public string? AuthorizationUrl { get; init; }

    public string? TokenUrl { get; init; }

    /// <summary>Public client (no secret) the reference UI signs in with, using PKCE.</summary>
    public string? ClientId { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AuthorizationUrl)
        && !string.IsNullOrWhiteSpace(TokenUrl)
        && !string.IsNullOrWhiteSpace(ClientId);
}
