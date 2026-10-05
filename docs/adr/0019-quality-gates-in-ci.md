# 19. Quality and supply-chain gates in CI/CD

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

CI built, tested and reported coverage, but nothing failed when coverage dropped, when tests passed without
asserting much, when the published contract changed by accident, or when the image or infrastructure code picked up
a known weakness. Published images were neither inventoried nor signed.

## Decision

Add gates that fail fast where a fix is cheap, and reports where a human must judge.

| Gate | Where | Fails when |
|---|---|---|
| Coverage threshold | [`ci.yml`](../../.github/workflows/ci.yml) `build-test` | Merged line coverage < 85 % or branch coverage < 80 % |
| OpenAPI contract snapshot | [`OpenApiContractTests`](../../tests/CleanArchitecture.IntegrationTests/OpenApi/OpenApiContractTests.cs) | `/openapi/v1.json` differs from `openapi.v1.approved.json` |
| Mutation testing (Stryker.NET, MTP runner) | [`mutation.yml`](../../.github/workflows/mutation.yml), PRs touching Domain/Application + weekly | Mutation score < 60 % (`break` in `stryker-config.json`) |
| Image vulnerabilities (Trivy) | `ci.yml` `docker-build-check` | Fixable HIGH/CRITICAL vulnerability in the image |
| Terraform lint (tflint + azurerm ruleset) | `ci.yml` `terraform-lint` | Any tflint issue |
| Terraform policy (checkov) | `ci.yml` `terraform-lint` | Any failed check not skipped inline with a reason |
| Load test (k6) | [`load-test.yml`](../../.github/workflows/load-test.yml), weekly + manual | Error rate ≥ 1 %, browse p95 ≥ 300 ms or checkout p95 ≥ 800 ms |

[`cd.yml`](../../.github/workflows/cd.yml) additionally attaches an SPDX SBOM and SLSA provenance to the pushed image,
uploads a CycloneDX SBOM as an artifact, signs the image digest with keyless cosign (Sigstore, GitHub OIDC), and
sends a Trivy SARIF report to code scanning. [`badges.yml`](../../.github/workflows/badges.yml) publishes test-count
and coverage badges to the `badges` branch after CI succeeds on `main`.

- The OpenAPI snapshot is approved by replacing the file (or running the tests with `UPDATE_SNAPSHOTS=1`); a mismatch
  writes `openapi.v1.received.json` next to it. It is hand-rolled rather than Verify because Verify now fails the
  build until a sponsorship status is declared.
- Error-catalogue files (`*Errors.cs`) are excluded from mutation: their strings are asserted by integration tests,
  which Stryker does not run.
- Accepted Terraform findings are documented where they live: three `#checkov:skip` comments on the PostgreSQL
  resources (public access with a firewall, no private endpoint, no geo-redundant backup) and the disabled
  `azurerm_resources_missing_prevent_destroy` rule in [`.tflint.hcl`](../../terraform/.tflint.hcl).

## Consequences

**Positive**

- Regressions in coverage, test strength, contract, image or IaC hygiene fail a build instead of reaching `main`.
- Consumers of the image can verify its signature and inspect its SBOM and provenance.

**Negative**

- Trivy can fail CI because of a newly disclosed base-image vulnerability unrelated to the change; the fix is
  usually a base-image bump.
- Mutation and load tests are slower and run on narrower triggers, so a regression there can land and surface on the
  next scheduled run.
- Thresholds are numbers someone must keep honest; raising them as the suite improves is a manual step.
- The skipped checkov findings are real hardening work (private networking, geo-redundant backups) for production.

## Alternatives considered

- **Coverage services (Codecov, Coveralls).** Nicer diffs, but a third-party token and service for a threshold check.
- **Verify for the snapshot.** The standard tool, but it now requires a sponsorship declaration in the build.
- **NBomber instead of k6.** Tests in C#, but k6 runs against the real Compose stack with no extra project.
