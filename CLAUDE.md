# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet run                      # run with the default (http) profile → http://localhost:5298
dotnet run --launch-profile https   # https://localhost:7157
dotnet build
dotnet watch                    # hot reload during development
```

There is no test project yet. When one is added, `dotnet test` runs the suite and
`dotnet test --filter FullyQualifiedName~MyTest` runs a single test.

## Architecture

ASP.NET Core **Razor Pages** app targeting **.NET 10**, single project (`EKProno.csproj`),
with `Nullable` and `ImplicitUsings` enabled. The code is still the default scaffold:

- [Program.cs](Program.cs) — minimal-hosting startup; the only registered service is
  `AddRazorPages()`. Any DI registration, data access, or auth goes here.
- [Pages/](Pages/) — each page is a `.cshtml` + `.cshtml.cs` PageModel pair in namespace
  `EKProno.Pages`. `_ViewImports.cshtml` sets that namespace and the tag helpers;
  `Pages/Shared/_Layout.cshtml` is the shared layout.
- [wwwroot/](wwwroot/) — static assets, served via `MapStaticAssets()`/`WithStaticAssets()`
  (.NET 9+ static asset pipeline, not the older `UseStaticFiles`). Bootstrap, jQuery, and
  jQuery-validation are vendored under `wwwroot/lib/` — no npm toolchain.

Because the application code is essentially empty, treat the documentation workflow below
as the source of truth for intended behavior.

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
3. **Architecture** — [docs/architecture/](docs/architecture/) holds the 12 **arc42**
   sections (`00`–`12`). These are currently empty templates; fill the relevant section
   rather than inventing a new doc location. Specs should reference them.

When adding a feature, check for an existing spec and story-map entry first, and keep the
domain vocabulary consistent with what those documents already establish.
