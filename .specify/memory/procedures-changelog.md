---
name: Changelog update procedure
description: Load when finalizing a feature branch or preparing to merge — covers CHANGELOG.md format, entry writing rules, pre-merge checklist, and release promotion steps
type: reference
---

# Changelog Update Procedure

## Format (Keep a Changelog)

```markdown
# Changelog

## [Unreleased]
### Added
- **Feature Name**: User-facing description
  - Supporting detail as sub-bullet

### Changed
### Fixed
### Deprecated
### Removed
### Security

## [X.Y.Z] - YYYY-MM-DD
(same structure)
```

## When to update

| Trigger | Action |
|---------|--------|
| Starting a feature/fix branch | Add `[Unreleased]` section if missing |
| Completing a feature | Add entry under correct category |
| Before merging to main | Verify completeness; remove WIP markers |
| Creating a release | Promote `[Unreleased]` to version + date |

## Writing good entries

- **User-focused**: describe the user-visible effect, not the implementation
- **Action verb first**: "Added", "Fixed", "Changed" — not "We added" or "Adds"
- **Specific**: enough detail to understand the change without reading code
- **No technical internals**: no class names, method names, file paths, namespace references

```markdown
# Good
### Added
- **Grid Layout**: Configure overlay grid from 1–4 rows and 1–4 columns
  - Defaults to 2×4 for 8-position wheels
  - UI adapts dynamically when position count changes

# Bad
### Added
- Updated OverlayViewModel.cs to support configurable grid
```

## Pre-merge checklist

- [ ] `[Unreleased]` section exists
- [ ] Every user-facing change is documented
- [ ] Entries are in the correct category
- [ ] No WIP or TODO markers remain
- [ ] Spelling and grammar checked
- [ ] Format follows Keep a Changelog

## Releasing (promoting Unreleased → version)

1. Replace `## [Unreleased]` with `## [X.Y.Z] - YYYY-MM-DD`
2. Add a new empty `## [Unreleased]` section above it
3. Add a comparison link at the bottom of the file:
   ```markdown
   [X.Y.Z]: https://github.com/gadams999/opendash-overlays/compare/wheel-overlay/vA.B.C...wheel-overlay/vX.Y.Z
   ```
4. Copy the version section into the GitHub Release notes field

## Relationship to README

- CHANGELOG.md: detailed, all categories, full entry text
- README.md "Version History": summary only, one line per major change
- Keep them consistent but do not duplicate verbatim