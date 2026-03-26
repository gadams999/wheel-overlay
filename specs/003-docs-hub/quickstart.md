# Quickstart: Documentation Hub Development

**Audience**: Contributors working on the docs site itself (not end-users of the overlays).

---

## Prerequisites

- Python 3.14 installed
- Git repository cloned

## One-Time Setup

No setup step required — `uv run` resolves and caches dependencies automatically on first use.

---

## Local Preview

```bash
# Serve with live reload (auto-refreshes browser on file save)
uv run --with-requirements scripts/docs/requirements.txt mkdocs serve
```

Open `http://127.0.0.1:8000` in a browser. Changes to `docs/**`, `mkdocs.yml`, and `hooks/**` trigger automatic rebuild.

---

## Pre-Push Validation

Always run before opening a PR that touches docs:

```bash
uv run --with-requirements scripts/docs/requirements.txt mkdocs build --strict
```

This runs identically to CI. If this passes locally, the CI build will pass.

**What `--strict` checks**:
- All internal links resolve to real pages (FR-013)
- All overlay sections have the five required pages (hook: validate_structure.py, FR-010)
- No empty pages

---

## Adding a New Overlay Section

1. Create `docs/{app-name}/` with five required files:
   ```
   docs/{app-name}/
   ├── index.md           # Must include title:, description: front-matter
   ├── requirements.md
   ├── installation.md
   ├── configuration.md
   └── troubleshooting.md
   ```

2. Add nav entry to `mkdocs.yml` (the **one** external file change):
   ```yaml
   - <Display Name>:
     - Overview: <app-name>/index.md
     - Requirements: <app-name>/requirements.md
     - Installation: <app-name>/installation.md
     - Configuration: <app-name>/configuration.md
     - Troubleshooting: <app-name>/troubleshooting.md
   ```

3. Run `mkdocs build --strict` to verify the new section passes validation.

The App Gallery on the homepage updates automatically — no further changes needed.

---

## Marking an Overlay as Deprecated

1. Open `docs/{app-name}/index.md`.
2. Set `deprecated: true` in the front-matter block:
   ```yaml
   ---
   title: "MyOverlay"
   description: "..."
   deprecated: true
   ---
   ```
3. Run `mkdocs build --strict` to preview the deprecation notices.

The deprecation badge appears in the App Gallery and a notice banner appears on every page of the section automatically.

---

## Folder Structure Reference

```text
docs/                        # All MkDocs source content
  CNAME                      # Custom domain (do not edit)
  index.md                   # Hub homepage with App Gallery
  common-setup/index.md      # Shared prerequisites
  contribute/index.md        # How to contribute docs
  wheel-overlay/             # Per-overlay section (example)
    index.md
    requirements.md
    installation.md
    configuration.md
    troubleshooting.md

hooks/                       # Python build hooks
  gallery.py                 # Generates App Gallery
  deprecation.py             # Injects deprecated notices
  validate_structure.py      # Validates required page structure

mkdocs.yml                   # Site config (edit nav here only)

scripts/docs/
  requirements.txt           # pip dependencies for docs build
```
