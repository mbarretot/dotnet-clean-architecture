# 9. Provider-agnostic JWT bearer, scope-based policies, secure by default

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

The API must authenticate callers and restrict writes without tying the code to one identity provider: the same
build runs against Keycloak locally ([ADR-0010](0010-keycloak-local-identity-provider.md)), self-minted tokens in
tests ([ADR-0011](0011-integration-tests-testcontainers.md)), and whatever OIDC issuer a deployment uses. With
per-endpoint opt-in authorization, a new endpoint that forgets `RequireAuthorization()` is public by accident.

## Decision

Validate OAuth 2.0 access tokens with the stock JwtBearer handler, authorize writes by scope, and deny anonymous
access unless an endpoint opts out.

- [`AuthenticationExtensions`](../../src/CleanArchitecture.Presentation/Extensions/AuthenticationExtensions.cs)
  registers `AddJwtBearer` with options bound from `Authentication:Schemes:Bearer` (the section `dotnet user-jwts`
  writes). An issuer is plugged in through configuration alone; with none configured the app still starts and
  protected requests return 401.
- Inbound claim mapping is disabled, so claims keep their JWT names; `sub` is the user id read by
  [`CurrentUser`](../../src/CleanArchitecture.Infrastructure/Identity/CurrentUser.cs) for auditing.
- The `ProductsWrite` and `OrdersWrite` policies require the `products:write` and `orders:write`
  [scopes](../../src/CleanArchitecture.Presentation/Authorization/Scopes.cs), checked by
  [`ScopeAuthorizationHandler`](../../src/CleanArchitecture.Presentation/Authorization/ScopeAuthorizationHandler.cs),
  which accepts both a space-delimited `scope` claim and one claim per scope.
- A **fallback policy** requires an authenticated user for every endpoint without authorization metadata. Public
  routes opt out with `AllowAnonymous()` in
  [`EndpointExtensions.MapApi`](../../src/CleanArchitecture.Presentation/Extensions/EndpointExtensions.cs): `/health`,
  `/alive`, the OpenAPI document, and Scalar (Development only).
- [`AnonymousEndpointConventionTests`](../../tests/CleanArchitecture.Presentation.UnitTests/Authorization/AnonymousEndpointConventionTests.cs)
  pins that allow-list, and
  [`BearerSecuritySchemeTransformer`](../../src/CleanArchitecture.Presentation/OpenApi/BearerSecuritySchemeTransformer.cs)
  documents 401 (and 403 for scoped policies) on protected operations.
- [`ProblemDetailsAuthorizationResultHandler`](../../src/CleanArchitecture.Presentation/Authorization/ProblemDetailsAuthorizationResultHandler.cs)
  wraps the default authorization result handler and writes those 401/403 responses as RFC 9457 `ProblemDetails`
  through `IProblemDetailsService`. The scheme challenges first, so `WWW-Authenticate` (including
  `error="invalid_token"`) is preserved, and the body never reveals why token validation failed.

## Consequences

**Positive**

- Any standards-compliant issuer works (Keycloak, Entra ID, Auth0, `dotnet user-jwts`) with no code change.
- New endpoints are protected by default; making one public is an explicit, test-visible act.
- Scopes express API permissions independently of user roles, which suits service-to-service clients.
- Authentication and authorization failures share the error shape of every other response.

**Negative**

- Authorization is coarse: one write scope per resource, with no resource-level policies. Order ownership is checked
  in the application handlers ([ADR-0014](0014-order-aggregate-references-products-by-id.md)), not by a policy.
- A misconfigured deployment fails closed (every call 401) rather than failing at startup, which can be slower to
  diagnose.
- Scope handling has to tolerate issuer differences in claim shape, which the custom handler owns.

## Alternatives considered

- **Opt-in `RequireAuthorization()` per endpoint without a fallback.** Simpler to read; one omission exposes an
  endpoint.
- **Role-based authorization.** Fits user-centric apps; roles are provider-specific and fit clients poorly.
- **A provider SDK (e.g. Microsoft.Identity.Web).** Richer integration with one provider; couples the API to it.
