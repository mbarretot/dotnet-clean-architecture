# 1. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

This repository is a reference implementation meant to be studied and adapted. Its most useful content is not the
code itself but *why* the code is shaped the way it is: why there is no MediatR, why failures are results, why
domain events are dispatched from an EF Core interceptor. That reasoning is scattered across XML doc comments,
commit messages, and the README, where it is easy to miss and hard to challenge.

## Decision

Record every significant architectural decision as a short Architecture Decision Record (ADR) in `docs/adr/`.

- One Markdown file per decision, named `NNNN-kebab-case-title.md`, numbered sequentially and never reused.
- Every ADR uses the same template: **Status**, **Date**, **Context**, **Decision**, **Consequences** (positive and
  negative), **Alternatives considered**.
- Claims link to the code that implements them (relative links into `src/`, `tests/`, `deploy/`, `terraform/`), so
  an ADR can be verified and goes visibly stale when the code moves.
- Accepted ADRs are not rewritten. A changed decision gets a new ADR, and the old one is marked
  *Superseded by ADR-NNNN*.
- [`README.md`](README.md) in this folder is the index.

Decisions that predate the log are recorded retroactively and dated with the commit that introduced them.

## Consequences

**Positive**

- Newcomers can learn the architecture decision by decision instead of reverse-engineering it.
- Trade-offs, including known weaknesses, sit next to the decision instead of being rediscovered.
- Proposed changes can be argued against an explicit record.

**Negative**

- ADRs must be kept in sync with the code; a stale ADR misleads.
- Retroactive ADRs describe reasons as far as they can be inferred from the code and history, not as they were
  originally debated.

## Alternatives considered

- **Rationale in code comments only.** Good for local choices, poor for cross-cutting ones, and not discoverable
  as a list.
- **Rationale in the README.** The README is a usage guide; long rationale would bury the quick start.
- **An external wiki.** Drifts from the code and is not reviewed in the same pull request.
