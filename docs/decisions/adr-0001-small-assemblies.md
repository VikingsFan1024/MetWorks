# ADR 0001: Keep assemblies small and well-defined


## Status
Accepted


## Context
Assemblies tend to grow large and complex, making it difficult to reason about boundaries.

## Decision
Each assembly must expose the minimum necessary surface ara. If two assemblies share too much, either merge them into a single assembly or use InternalsVisibleTo for friend assemblies.

## Consequences
- Assemblies remain small and teachable.
- Exceptions require explicit documentation.
- Friend assemblies risk exposing internals, so use sparingly.
