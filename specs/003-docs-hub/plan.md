# Implementation Plan: OpenDash Overlays Documentation Hub

**Branch**: `docs/docs-hub` | **Date**: 2026-03-25 | **Spec**: `specs/003-docs-hub/`
**Input**: Feature specification from `specs/003-docs-hub/spec.md`

## Summary

Build a MkDocs Material documentation hub at `docs.opendashoverlays.com` using a hub-and-spoke architecture. The hub homepage auto-generates an App Gallery from `docs/` directory contents. Each overlay app contributes exactly five documentation pages. Three Python hooks enforce structure at build time, inject deprecation notices, and populate the App Gallery. GitHub Actions deploys on every qualifying push to `main`. The initial launch ships WheelOverlay as the only overlay section, with the hub infrastructure ready to onboard additional overlays with a single `mkdocs.yml` nav change.

## Technical Context

**Language/Version**: Python 3.14 (docs toolchain only; no C# changes in this feature)
**Primary Dependencies**: `mkdocs-material>=9.5` (ships with `pymdownx`, `admonition`, built-in search)
**Storage**: Static files — Markdown source in `docs/`, generated HTML deployed to `gh-pages` branch
**Testing**: `mkdocs build --strict` (runs hooks + link checker); no FsCheck/xUnit tests (no C# code)
**Target Platform**: GitHub Pages with custom domain `docs.opendashoverlays.com`
**Project Type**: Static documentation site
**Performance Goals**: First page load ≤ 2 seconds on broadband (SC-007); search results ≤ 2 seconds (SC-004; MkDocs Material client-side search, index built at compile time)
**Constraints**: Zero server-side infrastructure; no analytics; no auth; single-version docs only; GitHub Pages availability (best-effort)
**Scale/Scope**: 1 overlay at launch (WheelOverlay); architecture supports N overlays with O(1) config change per addition

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I — Monorepo ProjectReference** | ✅ N/A | No C# code changes. No new `.csproj`. |
| **II — Test-First PBT** | ✅ N/A | No C# correctness properties. Build validation is a Python hook, not an FsCheck test. |
| **III — Independent Per-App Versioning** | ✅ N/A | Documentation infrastructure; no app version bump required. |
| **IV — Changelog as Release Source of Truth** | ⚠ Required | CHANGELOG.md must record the launch of `docs.opendashoverlays.com`. Add under `[Unreleased]` as user-facing addition. |
| **V — Observability and Error Resilience** | ✅ N/A | No C# services or startup paths. |
| **VI — Branch Naming** | ✅ Pass | `docs/docs-hub` uses the valid secondary format (`docs/<description>`) for non-versioned cross-cutting docs work. |
| **VII — Documentation and Public Site** | ✅ This IS Principle VII | Feature implements all Principle VII non-negotiable rules: `docs.opendashoverlays.com`, MkDocs + Material, GitHub Pages, `--strict`, user-focused content, CI/CD deployment. |

**Gate result: PASS** (one mandatory action: CHANGELOG update)

## Project Structure

### Documentation (this feature)

```text
specs/003-docs-hub/
├── plan.md               # This file
├── research.md           # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/
│   ├── overlay-template-contract.md    # Phase 1 output
│   └── ci-cd-workflow-contract.md      # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit.tasks — not created here)
```

### Source Code (repository root)

```text
# New at repo root
mkdocs.yml                              # MkDocs site config

# Docs tooling dependencies
scripts/docs/
└── requirements.txt                    # pip deps: mkdocs-material>=9.5

# New docs tree
docs/
├── CNAME                               # docs.opendashoverlays.com
├── index.md                            # Hub homepage + App Gallery marker
├── common-setup/
│   └── index.md                        # Shared prerequisites (OS requirement)
├── contribute/
│   └── index.md                        # Documentation contribution guide (FR-017)
└── wheel-overlay/                      # Restructured from existing content
    ├── index.md                        # front-matter: title, description, deprecated: false
    ├── requirements.md                 # From: getting-started.md §Prerequisites
    ├── installation.md                 # From: getting-started.md §Installation + First Launch
    ├── configuration.md                # From: getting-started.md §Configuration + usage-guide.md
    └── troubleshooting.md              # Keep: troubleshooting.md (fold in tips.md content)

# New hooks (Python)
hooks/
├── gallery.py                          # on_page_markdown — App Gallery injection
├── deprecation.py                      # on_page_context — deprecated overlay notices
└── validate_structure.py              # on_nav — required page validation (build fails if missing)

# Existing docs files to retire (content merged into template pages)
# docs/wheel-overlay/getting-started.md  → DELETED (content in installation.md + requirements.md)
# docs/wheel-overlay/usage-guide.md      → DELETED (content in configuration.md)
# docs/wheel-overlay/tips.md             → DELETED (content folded into troubleshooting.md)

# CI/CD
.github/workflows/
└── deploy-docs.yml                     # New: build + deploy on push to main (path-filtered)
```

**Structure Decision**: Single-tier `docs/{app-name}/` layout with a flat top-level for structural sections (`common-setup`, `contribute`). No nested overlay namespacing. All build logic lives in `hooks/` at the repo root (alongside `scripts/`). The `docs/overrides/` directory is NOT created — MkDocs Material's native grid cards and admonition extensions handle all UI patterns without a custom theme override.

## Complexity Tracking

No constitution violations. No additional complexity to justify.

---

## Phase 0: Research Summary

All NEEDS CLARIFICATION items resolved. See `research.md` for full decision rationale.

| Decision | Resolution |
|----------|-----------|
| App Gallery mechanism | Python hook (`gallery.py`) scans `docs/` at build time; injects grid cards into `docs/index.md` via `<!-- APP_GALLERY -->` marker |
| Deprecated overlay notices | Python hook (`deprecation.py`) reads `index.md` front-matter; prepends danger admonition on all section pages |
| Build-time page validation | Python hook (`validate_structure.py`) on `on_nav` event; `raise SystemExit` on missing pages |
| GitHub Actions deployment | Two-job workflow: `build` (mkdocs build --strict) → `deploy` (actions/deploy-pages) |
| Custom domain CNAME | `docs/CNAME` file; MkDocs copies to `site/CNAME` at build time |
| Wheel-overlay docs restructure | Migrate from current 4-file layout to 5-page template; delete legacy files |
| Python dependencies | `scripts/docs/requirements.txt`: `mkdocs-material>=9.5` only |
| Common Setup initial scope | Windows 10/11 64-bit OS requirement (applies to all overlays) |
| Gallery exclusion list | `common-setup`, `contribute`, `overrides`, and any directory whose name starts with `_` or `.` excluded from gallery; all other `docs/` subdirs with `index.md` included |

---

## Phase 1: Design Summary

### Key Design Decisions

**App Gallery**: `docs/index.md` contains `<!-- APP_GALLERY -->` marker. The `gallery.py` hook intercepts `on_page_markdown` for the homepage, scans `docs/` for overlay directories (any subdir with `index.md`, excluding structural sections), reads front-matter for title/description/deprecated, and emits MkDocs Material grid-cards markdown. Deprecated entries include a badge. New overlays appear in the gallery automatically when their `index.md` is present.

**Build validation hook**: `validate_structure.py` fires on `on_nav`. For each nav section that is not `Home`, `Common Setup`, or `Contribute`, it verifies all five required filenames exist in the corresponding `docs/{app-name}/` directory. Missing files cause `raise SystemExit(...)` with a message identifying the overlay and the missing file.

**Deprecated notice hook**: `deprecation.py` fires on `on_files` to build a set of deprecated overlay names (by reading `index.md` front-matter from each overlay dir), then fires on `on_page_context` to prepend the danger admonition to any page whose `src_path` starts with a deprecated overlay name.

**mkdocs.yml nav structure**:
```yaml
nav:
  - Home: index.md
  - Common Setup: common-setup/index.md
  - WheelOverlay:
    - Overview: wheel-overlay/index.md
    - Requirements: wheel-overlay/requirements.md
    - Installation: wheel-overlay/installation.md
    - Configuration: wheel-overlay/configuration.md
    - Troubleshooting: wheel-overlay/troubleshooting.md
  - Contribute: contribute/index.md
```

**mkdocs.yml features**:
```yaml
theme:
  name: material
  features:
    - navigation.instant
    - navigation.tracking
    - navigation.top
    - search.suggest
    - search.highlight
    - content.code.copy

markdown_extensions:
  - admonition
  - pymdownx.details
  - pymdownx.superfences
  - attr_list
  - md_in_html

plugins:
  - search
```

### Constitution Check (Post-Design)

All Principle VII requirements are met by the design:
- `mkdocs.yml` at repo root with `theme: material` ✓
- Generated content NOT committed to `main` ✓
- GitHub Actions workflow builds with `--strict` and deploys on qualifying pushes ✓
- `docs/{app-name}/` source layout ✓
- User-focused content standards in contracts ✓
- CHANGELOG update required before merge ✓

### Entities and Contracts

- **OverlaySection**: documented in `data-model.md` — 5 required pages, mandatory front-matter on `index.md`
- **AppGalleryEntry**: derived entity populated by `gallery.py` hook
- **Overlay Template Contract**: `contracts/overlay-template-contract.md` — normative page list, front-matter schema, naming rules
- **CI/CD Workflow Contract**: `contracts/ci-cd-workflow-contract.md` — trigger conditions, jobs, permissions, local equivalence
