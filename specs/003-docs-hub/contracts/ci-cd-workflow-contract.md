# Contract: CI/CD Documentation Deployment Workflow

**File**: `.github/workflows/deploy-docs.yml`
**Trigger**: Push to `main` branch with path filter
**Output**: Static site deployed to GitHub Pages at `docs.opendashoverlays.com`

---

## Trigger Conditions

```yaml
on:
  push:
    branches: [main]
    paths:
      - 'docs/**'
      - 'mkdocs.yml'
      - 'hooks/**'
      - '.github/workflows/deploy-docs.yml'
  workflow_dispatch:  # Allow manual trigger
```

The `hooks/**` path is included because hook changes affect the built site output even when no Markdown files change.

---

## Jobs

### Job 1: build

| Property | Value |
|----------|-------|
| Runner | `ubuntu-latest` |
| Python version | `3.14` |
| pip cache key | `pip` |
| Build command | `mkdocs build --strict --verbose` |
| Output artifact | `site/` directory (MkDocs default output) |

**Steps**:
1. `actions/checkout@v4` — full clone required (hooks read source files)
2. `actions/setup-python@v5` with cache
3. `pip install -r scripts/docs/requirements.txt`
4. `mkdocs build --strict --verbose`
5. `actions/upload-pages-artifact@v3` — uploads `site/` as the pages artifact

**Failure conditions** (any of these fails the build):
- Broken internal link detected by MkDocs `--strict`
- Missing required overlay page detected by `hooks/validate_structure.py`
- Malformed Markdown that prevents rendering
- Python syntax error in any hook file

### Job 2: deploy

| Property | Value |
|----------|-------|
| Depends on | `build` job (must succeed) |
| Runner | `ubuntu-latest` |
| Environment | `github-pages` (enables deployment tracking) |

**Steps**:
1. `actions/deploy-pages@v4` — deploys the artifact from job 1 to GitHub Pages

---

## Required Permissions

```yaml
permissions:
  contents: read
  pages: write
  id-token: write
```

`id-token: write` enables OIDC-based authentication — no personal access token or secret required.

---

## Concurrency

```yaml
concurrency:
  group: pages
  cancel-in-progress: false
```

`cancel-in-progress: false` ensures an in-progress deployment is not cancelled by a subsequent push — the queue completes in order.

---

## Custom Domain (CNAME)

The file `docs/CNAME` with contents `docs.opendashoverlays.com` is copied by MkDocs to `site/CNAME` during build. GitHub Pages reads this file from the deployed artifact and configures the custom domain automatically.

**DNS prerequisite** (out of scope — configured at registrar):
- `CNAME` record: `docs.opendashoverlays.com` → `<github-org>.github.io`

---

## Local Equivalence

The CI build step is reproducible locally:

```bash
pip install -r scripts/docs/requirements.txt
mkdocs build --strict
```

Running `mkdocs build --strict` locally executes the same hooks and link-checking as CI. Developers MUST run this before opening a PR that touches `docs/**`, `mkdocs.yml`, or `hooks/**`.

For live preview with auto-reload:
```bash
mkdocs serve
```

Note: `mkdocs serve` does not run `--strict`; always use `mkdocs build --strict` for pre-push validation.

---

## GitHub Repository Settings

The following repository settings are required (one-time setup, out of scope for this feature's tasks):

| Setting | Value |
|---------|-------|
| Pages source | GitHub Actions (not branch) |
| Custom domain | `docs.opendashoverlays.com` |
| Enforce HTTPS | Enabled |
| `gh-pages` branch | Managed by GitHub Actions — do not edit manually |
