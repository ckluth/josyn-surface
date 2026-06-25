# josyn-surface

> The human window into a headless platform — see
> [ADR-030](../josyn-platform/decisions/ADR-030-josyn-surface.md) (vision) and
> [ADR-031](../josyn-platform/decisions/ADR-031-surface-delivery-strategy.md) (delivery strategy).

`josyn-surface` is the coherent, human-facing window onto the otherwise headless JOSYN platform.
It replaces the *capabilities* of the scattered PoC toolbox scripts (not their code) with a unified,
environment-aware surface.

This repo holds the **external** parts of the surface: the shells and the shared backbone. The
platform-resident per-machine command host (`JOSYN.Backend.Gateway`, ADR-033) lives in
`josyn-backend`, not here.

---

## Current state — MVP-2b (read + one write verb)

MVP-2b is the second increment defined by ADR-031: **local CLI + read verbs + `change-argument`**,
DEV only. All six CLI verbs are wired through the `ISurfaceAgent` seam.

```
CLI shell  →  Query / Command record  →  ISurfaceAgent  →  Result<T>
                                           └─ CompositeSurfaceAgent
                                                ├─ FakeSurfaceAgent  (reads DEV DB — disposable)
                                                └─ GatewayCommandHandler  (writes via JOSYN.Backend.Gateway)
```

Wire contracts (queries, commands, DTOs, `SessionStatus` enum) live in the **`josyn-jrp`** sibling
repo (`JOSYN.Jrp.Launch` + `JOSYN.Jrp.Surface`) — not in this repo. See ADR-033.

### Projects

| Project | Role | Durability |
|---------|------|-----------|
| `JOSYN.Surface.Contracts` | `ISurfaceAgent` client seam — the only durable type defined here | **Durable** |
| `JOSYN.Surface.FakeAgent` | `FakeSurfaceAgent` (reads DEV DB) + `CompositeSurfaceAgent` (routes reads and writes) | **Throwaway** — replaced by `HttpAgent` |
| `JOSYN.Surface.Cli` | Power-ops CLI shell — six verbs | Mostly durable |
| `JOSYN.Surface.Test` | Contract and mapping tests (20 tests) | — |

### Wire contracts live in josyn-jrp (not here)

All query records, command records, response DTOs, `SessionStatus`, and `JrpError` live in:

| Package | Contents |
|---------|----------|
| `JOSYN.Jrp.Launch` | `JrpTarget`, `JrpErrorCategory`, `StartSessionRequest/Response` |
| `JOSYN.Jrp.Surface` | Read queries, `ChangeJobArgument` command, all DTOs, `SessionStatus` wire enum |

### The seam is the agent, not the transport (ADR-031 DS-2)

`ISurfaceAgent` is the load-bearing abstraction. MVP-2b satisfies it with `CompositeSurfaceAgent`
(throwaway scaffolding); a later phase swaps in an `HttpAgent` talking to the real platform-resident
Gateway over the network — **with no change to any query, DTO, handler, or shell** above the seam.

### The FakeAgent is a deliberate, scoped, temporary exception (ADR-031 DS-4)

`FakeSurfaceAgent` reads `josyn-db-local` directly. This is an explicit, **DEV-only, read-only**
exception to ADR-030 D-8 (API-mediated) and D-17 (store access is platform-resident). It is
disposable scaffolding: removed wholesale when `HttpAgent` lands. **No DB table/row shape crosses
`ISurfaceAgent`** — `FakeAgent` maps its raw reads to the durable JRP DTOs internally.

---

## Running MVP-2b

Prerequisite: a bootstrapped local dev DB (`josyn-backend/db/db-bootstrap/bootstrap-local-dev.sql`).

```
dotnet run --project JOSYN.Surface.Cli -- sessions --max 20
dotnet run --project JOSYN.Surface.Cli -- error <error-guid>
dotnet run --project JOSYN.Surface.Cli -- jobs
dotnet run --project JOSYN.Surface.Cli -- arguments <job-name>
dotnet run --project JOSYN.Surface.Cli -- schedule <job-name>
dotnet run --project JOSYN.Surface.Cli -- change-argument <job-name> <key> <value>
```

## Building

Local developer tooling lives in `.local-build/` (see `josyn-platform/architecture/local-build.md`).

```
.local-build\build.cmd        # build (Release by default)
.local-build\test.cmd         # run tests
.local-build\all.cmd          # clean caches, build, test
```

---

## Out of scope for MVP-2b

REST / `HttpAgent`, the Gateway EXE/service host, `JOSYN.Surface.SessionClient`, `CreateJobArgument`,
MVP-3 schedule writes, the central aggregator, the machine registry, the Blazor web shell, and
auth/RBAC enforcement.
