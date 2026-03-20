# Contributing to OpenDash Overlays

Thank you for your interest in contributing! This guide covers the monorepo conventions, branch naming, version rules, and testing requirements.

---

## Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 or VS Code
- Git
- Windows OS (DirectInput support required)

---

## Setting Up

```bash
git clone https://github.com/gadams999/opendash-overlays.git
cd opendash-overlays
dotnet restore
dotnet build OpenDash-Overlays.sln
```

---

## Branch Naming

| Work type | Pattern | Example |
|---|---|---|
| New overlay app or major feature | `{app-name}/v{version}` | `overlay-core/v0.1.0` |
| Bug fix | `fix/{short-description}` | `fix/hotkey-registration` |
| Documentation | `docs/{short-description}` | `docs/troubleshooting` |

---

## Version Bump Rules

- Each application (`WheelOverlay`, etc.) owns its own version in its `.csproj`
- Set all three properties together: `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
- `OverlayCore` **must never** have a `<Version>` element — it is referenced only via `ProjectReference`
- Version bump commit must be the **first commit** on the branch (before any feature work)

---

## Adding a New Overlay App

See [`specs/001-opendash-monorepo-rebrand/quickstart.md`](../specs/001-opendash-monorepo-rebrand/quickstart.md) for the complete 10-step guide.

---

## Development Workflow

### 1. Create a branch (see naming conventions above)

```bash
git checkout -b fix/your-fix-name
```

### 2. Build and test during development

```bash
# Fast feedback (10 PBT iterations)
dotnet test --configuration FastTests

# Before committing (100 PBT iterations)
dotnet test --configuration Release
```

### 3. Validate property test directives

```powershell
powershell -File scripts/Validate-PropertyTests.ps1 -TestProjectPath tests/WheelOverlay.Tests
powershell -File scripts/Validate-PropertyTests.ps1 -TestProjectPath tests/OverlayCore.Tests
```

### 4. Build from the solution root

```bash
dotnet build OpenDash-Overlays.sln
```

---

## Property-Based Test Conventions

Every `[Property]` test **must** include conditional compilation directives and a comment header:

```csharp
// Feature: {feature-name}, Property {N}: {title}
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
public Property YourTestName()
{
    return Prop.ForAll(
        Arb.From<YourType>(),
        value => /* assertion */);
}
```

- Fast (10 iterations): development feedback
- Release (100 iterations): pre-merge gate

---

## Pull Request Checklist

Before opening a PR:

- [ ] `dotnet build OpenDash-Overlays.sln` succeeds with 0 warnings
- [ ] `dotnet test --configuration Release` passes
- [ ] `scripts/Validate-PropertyTests.ps1` passes for all test projects
- [ ] `<Version>` is set in the application `.csproj` (and **not** in `OverlayCore.csproj`)
- [ ] `CHANGELOG.md` has an `[Unreleased]` entry
- [ ] Branch name follows the convention above

### PR Description

Include in your PR description:

```
## Summary
- What changed and why

## Constitution Check
- [ ] Branch name follows convention
- [ ] Version bumped correctly (app csproj only)
- [ ] CHANGELOG.md updated
- [ ] dotnet build succeeds from repo root
- [ ] dotnet test --configuration Release passes
- [ ] Validate-PropertyTests.ps1 passes
```

---

## CI/CD

- **PR builds**: FastTests (10 PBT iterations) for quick feedback
- **Merge to main**: Release configuration (100 PBT iterations)
- **Tag push** `{app-name}/v*`: builds MSI installer and creates GitHub Release

---

## Getting Help

- **Bugs / feature requests**: [GitHub Issues](https://github.com/gadams999/opendash-overlays/issues)
- **Questions**: [GitHub Discussions](https://github.com/gadams999/opendash-overlays/discussions)
