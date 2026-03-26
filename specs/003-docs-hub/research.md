# Research: OpenDash Overlays Documentation Hub

**Phase**: 0 — Research
**Feature**: `specs/003-docs-hub/`
**Date**: 2026-03-25

---

## Decision 1: App Gallery Population Mechanism

**Decision**: Python hook (`hooks/gallery.py`) using MkDocs `on_page_markdown` event — scans `docs/` directory at build time, reads `{app-name}/index.md` front-matter, generates MkDocs Material grid-cards markdown, injects it in place of a `<!-- APP_GALLERY -->` marker in `docs/index.md`.

**Rationale**: Satisfies SC-003 and FR-009 (adding an overlay requires only `docs/{app-name}/` + one `mkdocs.yml` nav entry). The homepage `docs/index.md` never needs editing when new overlays are added — the hook drives the gallery from the filesystem. This approach requires zero additional pip packages beyond `mkdocs-material`.

**Alternatives considered**:
- *mkdocs-macros-plugin*: Clean Jinja2 macro call (`{{ app_gallery() }}`), but adds a second pip dependency and plugin config. Not needed when a hook achieves the same result.
- *Hardcoded homepage*: Simplest to write but violates SC-003 — `docs/index.md` would need editing for every new overlay.
- *Separate gallery data file*: Explicitly excluded by the spec assumption: "The App Gallery is generated from the site navigation configuration rather than requiring a separate data file."

**Gallery scanning rule**: The hook treats any subdirectory of `docs/` as an overlay section if it contains `index.md`. Directories prefixed with `_`, `.`, or `overrides`, and named directories `common-setup` and `contribute`, are excluded from the gallery scan — they are structural sections, not overlay apps. The overlay app list is ordered alphabetically.

---

## Decision 2: Build-Time Template Validation

**Decision**: Python hook (`hooks/validate_structure.py`) using MkDocs `on_nav` event. Iterates all nav entries, identifies overlay sections (all top-level nav entries except `Home`, `Common Setup`, `Contribute`), and verifies that each has exactly the five required pages. Fails with `raise SystemExit(...)` naming the overlay and missing page(s).

**Rationale**: Hooks run natively in MkDocs 1.4+ with no additional packages. The `on_nav` event fires after nav is built, giving access to the full page list. Using `raise SystemExit` (vs. `PluginError`) ensures the build fails visibly regardless of `--strict` mode, so validation runs in both local and CI contexts.

**Error message format**: `"Overlay validation failed — docs/{overlay}/: missing {page1}, {page2}. Each overlay section must contain: index.md, requirements.md, installation.md, configuration.md, troubleshooting.md"`

**Alternatives considered**:
- *Custom MkDocs plugin class*: More verbose (requires `BasePlugin` subclass, entry-points registration). Hooks achieve the same with a plain Python function.
- *Pre-commit script*: Would only catch errors locally, not in CI. Hook catches both.
- *`on_files` event*: Fires before nav is built; identifies files but not the semantic overlay structure. `on_nav` is more accurate.

---

## Decision 3: Deprecated Overlay Notices

**Decision**: Python hook (`hooks/deprecation.py`) using `on_page_context` event. Reads the overlay's `index.md` front-matter (`deprecated: true`), applies this status to all pages in the same overlay directory, and prepends a `!!! danger "Deprecated"` admonition to every affected page's rendered content.

**Rationale**: Front-matter on `index.md` is the single source of truth for overlay status (per FR-016). The hook propagates deprecated status to sibling pages by checking if the page's directory path matches a known-deprecated overlay. The `danger` admonition type (red) provides high visual prominence on every page without CSS overrides.

**Front-matter schema**:
```yaml
---
title: WheelOverlay       # Required on index.md; used for gallery title
description: "..."        # Required on index.md; used for gallery description
deprecated: false         # Optional; defaults to false; true triggers notice + badge
---
```

**Deprecation notice content**:
```
!!! danger "This overlay is deprecated"
    This overlay is no longer actively maintained. Pages remain accessible for reference.
```

**Alternatives considered**:
- *Tag-based approach (tags plugin)*: Would require the mkdocs-material Insiders plan for full tag functionality. Rejected on license cost grounds.
- *Nav-level flag in mkdocs.yml*: Not supported by MkDocs nav schema; would require custom parsing.
- *Manual admonition on each page*: Violates DRY; also easy to miss when adding new pages.

---

## Decision 4: GitHub Actions Deployment

**Decision**: Two-job workflow using GitHub-native actions — `actions/upload-pages-artifact@v3` + `actions/deploy-pages@v4`. Build job runs `mkdocs build --strict`; deploy job publishes to GitHub Pages.

**Rationale**: GitHub-native deployment is simpler (no PAT or external secrets needed), uses `permissions: pages: write` + `id-token: write` (OIDC), and the `environment: github-pages` block adds deployment tracking to the repository. `peaceiris/actions-gh-pages` is community-maintained and requires a PAT for private repos; the GitHub-native approach is first-party.

**Path filter**: Workflow triggers on push to `main` with `paths: ['docs/**', 'mkdocs.yml', 'hooks/**', '.github/workflows/deploy-docs.yml']`. The `hooks/**` path is included because hook changes affect the built site even if no Markdown changed.

**Alternatives considered**:
- *`peaceiris/actions-gh-pages`*: Widely used, mature. Requires `cname:` parameter or a `docs/CNAME` file. Acceptable alternative but not first-party.
- *`mkdocs gh-deploy` directly*: Force-pushes to `gh-pages` from the runner, requires PAT for pushes. More fragile than artifact-based deploy.

---

## Decision 5: Custom Domain (CNAME)

**Decision**: `docs/CNAME` file containing `docs.opendashoverlays.com`. MkDocs copies this to `site/CNAME` during build; GitHub Pages reads it from the deployed `gh-pages` branch.

**Rationale**: The CNAME file approach is the most portable — works with any GitHub Pages deployment action without workflow-level configuration. No mkdocs.yml entry is needed.

**DNS setup** (out of scope for this spec, noted for implementation reference): A `CNAME` DNS record at the registrar pointing `docs.opendashoverlays.com` → `<github-org>.github.io`.

---

## Decision 6: Existing Wheel-Overlay Docs Restructure

**Decision**: Migrate `docs/wheel-overlay/` from the current layout (getting-started.md, usage-guide.md, troubleshooting.md, tips.md) to the 5-page template (index.md, requirements.md, installation.md, configuration.md, troubleshooting.md).

**Content mapping**:
| New file | Source |
|----------|--------|
| `index.md` | New: overview/intro paragraph + app gallery entry front-matter |
| `requirements.md` | Extracted from `getting-started.md` Prerequisites section |
| `installation.md` | Extracted from `getting-started.md` Installation + First Launch sections |
| `configuration.md` | Merged from `getting-started.md` configuration steps + `usage-guide.md` |
| `troubleshooting.md` | Keep existing `troubleshooting.md`; fold in `tips.md` where relevant |

**Files to remove**: `getting-started.md`, `usage-guide.md`, `tips.md` (content merged into template pages).

**Rationale**: The 5-page template is normative (FR-003); the existing structure is pre-spec. The build validator (Decision 2) will fail the build until the restructure is complete, enforcing the migration.

---

## Decision 7: Python Dependencies

**Decision**: `requirements-docs.txt` at repo root with a single dependency: `mkdocs-material`. All hooks are pure Python (no third-party libraries beyond the MkDocs core that ships with `mkdocs-material`).

**Location**: `scripts/docs/requirements.txt` — consistent with the monorepo convention of keeping tooling scripts and deps under `scripts/{scope}/`.

**Contents**:
```
mkdocs-material>=9.5
```

**Rationale**: Minimal dependency surface. The `pymdownx` extensions (superfences, admonition) ship with `mkdocs-material`. No macros plugin, no git revision plugin, no tags plugin (Insiders-only) needed.

---

## Decision 8: Common Setup Scope

**Decision**: The initial `docs/common-setup/` section documents **one** shared prerequisite: Windows 10/11 64-bit operating system requirement (applies to all overlays). Additional shared prerequisites (OBS configuration, audio routing, etc.) are added as future overlays are introduced.

**Rationale**: Per the spec assumption, "Common Setup content scope will be determined during the planning phase based on the actual dependencies of current and near-future overlays." WheelOverlay's only non-overlay-specific prerequisite is the OS requirement. DirectInput wheel hardware is WheelOverlay-specific. A one-page `docs/common-setup/index.md` establishes the section structure without bloating it for hypothetical future content.

---

## Decision 9: Front-Matter-Derived Gallery Exclusion List

**Decision**: The hook uses a **whitelist of structural sections** to exclude from the App Gallery: `['common-setup', 'contribute']`. Any other `docs/` subdirectory with an `index.md` is treated as an overlay app and included in the gallery.

**Rationale**: An explicit exclusion list is more robust than an opt-in flag — new overlay contributors don't need to add an "include_in_gallery: true" flag; inclusion is the default. Structural sections are stable and unlikely to proliferate.

---

## Checklist Item Resolutions

Resolving checklist items from `specs/003-docs-hub/checklists/contributor-workflow.md`:

| Item | Resolution |
|------|-----------|
| CHK001 | FR-017 exists as explicit deliverable ✓ |
| CHK002 | Contribution guide lives at `docs/contribute/index.md` — a page on the docs site |
| CHK003 | Guide sync on template change — out of scope; treated as normal maintenance |
| CHK004 | FR-003 provides normative list; FR-010 references it explicitly ✓ |
| CHK005 | Minimum content: file must exist and be non-empty. Empty files fail `mkdocs build --strict` (empty page warning). Stub placeholder prohibition deferred to future review. |
| CHK006 | Stub content: permitted at initial PR if non-empty. Not enforced by build. |
| CHK007 | Folder naming: lowercase, hyphen-separated (e.g., `wheel-overlay`), matching the established monorepo `src/{AppName}/` convention. |
| CHK008 | FR-009 explicitly names `mkdocs.yml` as the one permitted external file ✓ |
| CHK009 | Gallery population mechanism: Python hook scans `docs/` directory at build time — stated explicitly in plan |
| CHK010 | SC-003 is verifiable: count file changes in a PR that adds `docs/{new-app}/` ✓ |
| CHK011 | Gallery appearance is automatic via hook scan — no manual gallery update required ✓ |
| CHK012 | Error format: `"docs/{overlay}/: missing {page}"` — naming both overlay and page ✓ |
| CHK013 | FR-010 references "Required Template Page List (FR-003)" ✓ |
| CHK014 | Local validation: `mkdocs build --strict` runs the same hooks — identical to CI ✓ |
| CHK015 | Deployment cycle duration: one GitHub Actions run; typically completes within 5 minutes of merge ✓ |
| CHK016 | Deprecation mechanism: `deprecated: true` in `docs/{app-name}/index.md` front-matter — per FR-016 ✓ |
| CHK017 | Deprecation access: any contributor (no restriction beyond normal PR review) |
| CHK018 | Badge content: must display "Deprecated" text label. Reason/date/replacement: optional front-matter fields, not enforced |
| CHK019 | "Prominent" = first rendered content element on every page of the section (before any page content) |
| CHK020 | Overlay folder renaming: out of scope. URL stability applies to deprecated overlays (FR-016). Active overlays are not renamed without a separate spec. |
| CHK021 | Concurrent PRs: standard git merge conflict on `mkdocs.yml`. No special tooling. |
| CHK022 | Removing deprecated docs entirely: out of scope. FR-016 requires stable URLs. |
