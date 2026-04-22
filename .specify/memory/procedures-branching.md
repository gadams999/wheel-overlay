---
name: Branch naming and commit convention procedure
description: Load when creating a branch, writing commit messages, or generating task descriptions that reference branch names — covers version-based primary format, spec-folder/branch linkage, and commit message structure
type: reference
---

# Branch Naming and Commit Conventions

## Branch formats

### PRIMARY format — spec-driven and version-targeted work

All branches that implement a spec or target a specific app version MUST use:

```
[overlay-name/]vN.N.N
```

- `overlay-name/` is **required** when the work is scoped to a single overlay app.
- Omit `overlay-name/` only for cross-cutting monorepo changes not app-specific.
- `N.N.N` is the SemVer version the branch ships as (MUST match `.csproj` after
  the first-commit version bump).

```
wheel-overlay/v0.8.0     ← WheelOverlay feature branch targeting v0.8.0
wheel-overlay/v0.7.1     ← WheelOverlay patch branch
speakersight/v1.0.0      ← SpeakerSight initial release branch
v2.0.0                   ← monorepo-wide (OverlayCore-only restructure)
```

### SECONDARY format — ad-hoc, non-versioned work

Small changes that don't target a new version (doc corrections, CI tweaks,
dependency bumps) MAY use:

```
<type>/<description>
```

- Description: lowercase, words separated by hyphens
- No underscores, no camelCase, no uppercase
- `feat/` MUST NOT be used here — all feature work goes through a spec and
  uses the PRIMARY format.

| Type | Use for |
|------|---------|
| `fix/` | Bug fixes that don't warrant a new spec |
| `docs/` | Documentation-only changes |
| `test/` | Adding or updating tests without new features |
| `refactor/` | Code restructuring without behavior change |
| `chore/` | Maintenance, dependency updates, build changes |
| `perf/` | Performance improvements |

```
fix/typo-in-readme
chore/update-wix-deps
docs/add-troubleshooting-faq
```

## Spec folder naming (speckit convention)

Spec folders MUST use `NNN-spec-description` under `specs/`. This is a
permanent identifier — it does NOT become the branch name.

```
specs/001-opendash-monorepo-rebrand/
specs/002-material-design-settings/
```

## Linking spec folders to branches

Every spec's `plan.md` MUST reference both the branch name and the spec folder
in its header line:

```markdown
**Branch**: `wheel-overlay/v0.8.0` | **Spec**: `specs/002-material-design-settings/`
```

This makes navigation bidirectional: from the branch you know what version ships,
from the spec folder you know what branch to check out.

## Quick valid/invalid reference

| Branch name | Valid? | Reason |
|-------------|--------|--------|
| `wheel-overlay/v0.8.0` | ✅ | Primary format, app-scoped |
| `speakersight/v1.0.0` | ✅ | Primary format, app-scoped |
| `v2.0.0` | ✅ | Primary format, monorepo-wide |
| `fix/exit-handling` | ✅ | Secondary format, ad-hoc fix |
| `docs/update-readme` | ✅ | Secondary format, docs-only |
| `001-opendash-monorepo-rebrand` | ❌ | Spec folder name ≠ branch name |
| `feat/animated-transitions` | ❌ | Use version-based name for all feature work |
| `wheel-overlay-v0.8.0` | ❌ | Missing slash separator |
| `WheelOverlay/v0.8.0` | ❌ | Not lowercase |
| `feature/thing` | ❌ | Use `feat` prefix; but even then, feature work needs version branch |

## Commit message format (Conventional Commits)

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

- `type`: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`
- `scope`: optional; identifies the affected component
- `description`: lowercase, imperative mood, no trailing period

Examples:
```
feat(wheel-overlay): add animated dial transitions
fix(overlay-core): handle missing theme resource gracefully
docs(readme): update monorepo structure diagram
chore: bump WiX to 4.0.6
refactor(overlay-core): extract ThemeService to shared library
```

## Branch lifecycle

1. Create from `main` using PRIMARY format: `[overlay-name/]vN.N.N`
2. First commit: version bump in `.csproj` — version MUST match branch name
   (see `procedures-versioning.md`)
3. Implement changes with property-based tests
4. Update CHANGELOG.md and README.md before final commit
5. Open PR to `main`; reference the spec folder in the PR description
6. Merge after CI passes and review approval
7. Delete branch after merge
8. Push release tag `overlay-name/vN.N.N` to trigger release workflow
