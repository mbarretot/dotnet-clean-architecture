namespace CleanArchitecture.Presentation.Authorization;

/// <summary>Named policies referenced by endpoints, so policy names are never repeated as string literals.</summary>
public static class AuthorizationPolicies
{
    public const string ProductsWrite = "ProductsWrite";

    public const string OrdersWrite = "OrdersWrite";
}
