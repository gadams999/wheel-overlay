<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0
Version bump type: MINOR — new "Procedural References" section added; no principles changed.

Modified principles:
  - [PRINCIPLE_1_NAME] → I. Monorepo with Shared Core (ProjectReference)
  - [PRINCIPLE_2_NAME] → II. Test-First with Property-Based Testing
  - [PRINCIPLE_3_NAME] → III. Independent Per-App Versioning
  - [PRINCIPLE_4_NAME] → IV. Changelog as Release Source of Truth
  - [PRINCIPLE_5_NAME] → V. Observability and Error Resilience
  - [PRINCIPLE_6_NAME] (added) → VI. Branch Naming and Conventional Commits

Added sections:
  - VI. Branch Naming and Conventional Commits (6th principle)
  - Technology Stack (replaces [SECTION_2_NAME])
  - Development Workflow (replaces [SECTION_3_NAME])
  - Procedural References (new in v1.1.0 — pointers to distilled .specify/memory/ procedure files)

Removed sections:
  - None (all placeholders replaced)

Templates status:
  ✅ .specify/templates/plan-template.md — "Constitution Check" section is generic
     and correctly defers to this file per-feature; no structural update required.
  ✅ .specify/templates/spec-template.md — no constitution references; aligned.
  ✅ .specify/templates/tasks-template.md — phase/category model aligns with
     principle II (test tasks) and principle III (versioning tasks). No update required.
  ✅ .specify/templates/agent-file-template.md — generic placeholders; no conflicts.
  ✅ .kiro/steering/* — branch-naming, changelog, version-management, and
     documentation-update steering docs distilled into .specify/memory/ procedure
     files (v1.1.0). Steering docs remain as the original source; procedure memory
     files are the speckit-native form.

Deferred TODOs:
  - None. All fields resolved from .kiro steering docs, spec, design, and repo context.
-->

# OpenDash-Overlays Constitution

## Core Principles

### I. Monorepo with Shared Core (ProjectReference)

All overlay applications MUST reside in a single Git repository
(`OpenDash-Overlays`) under `src/{AppName}/`. Shared overlay infrastructure
(theme detection, logging, process monitoring, window transparency, config-mode
drag, system tray scaffolding, settings framework, font resources, global
hotkey) MUST live in `src/OverlayCore/` and be consumed by overlay apps via
MSBuild `ProjectReference` — never as a NuGet package.

Non-negotiable rules:
- OverlayCore MUST NOT carry an independent `<Version>` property; it is
  versioned implicitly through the consuming application.
- New overlay apps MUST be added under `src/`, `tests/`, `installers/`, and
  `scripts/{app-name}/` following the established monorepo layout.
- OverlayCore changes are atomic with the consuming app change — a single
  commit or PR may span both.
- The root namespace for the shared library is `OpenDash.OverlayCore`; each
  overlay app uses `OpenDash.{AppName}`.

**Rationale**: A ProjectReference-based monorepo enables atomic cross-project
refactoring and eliminates NuGet publish ceremony for every internal change,
while keeping overlay apps independently releasable.

### II. Test-First with Property-Based Testing

All correctness properties defined in a feature's design document MUST be
implemented as property-based tests (FsCheck) before the implementation is
considered complete.

Non-negotiable rules:
- Each design-document correctness property MUST map to exactly one
  property-based test in the appropriate test project.
- Property tests MUST include a comment in the form:
  `// Feature: {feature-name}, Property {N}: {property title}`
- Property tests MUST use `#if FAST_TESTS` / `#else` directives to control
  iteration counts: 10 iterations in `FastTests` configuration, 100 in
  `Release` configuration.
- `dotnet test` MUST be green on `main` at all times. A failing test MUST
  block merge.
- Unit tests cover specific examples, edge cases, and integration points;
  property tests validate universal correctness across random inputs. Both
  are required where the design doc specifies properties.

**Rationale**: Property-based testing catches edge cases that example-based
tests miss, particularly in serialization round-trips, state machines, and
ordering invariants that are central to overlay correctness.

### III. Independent Per-App Versioning

Each overlay application maintains its own semantic version independently.
Releasing one overlay MUST NOT require version changes in any other overlay.

Non-negotiable rules:
- Each overlay app MUST declare `<Version>`, `<AssemblyVersion>`, and
  `<FileVersion>` in its own `.csproj` file.
- Version bumps follow SemVer: MAJOR for breaking changes, MINOR for new
  backward-compatible features, PATCH for backward-compatible bug fixes.
- The version MUST be bumped as the **first commit** on a new feature or fix
  branch so that local builds immediately reflect the target version.
- Git release tags MUST use the namespaced format:
  `{app-name}/v{major}.{minor}.{patch}` (e.g., `wheel-overlay/v0.7.0`).
- Tag-triggered CI/CD MUST validate that the tag version matches the
  `.csproj` Version property and MUST fail the workflow if they diverge.

**Rationale**: Namespaced tags and per-app `.csproj` versions allow
independent release cadences and make the GitHub Actions release workflow
unambiguously scope-aware.

### IV. Changelog as Release Source of Truth

`CHANGELOG.md` is the authoritative record of all user-facing changes.
It MUST be updated as part of every feature or fix branch before merging
to `main`.

Non-negotiable rules:
- CHANGELOG.md MUST follow the
  [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format with
  an `[Unreleased]` section at the top.
- Every user-facing change (Added, Changed, Deprecated, Removed, Fixed,
  Security) MUST appear as a changelog entry. Internal refactors and test
  additions are excluded unless they affect observable behavior.
- Changelog entries MUST be user-focused — no class names, method names,
  or file paths. Start each entry with an action verb.
- Before release, the `[Unreleased]` heading is replaced with the version
  number and date: `## [X.Y.Z] - YYYY-MM-DD`.
- GitHub Release notes MUST be sourced from the corresponding CHANGELOG
  version section.
- README.md "Version History" MUST be kept in sync with CHANGELOG.md
  (CHANGELOG is detailed; README is the summary).

**Rationale**: A single source of truth for release notes prevents drift
between README, GitHub Releases, and PR descriptions, and ensures users
always have accurate change documentation.

### V. Observability and Error Resilience

Every overlay application MUST initialize `LogService` at startup and MUST
handle all recoverable failure modes without crashing.

Non-negotiable rules:
- `LogService.Initialize("{AppName}")` MUST be called before any other
  service initialization. Logs are written to
  `%APPDATA%\{AppName}\logs.txt` with a 1 MB truncation limit.
- All service initialization failures (WMI unavailable, hotkey conflict,
  theme resource not found, settings deserialization error) MUST be caught,
  logged, and handled gracefully — the application MUST continue operating
  in a degraded-but-functional state.
- No failure path may silently swallow errors without a `LogService.Error()`
  call that includes enough context to diagnose the issue.
- Build scripts MUST use `$ErrorActionPreference = "Stop"` so path errors
  fail loudly rather than silently.

**Rationale**: Overlay applications run in the background during sim sessions.
Silent crashes or hangs are unacceptable; graceful degradation keeps the
session running even if a non-critical service fails.

### VI. Branch Naming and Conventional Commits

All branches MUST use the format `<type>/<description>` with lowercase,
hyphen-separated descriptions.

Non-negotiable rules:
- Valid type prefixes: `feat/`, `fix/`, `docs/`, `test/`, `refactor/`,
  `chore/`, `perf/`. No other prefixes are permitted.
- Descriptions MUST be lowercase with words separated by hyphens (no
  underscores, no camelCase).
- Version release branches MUST use `feat/v{major}.{minor}.{patch}-release`.
- Hotfix branches targeting a specific version MUST use
  `fix/v{version}-{description}`.
- Commit messages SHOULD follow Conventional Commits format:
  `<type>(<scope>): <description>`.
- Branches MUST be created from `main`, merged back to `main` via PR, and
  deleted after merge.

**Rationale**: Consistent branch naming enables CI/CD path routing, makes
branch purpose immediately legible, and supports automated tooling that
relies on type prefixes.

## Technology Stack

**Runtime**: .NET 10.0-windows, WPF (UI), WinForms (system tray / NotifyIcon)
**Language**: C# with `<Nullable>enable</Nullable>` and
`<ImplicitUsings>enable</ImplicitUsings>`
**Input / Hardware**: Vortice.DirectInput (WheelOverlay only)
**Process monitoring**: System.Management (WMI) in OverlayCore
**Testing**: xUnit, FsCheck 2.16.6 with FsCheck.Xunit
**Installer**: WiX 4 (per-app MSI under `installers/{app-name}/`)
**CI/CD**: GitHub Actions with path-filter and tag-trigger workflows
**Build configurations**: `Debug`, `FastTests` (FAST_TESTS constant),
`Release`
**Scripting**: PowerShell 7+ scripts under `scripts/` and
`scripts/{app-name}/`

All new overlay applications MUST target the same framework version as the
monorepo baseline. Framework version changes require a constitution amendment.

## Development Workflow

**Branch lifecycle**:
1. Create branch from `main` using the naming convention in Principle VI.
2. Bump the app version in all required files as the first commit
   (Principle III).
3. Implement changes with tests (Principle II).
4. Update CHANGELOG.md before the final commit (Principle IV).
5. Update README.md if user-facing behavior changes.
6. Open PR to `main`; CI runs `dotnet build` + `dotnet test --configuration
   FastTests`; property test directives are validated by the shared
   PowerShell script.
7. Merge after review; CI runs `dotnet test --configuration Release`
   (100 PBT iterations) on `main`.
8. Delete the branch after merge.

**Releasing**:
- Push a namespaced tag (`wheel-overlay/vX.Y.Z`) to trigger the per-app
  release workflow. The workflow validates tag-vs-.csproj version parity,
  builds the MSI, and creates a GitHub Release from CHANGELOG content.

**Adding a new overlay app**:
- Create `src/{AppName}/`, `tests/{AppName}.Tests/`,
  `installers/{app-name}/`, `scripts/{app-name}/`, `docs/{app-name}/`.
- Add a `ProjectReference` to OverlayCore in the app's `.csproj`.
- Register the app in `OpenDash-Overlays.sln` under the `src` solution
  folder.
- Create a dedicated `{app-name}-release.yml` CI/CD workflow with path
  filters scoped to the app's directories plus `src/OverlayCore/**` and
  `tests/OverlayCore.Tests/**`.

## Procedural References

The files below live in `.specify/memory/` and contain step-by-step guidance
distilled from the project's original `.kiro/steering/` documents. They are
**not** loaded by default — read the relevant file when a task requires the
procedural detail.

| When you need to… | Read |
|-------------------|------|
| Bump the app version (which files, what order, how to verify) | `procedures-versioning.md` |
| Update or finalize CHANGELOG.md (format, entry rules, release promotion) | `procedures-changelog.md` |
| Update README.md or other documentation before pushing | `procedures-documentation.md` |
| Name a branch, write a commit message, or scope a tag | `procedures-branching.md` |

These files are descriptive (how-to); this constitution is prescriptive
(must/must not). When they conflict, the constitution takes precedence.

## Governance

This constitution supersedes all other development practices and informal
conventions documented in `.kiro/steering/` files. The steering docs remain
authoritative for step-by-step procedural guidance; this constitution defines
the non-negotiable rules.

**Amendment procedure**:
1. Propose the amendment in a PR with a description of the change and
   rationale.
2. All affected templates (`.specify/templates/`) MUST be reviewed for
   consistency and updated in the same PR.
3. Increment `CONSTITUTION_VERSION` per semantic versioning:
   MAJOR for backward-incompatible governance changes or principle removal,
   MINOR for new principles or materially expanded guidance,
   PATCH for wording clarifications and non-semantic refinements.
4. Update `LAST_AMENDED_DATE` to the merge date.

**Compliance review**: Every PR description MUST include a "Constitution
Check" affirming that the branch naming (VI), version bump (III), changelog
update (IV), and test coverage (II) requirements are satisfied. The plan
template's "Constitution Check" gate enforces this at spec time.

**Versioning policy**: Principle changes that alter what is MUST/MUST NOT
are MAJOR. New principles or new SHOULD guidance are MINOR. Typos, examples,
and rationale additions are PATCH.

**Version**: 1.1.0 | **Ratified**: 2026-03-18 | **Last Amended**: 2026-03-18