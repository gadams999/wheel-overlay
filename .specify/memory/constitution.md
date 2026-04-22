<!--
SYNC IMPACT REPORT
==================
Version change: 2.1.2 → 2.1.3
Version bump type: PATCH — Replace fictional `discord-notify` branch-name examples
with `speakersight` examples throughout Principle VI and the branching procedure,
reflecting the project's actual overlay naming after the Discord Chat Overlay →
SpeakerSight rename (2026-04-19).

Modified principles:
  - Principle VI: Branch Naming — example `discord-notify/v1.0.0` →
    `speakersight/v1.0.0` in both valid-examples block and the invalid-examples
    table comment. No rule changes.

Added sections:
  - None.

Removed sections:
  - None.

Templates updated:
  ✅ .specify/memory/constitution.md — this file (v2.1.3).
  ✅ .specify/memory/procedures-branching.md — same example substitution.
  ⚠  .specify/templates/plan-template.md — no change needed (placeholders only).
  ⚠  .specify/templates/spec-template.md — no change needed (placeholders only).
  ⚠  .specify/templates/tasks-template.md — no change needed (placeholders only).

Deferred TODOs:
  - None. All fields resolved.
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

### VI. Branch Naming, Spec Folders, and Conventional Commits

Spec-driven work and version-targeted work MUST use version-based branch
names. Spec folders use a separate sequential naming scheme. The two are
explicitly linked in every plan document.

#### Spec folder naming (speckit convention — unchanged)

Spec folders MUST follow the pattern `NNN-spec-description` under `specs/`:

```
specs/001-opendash-monorepo-rebrand/
specs/002-material-design-settings/
```

- `NNN` is a zero-padded three-digit sequential number.
- `spec-description` is lowercase, hyphen-separated.
- The spec folder name is a permanent identifier — it does NOT change once
  the spec is created, even after the branch is merged.

#### Branch naming — PRIMARY format (spec-driven and version-targeted work)

All branches that implement a spec or target a specific app version MUST use
the format:

```
[overlay-name/]vN.N.N
```

- `overlay-name/` is the app-scoped prefix (e.g., `wheel-overlay/`). It is
  **required** when the work is scoped to a single overlay app.
- Omit `overlay-name/` only when the branch targets a cross-cutting monorepo
  change that is not app-specific (e.g., `v2.0.0` for an OverlayCore-only
  restructure).
- `N.N.N` is the SemVer version the branch will ship as (matching the
  `.csproj` `<Version>` bumped as the first commit on the branch).

Valid examples:
```
wheel-overlay/v0.8.0     ← WheelOverlay feature targeting v0.8.0
wheel-overlay/v0.7.1     ← WheelOverlay patch targeting v0.7.1
speakersight/v1.0.0      ← SpeakerSight initial release
v2.0.0                   ← monorepo-wide (OverlayCore breaking change)
```

Invalid examples:
```
feat/animated-transitions   ✗ — use version-based name for spec work
001-opendash-monorepo-rebrand ✗ — spec folder name is not a branch name
wheel-overlay-v0.8.0        ✗ — missing slash separator
```

#### Branch naming — SECONDARY format (ad-hoc, non-versioned work)

Small non-spec changes that do not target a new version (documentation
corrections, dependency bumps, CI tweaks) MAY use `<type>/<description>`:

- Valid type prefixes: `fix/`, `docs/`, `chore/`, `test/`, `refactor/`,
  `perf/`. The `feat/` prefix MUST NOT be used for ad-hoc branches — all
  feature work MUST go through a spec and use the PRIMARY format.
- Descriptions MUST be lowercase, hyphen-separated (no underscores,
  no camelCase).

#### Linking spec folders to branches

Every spec's `plan.md` MUST reference the branch in its header:

```markdown
**Branch**: `wheel-overlay/v0.8.0` | **Spec**: `specs/002-material-design-settings/`
```

This link is the authoritative connection between the spec folder (permanent
sequential name) and the branch (version-based name). Both MUST be present in
`plan.md` so that a reader can navigate from either direction.

#### Commit messages

Commits SHOULD follow Conventional Commits format:
`<type>(<scope>): <description>`

- `type`: same set as secondary branch prefixes above, plus `feat` for
  implementation commits on a version branch.
- `scope`: optional; identifies the affected component.
- `description`: lowercase, imperative mood, no trailing period.

**Rationale**: Version-based branch names make the target release immediately
legible from the branch list and eliminate the disconnect between a spec's
sequential folder name and the git workflow. The spec folder provides
permanent traceability; the branch name provides release context.

### VII. Documentation and Public Site

The project's public presence MUST be maintained at `opendashoverlays.com`
(primary domain) and `docs.opendashoverlays.com` (user-facing documentation).
Documentation MUST be generated with MkDocs using the Material Design theme
and published via GitHub Pages.

Non-negotiable rules:
- User documentation source files MUST live under `docs/{app-name}/` in the
  monorepo (Markdown, conforming to MkDocs conventions).
- The MkDocs configuration file (`mkdocs.yml`) MUST reside at the repository
  root and MUST specify `theme: material` (MkDocs Material Design theme).
- Generated static content MUST NOT be committed to `main`. GitHub Pages MUST
  be deployed automatically by CI using either a dedicated `gh-pages` branch or
  the GitHub Actions deployment source (`actions/deploy-pages`); both satisfy
  this rule. Manual publishing to either destination is prohibited.
- A GitHub Actions workflow MUST build (`mkdocs build --strict`) and deploy
  (via `actions/deploy-pages` or equivalent GitHub Pages deployment action)
  documentation on every push to `main` that touches `docs/**` or `mkdocs.yml`.
- The `--strict` flag MUST be used in CI so that broken links, missing
  references, and malformed Markdown fail the build rather than silently
  producing a broken site.
- Documentation pages MUST be user-focused — no internal class names, file
  paths, or namespace references except in the Developer Guide section.
- Every new user-facing feature MUST have corresponding documentation added or
  updated in `docs/{app-name}/` before the feature branch merges to `main`.

**Rationale**: A publicly hosted documentation site at a stable domain
(`docs.opendashoverlays.com`) is the primary support surface for end users.
Generating it from source via MkDocs + Material keeps it version-controlled,
reviewable in PRs, and automatically deployed — eliminating manual publish
steps that cause drift between code and docs.

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
**Documentation**: MkDocs with Material Design theme; hosted on GitHub Pages
at `docs.opendashoverlays.com`; source under `docs/{app-name}/`

All new overlay applications MUST target the same framework version as the
monorepo baseline. Framework version changes require a constitution amendment.

## Development Workflow

**Branch lifecycle**:
1. Create branch from `main` using the PRIMARY naming convention in Principle
   VI: `[overlay-name/]vN.N.N`.
2. Bump the app version in all required files as the first commit
   (Principle III). The version MUST match the branch name.
3. Implement changes with tests (Principle II).
4. Update CHANGELOG.md before the final commit (Principle IV).
5. Update README.md if user-facing behavior changes.
6. Add or update documentation under `docs/{app-name}/` for any new or
   changed user-facing features (Principle VII).
7. Open PR to `main`; CI runs `dotnet build` + `dotnet test --configuration
   FastTests`; property test directives are validated by the shared
   PowerShell script.
8. Merge after review; CI runs `dotnet test --configuration Release`
   (100 PBT iterations) on `main`; documentation CI builds and deploys to
   GitHub Pages.
9. Delete the branch after merge.

**Releasing**:
- Push a namespaced tag (`wheel-overlay/vX.Y.Z`) to trigger the per-app
  release workflow. The workflow validates tag-vs-.csproj version parity,
  builds the MSI, and creates a GitHub Release from CHANGELOG content.
- The tag format matches the branch format — `overlay-name/vN.N.N`.

**Adding a new overlay app**:
- Create `src/{AppName}/`, `tests/{AppName}.Tests/`,
  `installers/{app-name}/`, `scripts/{app-name}/`, `docs/{app-name}/`.
- Add a `ProjectReference` to OverlayCore in the app's `.csproj`.
- Register the app in `OpenDash-Overlays.sln` under the `src` solution
  folder.
- Create a dedicated `{app-name}-release.yml` CI/CD workflow with path
  filters scoped to the app's directories plus `src/OverlayCore/**` and
  `tests/OverlayCore.Tests/**`.
- Add the app's documentation section to `mkdocs.yml` nav.

## Procedural References

The files below live in `.specify/memory/` and contain step-by-step guidance
distilled from the project's original `.kiro/steering/` documents. They are
**not** loaded by default — read the relevant file when a task requires the
procedural detail.

| When you need to… | Read |
|-------------------|------|
| Bump the app version (which files, what order, how to verify) | `procedures-versioning.md` |
| Update or finalize CHANGELOG.md (format, entry rules, release promotion) | `procedures-changelog.md` |
| Update README.md, MkDocs docs, or other documentation before pushing | `procedures-documentation.md` |
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
update (IV), test coverage (II), and documentation update (VII) requirements
are satisfied. The plan template's "Constitution Check" gate enforces this at
spec time.

**Versioning policy**: Principle changes that alter what is MUST/MUST NOT
are MAJOR. New principles or new SHOULD guidance are MINOR. Typos, examples,
and rationale additions are PATCH.

**Version**: 2.1.3 | **Ratified**: 2026-03-18 | **Last Amended**: 2026-04-19
