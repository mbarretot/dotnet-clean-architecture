# 18. API versioning with a default version

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

The API had no way to evolve a contract without breaking existing clients. Adding versioning later would itself be a
breaking change if it moved routes, so the mechanism has to fit the current URLs.

## Decision

Use `Asp.Versioning` with the version read from the `api-version` query parameter or the `X-Api-Version` header, and
`1.0` assumed when neither is sent.

- [`EndpointExtensions`](../../src/CleanArchitecture.Presentation/Extensions/EndpointExtensions.cs) registers
  versioning and maps every `IEndpoint` into a versioned route group with
  [`ApiVersions.V1`](../../src/CleanArchitecture.Presentation/Endpoints/ApiVersions.cs). Endpoints stay unaware of
  versions until one actually needs a v2.
- Responses report `api-supported-versions`; an unsupported version is a 400 problem
  (`https://docs.api-versioning.org/problems#unsupported`).
- [`OpenApiExtensions`](../../src/CleanArchitecture.Presentation/Extensions/OpenApiExtensions.cs) uses
  `Asp.Versioning.OpenApi`: one document per version (`/openapi/v1.json`, unchanged path) with the same security
  transformers, and the version parameter and header documented on each operation.
- Health and OpenAPI endpoints are not versioned.

## Consequences

**Positive**

- Existing clients and URLs keep working; a v2 is one more route group, with its own OpenAPI document.
- The approved OpenAPI snapshot ([ADR-0019](0019-quality-gates-in-ci.md)) makes any contract change visible.

**Negative**

- Query and header versions are less visible than `/v1/` in the path and are easy to drop in caches or proxies.
- Asp.Versioning's analyzers require its OpenAPI integration, so document generation now goes through its API.

## Alternatives considered

- **URL segment (`/api/v1/products`).** Most explicit, but changes every route and breaks every client today.
- **Media-type versioning.** Precise, but awkward for browsers, Scalar and simple clients.
