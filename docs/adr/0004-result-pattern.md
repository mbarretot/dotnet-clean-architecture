# 4. Result pattern for expected failures, exceptions for the unexpected

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Invalid input, missing products and duplicate SKUs are expected failures. Exceptions hide these paths and risk coupling business rules to HTTP.

## Decision

Return `Result`/`Result<T>` with typed errors for expected failures; reserve exceptions for unexpected faults. Validation produces failed results, constructors reject inconsistent success/error combinations, and Presentation centrally translates errors into ProblemDetails.

## Outcome

Validation/failure/problem errors map to 400, not-found to 404, conflict to 409 and unauthorized to 401; unexpected exceptions are logged and return a generic 500.

## Consequences

### Benefits

- Signatures expose expected failure paths.
- Stable error codes are testable while the domain remains HTTP-independent.

### Trade-offs

- Callers must explicitly inspect or compose results; generic validation needs a compiled result factory.
- Database-only failures, such as a concurrent duplicate SKU, still arrive as exceptions.
- Concurrency originally returned 500; [ADR-0015](0015-product-stock-reserved-with-the-order.md) later introduced the 409 `Concurrency.Conflict` mapping.

## Alternatives

- **Exception-driven control flow:** fewer checks, but implicit failure paths.
- **ErrorOr/FluentResults/OneOf:** richer utilities, but another dependency for a small contract.

## References

- [Result invariants](../../src/CleanArchitecture.SharedKernel/Results/Result.cs)
- [Validation behavior](../../src/CleanArchitecture.Application/Behaviors/ValidationBehavior.cs)
- [HTTP mapping](../../src/CleanArchitecture.Presentation/Extensions/ResultExtensions.cs)
- [Exception mapping](../../src/CleanArchitecture.Presentation/Middleware/GlobalExceptionHandler.cs)
- [Result tests](../../tests/CleanArchitecture.SharedKernel.UnitTests/Results/ResultTests.cs)
- [ADR-0003](0003-in-house-mediator.md)
