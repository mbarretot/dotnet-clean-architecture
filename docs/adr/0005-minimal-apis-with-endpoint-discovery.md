# 5. Minimal APIs with `IEndpoint` discovery

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

The HTTP layer is thin: each endpoint binds a request, sends one command or query, and maps the `Result` to a
response. MVC controllers add a class hierarchy, filters, and model-binding conventions this layer does not need.
Plain Minimal APIs, on the other hand, tend to pile every `MapGet`/`MapPost` into `Program.cs`.

## Decision

Use ASP.NET Core Minimal APIs, with one class per endpoint discovered by assembly scanning.

- Each endpoint implements [`IEndpoint`](../../src/CleanArchitecture.Presentation/Endpoints/IEndpoint.cs)
  (`MapEndpoint(IEndpointRouteBuilder)`) and lives next to its feature, e.g.
  [`Endpoints/Products/GetProducts.cs`](../../src/CleanArchitecture.Presentation/Endpoints/Products/GetProducts.cs).
- [`EndpointExtensions.AddEndpoints`](../../src/CleanArchitecture.Presentation/Extensions/EndpointExtensions.cs)
  registers every concrete `IEndpoint` in the assembly; `MapEndpoints` resolves and maps them; `MapApi` also maps
  health probes, the OpenAPI document, and Scalar (Development only).
- [`Program.cs`](../../src/CleanArchitecture.Presentation/Program.cs) only calls `AddEndpoints` and `MapApi`; adding
  an endpoint never touches it.
- Endpoints declare their own metadata (name, tags, summary, `Produces`, authorization policy) and return
  `TypedResults`, which feeds the built-in OpenAPI document (`Microsoft.AspNetCore.OpenApi`, rendered by Scalar).

## Consequences

**Positive**

- One small file per route: easy to find, review, and delete.
- `Program.cs` stays a short composition root.
- Typed results give accurate OpenAPI metadata and make handlers unit-testable.

**Negative**

- Discovery is reflection-based at startup and implicit: a class that forgets to implement `IEndpoint` is silently
  not mapped (integration tests are the safety net).
- Per-group conventions (a shared route prefix, shared filters) are repeated in each endpoint rather than declared
  once on a `MapGroup`.
- Assembly scanning is not trimming/AOT-friendly without extra work.

## Alternatives considered

- **MVC controllers.** Familiar and convention-rich; heavier for thin handlers.
- **All routes in `Program.cs`.** Fine for a demo, unmanageable as endpoints grow.
- **Carter or FastEndpoints.** Provide the same modular pattern plus more; an extra dependency for what is here a
  ten-line interface and extension method.
