# 5. Minimal APIs with `IEndpoint` discovery

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

HTTP handlers only bind requests, dispatch use cases and map results. Controllers add unused conventions; unstructured Minimal APIs crowd Program.cs.

## Decision

Use one `IEndpoint` class per route, discovered by assembly scanning. Each declares its metadata, authorization and typed responses; Program remains the composition root.

## Outcome

Adding an endpoint does not require editing Program; names, tags, summaries and response schemas feed built-in OpenAPI. `MapApi` also exposes health probes and OpenAPI, with Scalar restricted to Development.

## Consequences

### Benefits

- Small feature-local files are easy to find and review.
- Typed responses feed OpenAPI metadata and support focused tests.

### Trade-offs

- Forgetting `IEndpoint` silently prevents route discovery; integration tests are the safety net.
- Per-endpoint metadata/conventions can repeat.
- Reflection discovery needs additional work for trimming/AOT.

## Alternatives

- **MVC controllers:** familiar, but heavier for thin handlers.
- **Program-only routes:** simple initially, difficult to navigate at scale.
- **Carter/FastEndpoints:** modular discovery plus features, but an extra dependency.

## References

- [Endpoint contract](../../src/CleanArchitecture.Presentation/Endpoints/IEndpoint.cs)
- [Discovery and mapping](../../src/CleanArchitecture.Presentation/Extensions/EndpointExtensions.cs)
- [Example endpoint](../../src/CleanArchitecture.Presentation/Endpoints/Products/GetProducts.cs)
- [HTTP lifecycle tests](../../tests/CleanArchitecture.IntegrationTests/Products/ProductLifecycleTests.cs)
