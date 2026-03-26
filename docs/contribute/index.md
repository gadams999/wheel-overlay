---
title: "Contributing Documentation"
description: "How to add, update, and deprecate overlay documentation on this site"
---

# Contributing Documentation

This guide explains how to add documentation for a new overlay, keep existing documentation current, mark an overlay as deprecated, and validate your changes before opening a pull request.

## Adding a New Overlay Section

Each overlay gets its own directory under `docs/` containing exactly five required pages.

### 1. Create the overlay directory and pages

Create `docs/{app-name}/` (all lowercase, hyphen-separated — for example, `docs/my-overlay/`) with these five files:

```
docs/{app-name}/
├── index.md           # Overview — must include title: and description: front-matter
├── requirements.md    # System and hardware prerequisites
├── installation.md    # Step-by-step install and first-launch instructions
├── configuration.md   # All configurable settings and how to change them
└── troubleshooting.md # Symptom/solution pairs for common problems
```

Copy the front-matter schema from the WheelOverlay `index.md` as a starting point:

```yaml
---
title: "Your Overlay Name"
description: "One-sentence user-focused summary of what this overlay does."
deprecated: false
---
```

For pages where no content applies (for example, an overlay with no configurable settings), state this explicitly rather than leaving the page blank:

> This overlay requires no configuration.

### 2. Link to Common Setup for shared prerequisites

If your overlay requires Windows 10/11 64-bit, do not restate the requirement inline. Link to [Common Setup](/common-setup/) from your `installation.md` instead:

```markdown
Ensure your system meets the [Requirements](requirements.md) and complete
the shared operating system setup in [Common Setup](/common-setup/) before proceeding.
```

### 3. Add a nav entry to `mkdocs.yml`

`mkdocs.yml` is the **one** file outside `docs/{app-name}/` that you must update. Add your overlay entry after the last existing overlay and before the `Common Setup` entry:

```yaml
nav:
  - Home: index.md
  - Your Overlay Name:
    - Overview: {app-name}/index.md
    - Requirements: {app-name}/requirements.md
    - Installation: {app-name}/installation.md
    - Configuration: {app-name}/configuration.md
    - Troubleshooting: {app-name}/troubleshooting.md
  - Common Setup: common-setup/index.md
  - Contribute: contribute/index.md
```

The App Gallery on the homepage updates automatically — no further changes needed.

### 4. Validate before opening a PR

Run the following command from the repository root:

```bash
uv run --with-requirements scripts/docs/requirements.txt mkdocs build --strict
```

This runs identically to CI. If it passes locally, the CI build will pass. Fix any errors before opening a PR.

---

## Marking an Overlay as Deprecated

To mark an overlay as no longer actively maintained:

1. Open `docs/{app-name}/index.md`.
2. Set `deprecated: true` in the front-matter:

   ```yaml
   ---
   title: "MyOverlay"
   description: "..."
   deprecated: true
   ---
   ```

3. Run `mkdocs build --strict` to confirm the change builds correctly.

A **Deprecated** badge appears next to the overlay in the App Gallery, and a prominent warning banner appears at the top of every page in the section automatically. No further changes are required.

---

## Keeping Documentation Current

Documentation must be reviewed and kept current alongside each feature release. When a new version of an overlay changes user-facing behaviour — settings, steps, requirements — update the relevant pages in the same pull request as the code change.

---

## Pre-Push Validation

Always run before opening a PR that touches documentation:

```bash
uv run --with-requirements scripts/docs/requirements.txt mkdocs build --strict
```

**What `--strict` checks:**

- All internal links resolve to real pages
- All overlay sections contain the five required pages
- No empty pages
