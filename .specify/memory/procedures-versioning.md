---
name: Version bump procedure
description: Load when creating tasks or implementing a feature/fix branch — covers which files to update, order of operations, and verification steps for per-app version bumps
type: reference
---

# Version Bump Procedure

## When to apply

- Creating any `feat/*` or `fix/*` branch that will be merged to `main`
- First commit on the branch, before any feature work

## Files to update (WheelOverlay)

| File | Field(s) |
|------|---------|
| `src/WheelOverlay/WheelOverlay.csproj` | `<Version>`, `<AssemblyVersion>`, `<FileVersion>` |
| `installers/wheel-overlay/Package.wxs` | `Version="X.Y.Z"` attribute on `<Package>` element |
| `scripts/wheel-overlay/build_release.ps1` | `$zipPath` variable containing the version string |

When a new overlay app is added, it will have an equivalent set of three files.

## Order of operations

1. Create the branch (`git checkout -b fix/v0.5.3-description`)
2. Update all three files to the new version
3. Commit: `chore: bump version to X.Y.Z`
4. Proceed with feature/fix implementation

## Reading version in scripts/CI

```powershell
# Read version from csproj (used by build scripts and CI)
$xml = [xml](Get-Content "src/WheelOverlay/WheelOverlay.csproj")
$version = $xml.Project.PropertyGroup.Version
```

## Verification after update

1. `dotnet build` from repository root
2. Run the application
3. Open About (system tray → About Wheel Overlay)
4. Confirm displayed version matches the bumped value

## SemVer rules

- **MAJOR**: breaking changes (settings schema migration required, API removal)
- **MINOR**: new features, backward compatible
- **PATCH**: bug fixes, backward compatible