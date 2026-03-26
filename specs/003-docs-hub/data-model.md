# Data Model: OpenDash Overlays Documentation Hub

**Phase**: 1 — Design
**Feature**: `specs/003-docs-hub/`
**Date**: 2026-03-25

---

## Entities

### OverlaySection

Represents one overlay application's documentation section. The logical unit that the App Gallery displays, the build validator checks, and contributors create.

| Field | Type | Source | Constraints |
|-------|------|--------|-------------|
| `app_name` | string | Folder name under `docs/` | Lowercase, hyphen-separated (e.g., `wheel-overlay`). Must match `src/{AppName}/` convention (case-folded). |
| `title` | string | `docs/{app-name}/index.md` front-matter `title:` | Required. Displayed in App Gallery and page `<title>`. |
| `description` | string | `docs/{app-name}/index.md` front-matter `description:` | Required. One sentence. Displayed in App Gallery card. |
| `deprecated` | boolean | `docs/{app-name}/index.md` front-matter `deprecated:` | Optional; defaults to `false`. Drives deprecation badge and notices. |
| `status` | derived | Derived from `deprecated` | `"active"` or `"deprecated"`. Used in gallery display logic. |
| `pages` | DocumentationPage[] | Files in `docs/{app-name}/` | Must contain exactly five pages; see DocumentationPage. |

**Validation rules**:
- Every OverlaySection MUST contain exactly five DocumentationPages matching the Required Page Set.
- `title` and `description` MUST be present in `index.md` front-matter (non-empty strings).
- `app_name` MUST NOT be `common-setup` or `contribute` (structural sections, not overlay apps).

---

### DocumentationPage

Represents a single Markdown file within an overlay section. The unit that the build validator checks for presence.

| Field | Type | Source | Constraints |
|-------|------|--------|-------------|
| `page_type` | enum | File name | One of: `overview`, `requirements`, `installation`, `configuration`, `troubleshooting` |
| `file_name` | string | Actual file name | Must be exactly one of the Required Page Set filenames (see below). |
| `content` | string | File content | Must be non-empty (MkDocs Material warns on empty pages; `--strict` promotes to error). |
| `front_matter` | dict | YAML front-matter block | Only required on `index.md` (`title`, `description`). Optional on other pages. |

**Required Page Set** (normative — source for FR-003 and FR-010):
```
index.md            → page_type: overview
requirements.md     → page_type: requirements
installation.md     → page_type: installation
configuration.md    → page_type: configuration
troubleshooting.md  → page_type: troubleshooting
```

**Validation rules**:
- All five files MUST be present. Missing files cause build failure.
- If a page type is inapplicable (e.g., an overlay has no configuration), the file MUST still be present and MUST contain an explicit statement to that effect (e.g., "This overlay requires no configuration.").

---

### AppGalleryEntry

Derived entity. Populated at build time by the gallery hook. Represents one card in the App Gallery on the homepage.

| Field | Type | Derived From |
|-------|------|-------------|
| `title` | string | `OverlaySection.title` |
| `description` | string | `OverlaySection.description` |
| `deprecated` | boolean | `OverlaySection.deprecated` |
| `url_path` | string | `/{app_name}/` (relative to site root) |
| `badge_html` | string | Derived: `'<span class="md-badge">Deprecated</span>'` if `deprecated == true`, else `""` |

**Population rule**: All directories in `docs/` containing `index.md`, except `common-setup` and `contribute`, yield one AppGalleryEntry. Entries are sorted alphabetically by `app_name`.

---

### CommonSetupSection

Represents the shared prerequisites section. Not an overlay app — does not appear in the App Gallery.

| Field | Type | Notes |
|-------|------|-------|
| `url_path` | string | `/common-setup/` |
| `content_scope` | string[] | Prerequisites shared by two or more overlay apps |

**Initial content scope** (v1 launch):
- Windows 10/11 64-bit requirement (applies to all overlays)

**Extension rule**: A new prerequisite is added to Common Setup when it applies to two or more overlay apps. Per-app prerequisites live in the overlay's own `installation.md`.

---

### FrontMatterSchema

The normative YAML front-matter schema for `docs/{app-name}/index.md`. Other pages MAY include any standard MkDocs front-matter fields; only `index.md` has required custom fields.

```yaml
---
# Required fields (index.md only)
title: "WheelOverlay"                  # string — display name in gallery and page title
description: "One-sentence summary"   # string — shown in App Gallery card

# Optional fields (index.md only)
deprecated: false                      # boolean — drives deprecation badge and notices; defaults to false
---
```

All other pages in an overlay section MAY include standard MkDocs front-matter (`title:` for custom page title in nav) but have no custom required fields.

---

## State Transitions

### OverlaySection Status

```
[DRAFT — not in nav] → [ACTIVE — nav entry added, deprecated: false]
                                      ↓
                       [DEPRECATED — deprecated: true in index.md]
```

- Transition to ACTIVE: add `docs/{app-name}/` with 5 required pages + add nav entry to `mkdocs.yml`.
- Transition to DEPRECATED: set `deprecated: true` in `docs/{app-name}/index.md`. URL remains stable.
- No transition to REMOVED (URLs must remain stable per FR-016).

---

## Filesystem Layout

The authoritative source tree this feature creates:

```text
docs/
├── CNAME                              # Custom domain: docs.opendashoverlays.com
├── index.md                           # Hub homepage — App Gallery marker <!-- APP_GALLERY -->
├── common-setup/
│   └── index.md                       # Shared prerequisites
├── contribute/
│   └── index.md                       # Documentation contribution guide (FR-017)
└── wheel-overlay/
    ├── index.md                       # front-matter: title, description, deprecated: false
    ├── requirements.md
    ├── installation.md
    ├── configuration.md
    └── troubleshooting.md

hooks/
├── gallery.py                         # on_page_markdown — App Gallery injection
├── deprecation.py                     # on_page_context — deprecated notices
└── validate_structure.py             # on_nav — required page validation

mkdocs.yml                            # Site config at repo root (required by constitution Principle VII)

scripts/docs/
└── requirements.txt                  # mkdocs-material>=9.5

.github/workflows/
└── deploy-docs.yml                   # Build + deploy on push to main (path-filtered)
```
