# 18. API versioning with a default version

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Contracts need an evolution path without changing every existing route. Introducing versioning must not itself break unversioned clients.

## Decision

Read versions from the api-version query parameter or X-Api-Version header, defaulting to 1.0. Map IEndpoint implementations into an Asp.Versioning route group and generate one OpenAPI document per version.

## Outcome

Current URLs and `/openapi/v1.json` remain unchanged. Responses advertise supported versions; unsupported versions return 400, while health/document endpoints remain unversioned. Each operation documents both version inputs and retains the existing security transformers.

## Consequences

### Benefits

- Existing clients continue without sending a version.
- Future groups can carry distinct contracts and documentation; the approved snapshot exposes contract drift.

### Trade-offs

- Query/header versions are less visible than path versions and may be dropped by caches/proxies.
- Document generation depends on Asp.Versioning’s OpenAPI integration and analyzers.

## Alternatives

- **URL segments:** explicit, but would change current routes.
- **Media types:** precise, but awkward for browsers, Scalar and simple clients.

## References

- [Versioned mapping](../../src/CleanArchitecture.Presentation/Extensions/EndpointExtensions.cs)
- [Version constants](../../src/CleanArchitecture.Presentation/Endpoints/ApiVersions.cs)
- [Document generation](../../src/CleanArchitecture.Presentation/Extensions/OpenApiExtensions.cs)
- [Version tests](../../tests/CleanArchitecture.IntegrationTests/Versioning/ApiVersioningTests.cs)
- [ADR-0019](0019-quality-gates-in-ci.md)
