# 1. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

This reference needs discoverable reasons for cross-cutting choices. Code comments and commit messages alone make those reasons difficult to compare or challenge.

## Decision

Use sequential, never-reused `NNNN-kebab-case-title.md` records and an index to make significant choices discoverable. Standardize concrete evidence links and the sections **Context**, **Decision**, **Outcome**, **Consequences** (**Benefits** and **Trade-offs**), **Alternatives**, and **References**.

## Outcome

Readers can trace each choice to its purpose, resulting behavior, limitations and rejected alternatives. Editorial shortening, factual clarification and link repair may improve an accepted record without changing its meaning; substantive changes require a new ADR and an explicit supersession or amendment.

## Consequences

### Benefits

- Consistent records let readers compare reasoning and challenge trade-offs.
- Preserved statuses/dates retain historical context; retroactive records keep their recorded introduction dates.

### Trade-offs

- Records require maintenance; stale links or claims mislead.
- Retroactive rationale is reconstructed, not proof of the original discussion.

## Alternatives

- **Code comments only:** local context, but no cross-cutting decision index.
- **README rationale:** buries the usage path.
- **External wiki:** separates decisions from source review.

## References

- [Decision index](README.md)
- [Project entry point](../../README.md)
