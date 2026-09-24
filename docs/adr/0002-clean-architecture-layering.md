# 2. Clean Architecture layering with an executable dependency rule

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

The API needs business rules that do not depend on HTTP, EF Core, or PostgreSQL, so they can be tested in isolation
and outlive infrastructure choices. Layering conventions that are only documented tend to erode one convenient
`using` at a time.

## Decision

Split the solution ([`CleanArchitecture.slnx`](../../CleanArchitecture.slnx)) into five layers with inward-only
dependencies:

| Project | Owns | May depend on |
|---|---|---|
| [`SharedKernel`](../../src/CleanArchitecture.SharedKernel) | `Result`, `Error`, base entities, mediator primitives | — |
| [`Domain`](../../src/CleanArchitecture.Domain) | `Product` aggregate, value objects, domain events, `IProductRepository` | `SharedKernel` |
| [`Application`](../../src/CleanArchitecture.Application) | Commands, queries, handlers, validators, pipeline behaviors | `Domain`, `SharedKernel` |
| [`Infrastructure`](../../src/CleanArchitecture.Infrastructure) | EF Core persistence, repositories, identity, time | `Application`, `Domain`, `SharedKernel` |
| [`Presentation`](../../src/CleanArchitecture.Presentation) | Minimal API endpoints, composition root | the layers above + `ServiceDefaults` |

Inner layers own the abstractions and outer layers implement them: `IProductRepository` (Domain) is implemented by
`ProductRepository` (Infrastructure); `IUnitOfWork` and `IDateTimeProvider` (SharedKernel) likewise.

The rule is enforced by tests in
[`tests/CleanArchitecture.ArchitectureTests`](../../tests/CleanArchitecture.ArchitectureTests):

- [`DependencyTests.cs`](../../tests/CleanArchitecture.ArchitectureTests/DependencyTests.cs) uses NetArchTest to
  reject outward references, and forbids `Microsoft.EntityFrameworkCore` and `Microsoft.AspNetCore` in Domain and
  Application.
- [`HandlerConventionTests.cs`](../../tests/CleanArchitecture.ArchitectureTests/HandlerConventionTests.cs),
  [`CommandQueryConventionTests.cs`](../../tests/CleanArchitecture.ArchitectureTests/CommandQueryConventionTests.cs)
  and [`RepositoryConventionTests.cs`](../../tests/CleanArchitecture.ArchitectureTests/RepositoryConventionTests.cs)
  pin the conventions: sealed `*Handler` classes, sealed record commands/queries, and a sealed Infrastructure
  implementation for every Domain repository interface.

## Consequences

**Positive**

- Domain and Application are unit-testable without a database or web host.
- A layering violation fails `dotnet test` in CI instead of relying on code review.
- Infrastructure can change (for example the database provider) without touching use cases.

**Negative**

- More projects, interfaces, and mapping code than a CRUD-sized domain strictly needs.
- Application cannot compose EF Core `IQueryable` queries; data access goes through repository methods.
- NetArchTest inspects type references in compiled assemblies; it cannot catch every form of indirect coupling
  (for example a shared configuration key).

## Alternatives considered

- **Single project with folders.** Simplest, but nothing stops the domain from referencing EF Core.
- **Vertical slice architecture.** Groups by feature instead of layer, with fewer abstractions and weaker domain
  isolation. Use cases here are still organised per feature *inside* Application (`Products/CreateProduct/...`).
- **Convention and code review only.** Cheaper, but erodes silently.
