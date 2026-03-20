# Contract: CI/CD Workflow Definitions

**Location**: `.github/workflows/`
**Consumers**: GitHub Actions, developers pushing branches and tags

---

## Workflow: wheel-overlay-release.yml

**Replaces**: `release.yml`
**Triggers**:

```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/WheelOverlay/**'
      - 'src/OverlayCore/**'
      - 'tests/WheelOverlay.Tests/**'
      - 'tests/OverlayCore.Tests/**'
      - 'installers/wheel-overlay/**'
  push:
    tags:
      - 'wheel-overlay/v*'
```

**Jobs**:

1. `build-and-test`:
   - Validate property test directives: `.\scripts\Validate-PropertyTests.ps1 -TestProjectPath tests/WheelOverlay.Tests`
   - Validate OverlayCore tests: `.\scripts\Validate-PropertyTests.ps1 -TestProjectPath tests/OverlayCore.Tests`
   - Build: `dotnet build --configuration Release`
   - Test: `dotnet test --configuration Release` (100 PBT iterations on main/tags)

2. `package-and-release` (runs only when `build-and-test` passes):
   - Extract version from `.csproj`: parse `src/WheelOverlay/WheelOverlay.csproj`
   - If tag-triggered: validate tag version == csproj version; fail if mismatch
   - Run `.\scripts\wheel-overlay\build_msi.ps1`
   - Create GitHub release with tag `wheel-overlay/vX.Y.Z`, attach MSI and zip artifacts
   - Release notes sourced from `CHANGELOG.md` corresponding version section

**Version extraction (PowerShell)**:
```powershell
[xml]$csproj = Get-Content 'src/WheelOverlay/WheelOverlay.csproj'
$csprojVersion = $csproj.Project.PropertyGroup.Version
```

**Tag version validation**:
```powershell
$tagVersion = $env:GITHUB_REF_NAME -replace 'wheel-overlay/v', ''
if ($tagVersion -ne $csprojVersion) {
    Write-Error "Tag version '$tagVersion' does not match .csproj version '$csprojVersion'"
    exit 1
}
```

---

## Workflow: branch-build-check.yml (updated)

**Purpose**: Validate all PRs targeting `main`
**Updates required**: Add path filters; update CSPROJ paths

**Triggers**:
```yaml
on:
  push:
    branches-ignore: [main]
    paths:
      - 'src/**'
      - 'tests/**'
      - 'scripts/**'
      - '.github/workflows/**'
  pull_request:
    branches: [main]
    paths:
      - 'src/**'
      - 'tests/**'
      - 'scripts/**'
```

**Build step**:
- `dotnet build --configuration FastTests`
- `dotnet test --configuration FastTests` (10 PBT iterations)
- Validate property directives for both test projects

---

## Release Tag Format Contract

```
Pattern: {app-name}/v{major}.{minor}.{patch}

Valid examples:
  wheel-overlay/v0.7.0
  wheel-overlay/v1.0.0
  discord-notify/v1.0.0

Invalid examples:
  v0.7.0           (no app-name prefix)
  wheel-overlay/0.7.0  (missing v)
  wheel_overlay/v0.7.0 (underscore not hyphen)
```

**Property 6** validates the round-trip: format a tag from app-name + semver components, parse it back, recover original values.

---

## Workflow Removal

`release.yml` — **DELETE after `wheel-overlay-release.yml` is created and validated**.

The old workflow has incorrect CSPROJ paths (`WheelOverlay/WheelOverlay.csproj` vs `src/WheelOverlay/WheelOverlay.csproj`) and no namespaced release tags. It must not coexist with the new workflow.
