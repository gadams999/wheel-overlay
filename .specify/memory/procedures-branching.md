---
name: Branch naming and commit convention procedure
description: Load when creating a branch, writing commit messages, or generating task descriptions that reference branch names — covers type prefixes, format rules, and commit message structure
type: reference
---

# Branch Naming and Commit Conventions

## Branch format

```
<type>/<description>
```

- Description: lowercase, words separated by hyphens
- No underscores, no camelCase, no uppercase

## Valid type prefixes

| Type | Use for |
|------|---------|
| `feat/` | New features or enhancements |
| `fix/` | Bug fixes |
| `docs/` | Documentation-only changes |
| `test/` | Adding or updating tests |
| `refactor/` | Code restructuring without behavior change |
| `chore/` | Maintenance, dependency updates, build changes |
| `perf/` | Performance improvements |

## Special branch formats

**Version release branch**:
```
feat/v{major}.{minor}.{patch}-release
# e.g., feat/v0.7.0-release
```

**Hotfix branch**:
```
fix/v{version}-{description}
# e.g., fix/v0.5.3-exit-handling
```

**App-scoped branch** (for monorepo multi-app clarity):
```
feat/wheel-overlay/{description}
feat/overlay-core/{description}
# e.g., feat/wheel-overlay/animated-transitions
```

## Quick valid/invalid reference

| Branch name | Valid? | Reason |
|-------------|--------|--------|
| `feat/animated-transitions` | ✅ | |
| `fix/v0.5.1-timing-issues` | ✅ | |
| `refactor/extract-services` | ✅ | |
| `v0.5.2` | ❌ | No type prefix |
| `feat/MyNewFeature` | ❌ | Not lowercase |
| `fix/bug` | ❌ | Not descriptive |
| `feat/add_new_feature` | ❌ | Underscores |
| `feature/thing` | ❌ | Use `feat/` not `feature/` |

## Commit message format (Conventional Commits)

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Examples:
```
feat(overlay): add animated transitions
fix(tests): increase timing tolerance for CI
docs(readme): update installation instructions
chore: bump version to 0.7.0
refactor(overlay-core): extract ThemeService to shared library
```

- `type`: same set as branch type prefixes
- `scope`: optional, identifies the affected component
- `description`: lowercase, imperative mood, no period at end

## Branch lifecycle

1. Create from `main`
2. First commit: version bump (if feat/fix — see `procedures-versioning.md`)
3. Implement changes
4. Update CHANGELOG and README before final commit
5. Open PR to `main`
6. Merge after CI passes and review approval
7. Delete branch after merge