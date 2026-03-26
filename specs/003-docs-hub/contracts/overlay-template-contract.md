# Contract: Overlay Documentation Template

**Scope**: Every overlay app documentation section in `docs/{app-name}/`
**Enforced by**: `hooks/validate_structure.py` (build-time), CI (`mkdocs build --strict`)

---

## Required Files

Every overlay documentation section MUST contain exactly these five files, in this order in `mkdocs.yml` nav:

| File | Nav label | Purpose |
|------|-----------|---------|
| `index.md` | Overview | Landing page; required front-matter; app gallery source |
| `requirements.md` | Requirements | System and hardware prerequisites |
| `installation.md` | Installation | Step-by-step install instructions |
| `configuration.md` | Configuration | All configurable settings and how to change them |
| `troubleshooting.md` | Troubleshooting | Common issues and solutions |

No page may be omitted. If a page's topic is inapplicable, the file MUST be present and MUST contain an explicit "not applicable" statement.

---

## `index.md` Front-Matter Schema

The `index.md` file MUST include the following YAML front-matter block at the top of the file:

```yaml
---
title: "<Display Name>"               # Required: shown in App Gallery card and page <title>
description: "<One-sentence summary>" # Required: shown in App Gallery card
deprecated: false                     # Optional: set to true to activate deprecation notices
---
```

**Field rules**:
- `title`: non-empty string; the human-readable overlay name (e.g., `"WheelOverlay"`).
- `description`: non-empty string; one sentence; no trailing period; user-focused language; no class names or file paths.
- `deprecated`: boolean (`true` or `false`); defaults to `false` when absent. Setting to `true` triggers deprecation notices on all pages in the section and a deprecation badge in the App Gallery.

---

## mkdocs.yml Nav Entry

Each overlay section MUST be registered in `mkdocs.yml` nav with this structure:

```yaml
nav:
  - <Display Name>:
    - Overview: <app-name>/index.md
    - Requirements: <app-name>/requirements.md
    - Installation: <app-name>/installation.md
    - Configuration: <app-name>/configuration.md
    - Troubleshooting: <app-name>/troubleshooting.md
```

**Rules**:
- The top-level nav key matches `index.md` `title:` field.
- The five sub-entries MUST appear in the order above.
- This nav entry is the ONE permitted change to files outside `docs/{app-name}/` when adding a new overlay.

---

## `docs/{app-name}/` Folder Naming

- All lowercase.
- Words separated by hyphens (e.g., `wheel-overlay`, `discord-chat`).
- No underscores, no camelCase.
- Matches the pattern used in `src/{AppName}/` (case-folded, hyphen-separated).

---

## Deprecation Protocol

To mark an overlay as deprecated:

1. Set `deprecated: true` in `docs/{app-name}/index.md` front-matter.
2. No other files require modification.

Effect (automatic, driven by `hooks/deprecation.py`):
- A `!!! danger "This overlay is deprecated"` admonition appears as the **first content element** on every page in the section.
- The App Gallery card shows a **Deprecated** badge next to the overlay title.
- URLs remain stable (HTTP 200); deprecated pages are searchable.

---

## Build Validation

The build hook (`hooks/validate_structure.py`) validates the following on every `mkdocs build`:

1. Every top-level nav entry (excluding `Home`, `Common Setup`, `Contribute`) has exactly five sub-entries.
2. All five required file names are present in the corresponding `docs/{app-name}/` directory.
3. If any required file is missing, the build fails with:
   ```
   Overlay validation failed — docs/{overlay}/: missing {page}.
   Each overlay section must contain: index.md, requirements.md, installation.md, configuration.md, troubleshooting.md
   ```

---

## Content Standards

Following constitution Principle VII and procedures-documentation.md quality standards:

- **User-focused**: write from the user's perspective ("how to" over "what is").
- **No internal references**: no class names, file paths, or namespace references in user-facing sections (Developer Guide excepted).
- **Unique SEO titles**: each `index.md` `title:` is unique and includes the overlay name (FR-014).
- **Semantic headings**: `# H1` for page title, `## H2` for top-level sections, `### H3` for subsections — never skip levels (FR-015).
- **Links to Common Setup**: per-overlay `installation.md` MUST link to `/common-setup/` for any prerequisite shared across two or more overlays, rather than duplicating the content (FR-005).
