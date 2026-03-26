---
name: Documentation update procedure
description: Load when completing a spec or preparing to push — covers which documentation files to update, README structure, MkDocs site generation, and the pre-push checklist
type: reference
---

# Documentation Update Procedure

## When to update

| Trigger | Files to update |
|---------|----------------|
| After completing a spec | README.md + CHANGELOG.md + `docs/{app-name}/` |
| After adding new features | README.md Features section + relevant MkDocs page |
| After bug fixes (user-visible) | README.md Troubleshooting if relevant |
| After version change | README.md Version History |
| Before pushing to remote | All files below — run pre-push checklist |

## README.md required structure (in order)

1. Title and description
2. Features (organized by version, newest first)
3. Installation
4. Getting Started (first-time setup)
5. Usage (detailed instructions)
6. Troubleshooting
7. Development (build + test instructions)
8. Contributing
9. License
10. Support
11. Version History

## What to update in README.md

**Version History section**: Add new entry at the top of the list.
```markdown
### vX.Y.Z (YYYY-MM-DD)
- One-line summary of each major change
```

**Features section**: Add new features under the appropriate version heading.
Use user-friendly language; no internal/technical references.

**Getting Started / Usage**: Update if new features change how users interact
with the application. Add new sections for significant new functionality.

**Troubleshooting**: Add any new common issues introduced by the changes,
with solutions or workarounds.

**Development section**: Update build instructions if dependencies, .NET
version, or test commands change.

## MkDocs documentation site

### Site details

- **Primary domain**: `opendashoverlays.com`
- **Docs site**: `docs.opendashoverlays.com` (GitHub Pages)
- **Toolchain**: MkDocs with Material Design theme
- **Source location**: `docs/{app-name}/` in the repository root
- **Config file**: `mkdocs.yml` at the repository root

### Directory layout

```
docs/
└── wheel-overlay/
    ├── index.md           # Landing page / overview
    ├── installation.md
    ├── getting-started.md
    ├── usage.md
    ├── troubleshooting.md
    └── changelog.md       # Links to or mirrors CHANGELOG.md entries
mkdocs.yml                 # Site config; theme: material
```

### Local preview

```bash
# Install MkDocs and theme (one-time)
pip install mkdocs-material

# Serve locally with live reload
mkdocs serve

# Build and validate (mirrors CI)
mkdocs build --strict
```

The `--strict` flag treats warnings as errors (broken links, undefined
references). Always run this before pushing documentation changes.

### Adding docs for a new overlay app

1. Create `docs/{app-name}/` with at minimum `index.md` and `installation.md`.
2. Add the app section to `mkdocs.yml` under `nav:`.
3. Confirm `mkdocs build --strict` passes locally before opening the PR.

### CI/CD deployment

GitHub Actions builds and deploys the site automatically on every push to
`main` that touches `docs/**` or `mkdocs.yml`. The workflow:

1. Runs `mkdocs build --strict` — fails the build on any warning.
2. Runs `mkdocs gh-deploy` to push generated static content to the
   `gh-pages` branch.
3. Generated HTML is **never** committed to `main`.

## Pre-push checklist

- [ ] README.md version number matches project version in `.csproj`
- [ ] New features documented in Features section
- [ ] Usage instructions updated if behavior changed
- [ ] Troubleshooting updated for any new common issues
- [ ] Version History entry added
- [ ] All code examples and commands are tested and accurate
- [ ] CHANGELOG.md updated (see `procedures-changelog.md`)
- [ ] README and CHANGELOG tell a consistent story
- [ ] No broken links
- [ ] `docs/{app-name}/` updated for any new or changed user-facing features
- [ ] `mkdocs build --strict` passes locally

## Quality standards

- **User-focused**: write from the user's perspective ("how to" over "what is")
- **Accurate**: test instructions before documenting them
- **No technical jargon**: avoid class names, file paths, namespace references
  in user-facing sections (Development section is the exception)
- **Screenshots**: update if UI changes significantly
