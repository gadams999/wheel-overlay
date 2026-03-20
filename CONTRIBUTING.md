# Contributing to OpenDash-Overlays

## Branch Naming

All branches must follow `<type>/<description>` format using kebab-case:

| Prefix | Use for |
|--------|---------|
| `feat/` | New features |
| `fix/` | Bug fixes |
| `docs/` | Documentation only |
| `test/` | Adding or updating tests |
| `refactor/` | Code restructuring without behaviour change |
| `chore/` | Build, tooling, dependency updates |
| `perf/` | Performance improvements |

**Examples**:
```
feat/add-lap-counter-overlay
fix/hotkey-registration-failure
docs/update-getting-started
chore/bump-vortice-dependency
refactor/extract-theme-service
```

## Versioning

Each overlay app declares its version independently in its `.csproj`. OverlayCore must **never** have a `<Version>` element.

**Version bump rules**:
- The version bump commit must be the **first implementation commit** on your branch
- Set all three version properties together:
  ```xml
  <Version>X.Y.Z</Version>
  <AssemblyVersion>X.Y.Z.0</AssemblyVersion>
  <FileVersion>X.Y.Z.0</FileVersion>
  ```
- Follow [SemVer](https://semver.org/): increment PATCH for fixes, MINOR for new features, MAJOR for breaking changes
- Commit message: `chore(<app-name>): bump version to X.Y.Z`

## Release Tags

Releases are triggered by pushing a namespaced tag:

```
Pattern: {app-name}/vX.Y.Z

Examples:
  wheel-overlay/v0.7.0
  discord-notify/v1.0.0

Invalid:
  v0.7.0              (missing app-name prefix)
  wheel-overlay/0.7.0 (missing v)
  wheel_overlay/v0.7.0 (underscore not hyphen)
```

The tag version **must** match the `<Version>` in the app's `.csproj`. The CI pipeline validates this and fails with a clear error if they differ.

## Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <description>

Examples:
  feat(wheel-overlay): add Alt+F6 global hotkey for overlay repositioning
  fix(overlay-core): handle RegisterHotKey failure gracefully
  chore(wheel-overlay): bump version to 0.7.0
  docs(wheel-overlay): add getting-started guide
```

## Pull Request Checklist (Constitution Check)

Every PR description must include a Constitution Check confirming all principles are satisfied:

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Monorepo with Shared Core (ProjectReference) | ✅/❌ | OverlayCore has no `<Version>`; apps reference it via `ProjectReference` |
| II. Test-First with Property-Based Testing | ✅/❌ | Property tests written; `#if FAST_TESTS` / `#else` directives present |
| III. Independent Per-App Versioning | ✅/❌ | Version bump is first commit on branch |
| IV. Changelog as Release Source of Truth | ✅/❌ | `CHANGELOG.md` `[Unreleased]` section updated |
| V. Observability and Error Resilience | ✅/❌ | `LogService.Initialize` first; all failures log via `LogService.Error` |
| VI. Branch Naming and Conventional Commits | ✅/❌ | Branch uses `<type>/<description>`; commits follow Conventional Commits |

Before opening a PR, also verify:
- [ ] `dotnet build` succeeds from repository root
- [ ] `dotnet test --configuration FastTests` passes
- [ ] `.\scripts\Validate-PropertyTests.ps1 -TestProjectPath tests/<AppName>.Tests` passes
- [ ] `CHANGELOG.md` updated under `[Unreleased]`

## Adding a New Overlay App

See [specs/001-opendash-monorepo-rebrand/quickstart.md](specs/001-opendash-monorepo-rebrand/quickstart.md) for the complete 10-step guide covering project creation, test setup, build scripts, WiX installer, CI/CD workflow, and documentation.
