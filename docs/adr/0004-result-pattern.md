# 4. Result pattern for expected failures, exceptions for the unexpected

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Use cases routinely fail for expected reasons: invalid input, an unknown product, a duplicate SKU. Throwing for
these makes failure paths invisible in method signatures, couples the domain to HTTP-flavoured exception types, and
turns control flow into `try/catch`.

## Decision

Model expected failures as values; reserve exceptions for bugs and infrastructure faults.

- [`Result`](../../src/CleanArchitecture.SharedKernel/Results/Result.cs) and
  [`Result<T>`](../../src/CleanArchitecture.SharedKernel/Results/ResultOfT.cs) carry success or an
  [`Error`](../../src/CleanArchitecture.SharedKernel/Results/Error.cs) (code, description,
  [`ErrorType`](../../src/CleanArchitecture.SharedKernel/Results/ErrorType.cs)). The constructor rejects invalid
  combinations (a success with an error, a failure without one).
- Domain errors are declared once, e.g.
  [`ProductErrors`](../../src/CleanArchitecture.Domain/Products/ProductErrors.cs).
- Every command and query returns `Result` / `Result<T>` by contract (see [ADR-0003](0003-in-house-mediator.md)).
- [`ValidationBehavior`](../../src/CleanArchitecture.Application/Behaviors/ValidationBehavior.cs) turns
  FluentValidation failures into a failed result with a
  [`ValidationError`](../../src/CleanArchitecture.SharedKernel/Results/ValidationError.cs) instead of throwing.
- Presentation maps errors to RFC 7807 responses in
  [`ResultExtensions.ToProblem`](../../src/CleanArchitecture.Presentation/Extensions/ResultExtensions.cs):
  `Validation` → 400 validation problem, `NotFound` → 404, `Conflict` → 409, `Unauthorized` → 401,
  `Failure`/`Problem` → 400.
- Anything thrown is caught by
  [`GlobalExceptionHandler`](../../src/CleanArchitecture.Presentation/Middleware/GlobalExceptionHandler.cs), logged
  in full, and returned as a generic 500 `ProblemDetails` without internal details.

## Consequences

**Positive**

- Failure modes are visible in the signature, and handlers read as straight-line code.
- The domain stays free of HTTP concerns; one mapping table decides status codes.
- Error codes (`Product.NotFound`, `Product.SkuAlreadyExists`) are stable, testable contract values.

**Negative**

- More ceremony than throwing: every call site checks `IsFailure` or uses `Match` / `Bind`
  ([`SharedKernel/Results/ResultExtensions.cs`](../../src/CleanArchitecture.SharedKernel/Results/ResultExtensions.cs)).
- Failures raised by the database rather than by code still arrive as exceptions and become 500s. For example, a
  concurrent duplicate SKU that slips past `ExistsBySkuAsync` hits the unique index, and a concurrency conflict
  ([ADR-0007](0007-optimistic-concurrency-xmin.md)) is not translated to 409.
- `ValidationBehavior` needs a compiled factory to build a failed `Result<T>` generically.

## Alternatives considered

- **Exceptions plus exception-to-status middleware.** Less code per call site; failure paths become implicit and
  exceptions are costly on hot paths.
- **A library such as ErrorOr, FluentResults or OneOf.** Richer APIs; another dependency for a small, stable type.
