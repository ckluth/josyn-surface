# josyn-surface

> The human window into a headless platform — see
> [ADR-030](../josyn-platform/decisions/ADR-030-josyn-surface.md) (vision) and
> [ADR-031](../josyn-platform/decisions/ADR-031-surface-delivery-strategy.md) (delivery strategy).

`josyn-surface` is the coherent, human-facing window onto the otherwise headless JOSYN platform.
It replaces the *capabilities* of the scattered PoC toolbox scripts (not their code) with a unified,
environment-aware surface.

This repo holds the **external** parts of the surface: the shells and the shared backbone. The
platform-resident per-machine agent (ADR-030 D-17) lives in `josyn-backend`, not here.

---

## Current state — MVP-1 (read-only reporting precursor)

MVP-1 is the first increment defined by ADR-031: **read-only**, **local CLI**, **DEV only**.
It replaces the `get-session-report` and `get-error-report` toolbox capabilities.

```
CLI shell  →  Query record  →  ISurfaceAgent  →  Result<T>
                                 └─ FakeAgent (reads the DEV DB directly — disposable, see below)
```

### Projects

| Project | Role | Durability |
|---------|------|-----------|
| `JOSYN.Surface.Contracts` | Query records, response DTOs, the `ISurfaceAgent` seam, the error taxonomy | **Durable** — the contract phase-2 REST inherits |
| `JOSYN.Surface.FakeAgent` | In-process `ISurfaceAgent` that reads the DEV DB directly and maps rows to DTOs | **Throwaway** — abandoned when the real agent + `HttpAgent` arrive |
| `JOSYN.Surface.Cli` | Power-ops CLI shell | Mostly durable |
| `JOSYN.Surface.Test` | Contract and mapping tests | — |

### The seam is the agent, not the transport (ADR-031 DS-2)

`ISurfaceAgent` is the load-bearing abstraction. Phase 1 satisfies it with `FakeAgent`; a later
phase swaps in an `HttpAgent` talking to the real platform-resident agent over REST — **with no
change to any query, DTO, handler, or shell** above the seam, because the seam is designed for the
network boundary from day 1 (async, wire-safe identity-bearing records, a named error taxonomy,
bounded reads).

### The FakeAgent is a deliberate, scoped, temporary exception (ADR-031 DS-4)

`FakeAgent` reads `josyn-db-local` directly. This is an explicit, **DEV-only, read-only** exception
to ADR-030 D-8 (API-mediated) and D-17 (store access is platform-resident). It is disposable
scaffolding: it is removed wholesale when the real agent lands. **No DB table/row shape may cross
`ISurfaceAgent`** — `FakeAgent` maps its raw reads to the durable DTOs internally.

---

## Running MVP-1

Prerequisite: a bootstrapped local dev DB (`josyn-backend/db/db-bootstrap/bootstrap-local-dev.sql`).

```
dotnet run --project JOSYN.Surface.Cli -- sessions --max 20
dotnet run --project JOSYN.Surface.Cli -- error <error-guid>
```

## Building

Local developer tooling lives in `.local-build/` (see `josyn-platform/architecture/local-build.md`).

```
.local-build\build.cmd        # build (Release by default)
.local-build\test.cmd         # run tests
.local-build\all.cmd          # clean caches, build, test
```

---

## Out of scope for MVP-1

REST / `HttpAgent`, the platform-resident agent EXE, any command/mutation (incl. `RetriggerSession`,
which is MVP-2 and gated on a real platform-side agent), the central aggregator, the machine
registry, the Blazor web shell, and auth/RBAC enforcement.
