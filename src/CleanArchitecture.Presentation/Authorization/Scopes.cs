namespace CleanArchitecture.Presentation.Authorization;

/// <summary>OAuth 2.0 scopes the API understands; values are case-sensitive, as RFC 6749 defines them.</summary>
public static class Scopes
{
    /// <summary>Raw JWT claim name; JwtBearer's inbound claim mapping is disabled, so it is not rewritten.</summary>
    public const string ClaimType = "scope";

    public const string ProductsWrite = "products:write";

    public const string OrdersWrite = "orders:write";
}
