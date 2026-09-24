namespace CleanArchitecture.Presentation.OpenApi;

public sealed class OpenApiOAuth2Options
{
    public const string SectionName = "OpenApi:OAuth2";

    public string? AuthorizationUrl { get; init; }

    public string? TokenUrl { get; init; }

    public string? ClientId { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AuthorizationUrl)
        && !string.IsNullOrWhiteSpace(TokenUrl)
        && !string.IsNullOrWhiteSpace(ClientId);
}
