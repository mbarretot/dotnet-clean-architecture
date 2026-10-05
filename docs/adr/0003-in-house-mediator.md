# 3. In-house mediator instead of MediatR

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Commands, queries and notifications need dispatch plus logging/validation behaviors. History does not establish why MediatR was omitted; this record states observable trade-offs, not an invented motivation.

## Decision

Keep the small in-house mediator and forbid MediatR references. Commands/queries return `Result` types; scoped handlers are assembly-discovered, and cached wrappers build the logging/validation pipeline.

## Outcome

Endpoints dispatch through `ISender`; `IPublisher` invokes notifications sequentially, continues after handler failures, then throws an `AggregateException`.

## Consequences

### Benefits

- Contracts are tailored to the result-based application.
- Dispatch behavior is readable locally, without an external core dependency or its license/major-version upgrade lifecycle.

### Trade-offs

- The repository owns implementation bugs and tests.
- No streaming, pre/post processors or parallel notification publishing.
- Developers must learn registration and contracts different from MediatR.

## Alternatives

- **MediatR:** mature, but adds a dependency and unused surface.
- **Direct handler injection:** removes dispatch, but repeats cross-cutting decorators.
- **Generated mediator:** avoids reflection, but adds build tooling and generated code.

## References

- [Messaging implementation](../../src/CleanArchitecture.SharedKernel/Messaging)
- [Pipeline registration](../../src/CleanArchitecture.Application/DependencyInjection.cs)
- [Mediator tests](../../tests/CleanArchitecture.SharedKernel.UnitTests/Messaging)
- [MediatR prohibition](../../tests/CleanArchitecture.ArchitectureTests/MediatRTests.cs)
- [ADR-0004](0004-result-pattern.md)
