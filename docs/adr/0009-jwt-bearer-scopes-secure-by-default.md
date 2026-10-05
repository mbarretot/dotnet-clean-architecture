# 9. Provider-agnostic JWT bearer, scope-based policies, secure by default

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

The same API must validate tokens from different issuers and protect new endpoints from accidental anonymous access. Write permissions should also work for machine clients.

## Decision

Use configured JwtBearer validation, scope policies and an authenticated fallback policy. Disable inbound claim mapping so `sub` remains the audited user ID; scope handling accepts space-delimited or repeated claims.

## Outcome

Public routes require `AllowAnonymous`: health, aliveness, OpenAPI and Development-only Scalar. Writes require `products:write`/`orders:write`; [ADR-0016](0016-order-lifecycle-state-machine.md) adds `orders:fulfill`. Authorization returns generic RFC 9457 401/403 bodies while preserving `WWW-Authenticate`.

## Consequences

### Benefits

- Provider selection stays in configuration.
- Public access is explicit and pinned by convention tests.

### Trade-offs

- Scopes are coarse; application handlers enforce customer ownership, while fulfillment can act across customers.
- Missing issuer configuration fails closed on requests (401), not at startup.
- Custom scope handling must tolerate issuer claim differences without revealing validation details.

## Alternatives

- **Opt-in endpoint authorization:** an omission exposes a route.
- **Roles:** provider-specific and less suitable for machine permissions.
- **Provider SDK:** richer integration, but couples the API to one issuer.

## References

- [Registration](../../src/CleanArchitecture.Presentation/Extensions/AuthenticationExtensions.cs)
- [Scope handler](../../src/CleanArchitecture.Presentation/Authorization/ScopeAuthorizationHandler.cs)
- [Error responses](../../src/CleanArchitecture.Presentation/Authorization/ProblemDetailsAuthorizationResultHandler.cs)
- [Public allow-list test](../../tests/CleanArchitecture.Presentation.UnitTests/Authorization/AnonymousEndpointConventionTests.cs)
- [ADR-0010](0010-keycloak-local-identity-provider.md)
- [ADR-0014](0014-order-aggregate-references-products-by-id.md)
