# 19. Quality and supply-chain gates in CI/CD

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Build success alone does not protect coverage, assertion strength, API contracts or supply-chain hygiene. Published artifacts also need an inspectable origin and inventory.

## Decision

Use failing gates for actionable regressions and reports for findings requiring judgment. CI keeps build/format checks and enforces line ≥85%, branch ≥80%, contract snapshots, fixable HIGH/CRITICAL image vulnerabilities and unaccepted Terraform lint/policy findings.

## Outcome

Domain/Application mutation checks require ≥60%; scheduled/manual k6 requires errors <1%, browse p95 <300 ms and checkout p95 <800 ms. CD attaches SPDX/SLSA evidence, signs digests using keyless cosign, uploads CycloneDX and Trivy reports; measured tests/coverage remain in CI artifacts and summaries.

## Consequences

### Benefits

- Consumers can inspect image inventory/provenance and verify signatures.
- Snapshot updates are explicit (`UPDATE_SNAPSHOTS=1`); mismatches produce a received file.

### Trade-offs

- Mutation/load checks have narrower triggers; newly disclosed image flaws may fail unrelated changes.
- Thresholds need maintenance; error catalogs excluded from mutation are covered separately by integration tests.
- Accepted IaC skips retain real hardening debt: public networking, no private endpoint/geo-backup and relaxed prevent_destroy linting.

## Alternatives

- **Coverage services:** nicer diffs, but another service/token.
- **Verify snapshots:** rejected to avoid its sponsorship declaration; a hand-rolled snapshot is retained.
- **NBomber:** C# authoring, but k6 directly exercises Compose without another project.

## References

- [CI gates](../../.github/workflows/ci.yml)
- [Publication](../../.github/workflows/cd.yml)
- [Mutation settings](../../tests/CleanArchitecture.Application.UnitTests/stryker-config.json)
- [Load thresholds](../../tests/load/api-load.js)
- [Contract test](../../tests/CleanArchitecture.IntegrationTests/OpenApi/OpenApiContractTests.cs)
- [IaC exceptions](../../terraform/main.tf)
- [Lint exception](../../terraform/.tflint.hcl)
