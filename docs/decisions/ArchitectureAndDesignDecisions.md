# MetWorks Design Rules and Invariants

This document captures the agreed design rules, invariants, and actionable backlog for MetWorks. Add it to the repository at `docs/design-rules.md` so it is versioned with the code.

---

## Repositories Solutions and Project Layout
- One authoritative project per assembly.
- One solution per assembly that contains:
  - the assembly project (library),
  - a development harness (console app) for local manual testing/diagnostics.
- Physical project files are referenced in-place by consumers (no copies).
- Optional: maintain a single workspace solution for cross-cutting refactors, opened only when needed.
- Put cross-solution tooling scripts under `/tools` and repo-level build/props under `Directory.Build.*`.

---

## Public Surface Minimization
- Minimize the public API surface of each assembly. Prefer `internal`/`private` unless required for external consumption.
- Required public changes must include a short justification in the PR and update `PublicApi.txt`.
- CI will run a PublicApiGenerator diff against committed `PublicApi.txt` and fail on unexpected changes.
- Prefer `InternalsVisibleTo` (with strong naming) for deep access between a small, trusted set of assemblies rather than enlarging public surfaces.

---

## Namespaces Types and Registration
- Types used by DDI are canonical tokens: `namespace.typeName[.version]`.
- Treat namespaces as independent (no implicit nesting). `tempest` and `tempest.udp` are distinct.
- All types used by DDI must be explicitly registered in the `TypeRegistry` at bootstrap. No fuzzy cross-namespace lookup.

Resolution rules:
- Fully qualified declared tokens resolve exactly or fail.
- Short names must resolve to a type registered in the current module namespace; otherwise validation fails.
- If multiple versions exist and YAML omitted a version, pick the highest deterministically and emit a warning. Per-type versioning is discouraged (see Versioning Policy).

---

## Short-name Rule (No Duplication)
- If a type exists in the current module namespace, YAML MUST use the short name.
- If the type is outside the current namespace, YAML MUST use the fully qualified token.
- Violations fail validation; this avoids duplicated references and enforces canonical references.

---

## Component Keys and Ordering (Deterministic Lifecycle)
- The DDI YAML `components` array is authoritative for instantiation order. Declarations MUST be ordered in the required runtime sequence.
- Outputs, initializers, or references must point only to previously-declared keys. Forward references are errors.
- Circular dependencies are rejected; validation must present the cycle path.
- Loader starts components in the declared order and stops them in reverse order.

---

## Versioning Policy
- Single application-level version only (`app.version`). No per-type or per-component version selection at runtime.
- `TypeRegistry` records assembly provenance metadata for debugging, but YAML cannot select per-type versions.
- Rationale: minimize complexity until the team grows and needs per-type versioning.

---

## Settings Overrides and Provenance
- Settings are optional. Components may use DDI args (hard-coded) if no settings store is present.
- When used, the path-based settings store is authoritative at runtime. DDI seeds initial values into it with provenance `ddi:<file>@<hash>`.
- Components declare `overridable` fields in DDI YAML. Only these fields may be changed at runtime via settings overrides.
- Overrides are recorded with provenance (source, timestamp, author, originFileHash) and persisted as newline JSON under `artifacts/settings-overrides/`.
- `SettingsBridge` (planned) will:
  - seed settings from DDI,
  - subscribe to observer notifications,
  - validate overrides against `overridable` and field shapes,
  - invoke a component’s reconfiguration API or request deterministic restart,
  - persist override audit entries.
- Settings cannot create, rename, or change component keys/types at runtime; such attempts are rejected.

---

## Reconfiguration and Component Contract
- Prefer changing component code rather than introducing adapters. Adapters are acceptable only if refactoring cost becomes prohibitive.
- DDI-managed components should implement a minimal contract:
  - `StartAsync` / `StopAsync` / `ApplyOverrideAsync` / `SnapshotDiagnostics` / `Dispose`.
- `ApplyOverrideAsync` returns a boolean:
  - `true` = applied in-process;
  - `false` = restart required (LifecycleManager will stop/start deterministically for that component).
- Reconfigure is allowed only for fields listed in `overridable`. Attempts to change identity (key/type) are rejected.

---

## SettingsBridge Behavior (Conceptual)
- `SettingsBridge` is a thin coordinator that:
  - seeds initial settings with provenance,
  - listens for change events,
  - validates requested changes,
  - forwards deltas to component `ApplyOverride` methods,
  - persists overrides and their provenance,
  - schedules deterministic restarts when required.
- `SettingsBridge` implementation only after the settings review verifies readiness (notifications, provenance, atomicity, validation hooks).

---

## Diagnostics Provenance and Artifacts
- All components and lifecycle events attach `sourceKey` and `app.version` to diagnostics and counters.
- Periodic diagnostics snapshots are written to `artifacts/ddi-diagnostics/` with timestamped files and a stable `latest.json`.
- Override audit is recorded under `artifacts/settings-overrides/`.
- Diagnostics snapshots include per-component counters (received, parsed, dropped, malformed, restarts) and last-applied override provenance.

---

## Code Generation and Loader Expectations
- Code generation must emit:
  - registration calls with canonical tokens and provenance,
  - initializer signatures using fully qualified tokens for cross-namespace parameters,
  - `TypeRegistry` registrations in dependency-safe order.
- The runtime loader validates YAML vs `TypeRegistry` before side effects and fails fast on violations.

---

## Testing and Validation
- Add a YAML validator that enforces:
  - namespace short-name rule,
  - type resolution against `TypeRegistry`,
  - component ordering (no forward refs),
  - cycle detection.
- Unit & integration tests should cover:
  - settings seed and override accept/reject flows,
  - `ApplyOverride` behaviors (in-process vs restart),
  - diagnostics snapshot and audit persistence.
- Provide a migration/lint tool that suggests fixes for short-name or ordering violations.

---

## Operational and Security Constraints
- Settings writes must be auditable. The settings store must provide or be extended to provide provenance metadata.
- Access control (who can write overrides) is out of scope for this doc but must be considered operationally; rejected overrides produce deterministic diagnostics.
- Process settings changes serially per component key to avoid races. `SettingsBridge`/`LifecycleManager` will coordinate sequencing.

---

## Documentation and Traceability
- Record the above in `docs/design-rules.md`. Each project should have a README describing its DevHarness and any `InternalsVisibleTo` relationships.
- Maintain `PublicApi.txt` files per assembly and commit updates intentionally.
- Record decisions or deviations in `docs/decisions/` with date, rationale, and owner.

---

## Actionable Backlog (Recorded)
1. Inspect existing Settings implementation (`settings-review`) and produce `settings-review.md` with a readiness checklist.
2. Produce `TypeRegistry` resolver and YAML validator enforcing strict namespace/ordering rules.
3. Implement `SettingsBridge` only after (1) passes readiness. Deliverables: `SeedFromDdi`, `OnSettingsChanged`, audit persistence.
4. Add `ApplyOverrideAsync` signatures to UdpReceiver (and modified components) and tests for in-place vs restart behavior.
5. CI checks:
   - Public API diff (PublicApiGenerator) vs `PublicApi.txt`.
   - YAML validation step in pre-start and CI.

---

## Questions and Tradeoffs Worth Remembering
- Single application version simplifies reasoning but prevents in-process per-component upgrades. Revisit if team grows.
- Strict short-name rule prevents duplication but forces longer tokens across namespaces; this is intentional.
- Enforcing component ordering removes runtime convenience but increases determinism and prevents cycles.

---

## Next Step
If this doc is correct, add it to the solution at `docs/design-rules.md`. After that the next task is the settings review: start with "Surface model and storage" in the Settings assembly per the agreed plan.
