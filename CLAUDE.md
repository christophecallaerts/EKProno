# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet run                      # run with the default (http) profile → http://localhost:5298
dotnet run --launch-profile https   # https://localhost:7157
dotnet build
dotnet watch                    # hot reload during development
```

`dotnet test` runs the suite; `dotnet test --filter FullyQualifiedName~MyTest` runs a single
test. Only unit and integration tests — no performance tests.

## Architecture

ASP.NET Core **Razor Pages** app targeting **.NET 10**, with `Nullable` and `ImplicitUsings`
enabled. Two projects, tied together by `EKProno.slnx`: the web app (`EKProno.csproj`) and
[tests/EKProno.Tests/](tests/EKProno.Tests/).

- [Program.cs](Program.cs) — minimal-hosting startup, and the only place persistence, auth
  and services are registered.
- [Domain/](Domain/) — the entities from [docs/domain-model.md](docs/domain-model.md) as
  plain serialisable classes. No behaviour beyond their own invariants.
- [Services/](Services/) — the spec rules. `PoolService` owns everything spec 001 asks of a
  pool and returns `Result<T>` rather than throwing, so pages render field-level messages.
  `UserAccountService` is a deliberate authentication stand-in (see ADR-002).
- [Storage/](Storage/) — `IDataStore` is the persistence seam; `JsonFileDataStore` is the
  one implementation, a single JSON document mirrored to `App_Data/ekprono.json` (gitignored).
  `Mutate` commits a private copy only when the caller accepts the result, which is what
  makes multi-entity writes atomic. Construct it with `filePath: null` for an in-memory store.
  See [ADR-001](docs/architecture/09-architecture-decisions.md).
- [Pages/](Pages/) — each page is a `.cshtml` + `.cshtml.cs` PageModel pair in namespace
  `EKProno.Pages`. `_ViewImports.cshtml` sets that namespace and the tag helpers;
  `Pages/Shared/_Layout.cshtml` is the shared layout. `/Pools` is authorised as a folder.
  Bound string properties are declared nullable on purpose, to keep MVC's implicit
  `[Required]` from pre-empting the service's own validation messages.
- [wwwroot/](wwwroot/) — static assets, served via `MapStaticAssets()`/`WithStaticAssets()`
  (.NET 9+ static asset pipeline, not the older `UseStaticFiles`). Bootstrap, jQuery, and
  jQuery-validation are vendored under `wwwroot/lib/` — no npm toolchain.

Keep validation and domain rules in `Services/`, not in PageModels: the unit tests exercise
them directly, and the integration tests go over HTTP through `EkPronoWebApplicationFactory`.

## Issues

Whenever the user mentions an issue (creating, reading, commenting, closing, listing),
use the **GitHub MCP server** tools (`mcp__github__*`) — not `gh` CLI, not web fetches.

The repository is always `christophecallaerts/EKProno`, i.e. `owner: christophecallaerts`,
`repo: EKProno`. Do not infer the repo from a different remote or from the folder name.

## Documentation workflow

The project drives development from docs, in this order:

1. **Story map** — `docs/product/story-map.md`, produced by the `story-mapping` agent/skill.
   Stories are numbered; those numbers are the spec file prefixes.
2. **Feature specs** — `docs/specs/NNN-<slug>.md`, produced by the `spec` skill
   (`.claude/skills/spec/`), using `spec-template.md`. `NNN` must match the story number
   from the story map.
3. **Domain model** — `docs/domain-model.md` is the single location for the domain model.
   Whenever a new spec introduces or changes entities, relationships, or domain vocabulary,
   update that file (never a per-spec or alternative domain-model doc). It must **always**
   contain a Mermaid diagram (`classDiagram` or `erDiagram`) of the model, kept in sync
   with the prose.
4. **Architecture** — [docs/architecture/](docs/architecture/) holds the 12 **arc42**
   sections (`00`–`12`). These are currently empty templates; fill the relevant section
   rather than inventing a new doc location. Specs should reference them.

When adding a feature, check for an existing spec and story-map entry first, and keep the
domain vocabulary consistent with what those documents already establish.
