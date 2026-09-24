# 3. In-house mediator instead of MediatR

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Endpoints dispatch commands and queries to handlers, cross-cutting concerns (logging, validation) wrap every
request as pipeline behaviors, and domain events fan out to notification handlers. MediatR is the usual library for
this in .NET. Neither the code nor its history records *why* it was not used, so this ADR states the trade-offs
neutrally.

## Decision

Ship a small mediator in [`SharedKernel/Messaging`](../../src/CleanArchitecture.SharedKernel/Messaging) and forbid
MediatR.

- `ISender` / [`Sender`](../../src/CleanArchitecture.SharedKernel/Messaging/Sender.cs) resolves the handler, builds
  the `IPipelineBehavior<,>` chain, and caches one closed
  [`RequestHandlerWrapper`](../../src/CleanArchitecture.SharedKernel/Messaging/RequestHandlerWrapper.cs) per request
  type, so only the first dispatch pays for reflection.
- `IPublisher` / [`Publisher`](../../src/CleanArchitecture.SharedKernel/Messaging/Publisher.cs) runs every
  notification handler sequentially, keeps going if one throws, then throws an `AggregateException`.
- [`ICommand`](../../src/CleanArchitecture.SharedKernel/Messaging/ICommand.cs) and
  [`IQuery<T>`](../../src/CleanArchitecture.SharedKernel/Messaging/IQuery.cs) fix the response type to `Result` /
  `Result<T>` (see [ADR-0004](0004-result-pattern.md)).
- [`AddMediator`](../../src/CleanArchitecture.SharedKernel/Messaging/MediatorServiceCollectionExtensions.cs) scans
  assemblies and registers request and notification handlers as scoped;
  [`Application/DependencyInjection.cs`](../../src/CleanArchitecture.Application/DependencyInjection.cs) adds the
  logging and validation behaviors.
- [`MediatRTests.cs`](../../tests/CleanArchitecture.ArchitectureTests/MediatRTests.cs) fails if any layer references
  MediatR.

## Consequences

**Positive**

- No third-party dependency, licence, or major-version upgrade to track for a core abstraction.
- About a dozen small files a reader can study end to end; nothing is hidden behind a package.
- Contracts are tailored: commands and queries are typed to `Result`, which a general-purpose library does not
  impose.

**Negative**

- The repository owns this code, its tests
  ([`SharedKernel.UnitTests/Messaging`](../../tests/CleanArchitecture.SharedKernel.UnitTests/Messaging)), and its
  bugs.
- Fewer features than MediatR: no streaming requests, no pre/post processors, and notifications are always
  published sequentially.
- Developers who know MediatR must learn slightly different registration and types.

## Alternatives considered

- **MediatR.** Mature and widely known; adds an external dependency and more surface than this project uses.
- **Inject handlers directly into endpoints.** No mediator at all; cross-cutting behaviors would then need
  per-handler decorators.
- **A source-generated mediator.** Avoids runtime reflection; adds a build-time dependency and generated code that
  is harder to read in a reference project.
