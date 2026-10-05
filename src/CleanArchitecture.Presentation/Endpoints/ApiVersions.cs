using Asp.Versioning;

namespace CleanArchitecture.Presentation.Endpoints;

public static class ApiVersions
{
    public const string QueryParameter = "api-version";

    public const string HeaderName = "X-Api-Version";

    /// <summary>Formats as <c>v1</c>, matching the default OpenAPI document name.</summary>
    public const string GroupNameFormat = "'v'V";

    public static readonly ApiVersion V1 = new(1, 0);
}
