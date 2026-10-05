# 2. Clean Architecture layering with an executable dependency rule

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Business rules must remain independent of HTTP and persistence. Written layering conventions alone can erode through convenient outward references.

## Decision

Use five projects with inward references: Domain → SharedKernel; Application → Domain/SharedKernel; Infrastructure → Application/Domain/SharedKernel; Presentation composes those layers and ServiceDefaults. Inner layers own ports; Infrastructure implements them.

## Outcome

Domain and Application can be tested without a web host or database; architecture tests reject outward dependencies, EF/ASP.NET type references and handler/repository convention violations.

## Consequences

### Benefits

- Dependency rules become executable checks rather than review preferences.
- Feature folders inside Application retain use-case discoverability.

### Trade-offs

- More interfaces, projects and mappings than simple CRUD needs.
- Repository methods replace direct Application `IQueryable` access.
- Compiled-type checks cannot detect every indirect coupling, such as configuration keys.

## Alternatives

- **One project with folders:** simpler, but no assembly boundary.
- **Vertical slices:** fewer abstractions, but weaker core isolation for this reference.
- **Review-only conventions:** cheaper, but violations can drift unnoticed.

## References

- [Solution](../../CleanArchitecture.slnx)
- [Dependency rules](../../tests/CleanArchitecture.ArchitectureTests/DependencyTests.cs)
- [Repository conventions](../../tests/CleanArchitecture.ArchitectureTests/RepositoryConventionTests.cs)
- [Handler conventions](../../tests/CleanArchitecture.ArchitectureTests/HandlerConventionTests.cs)
- [Command/query conventions](../../tests/CleanArchitecture.ArchitectureTests/CommandQueryConventionTests.cs)
