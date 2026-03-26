# Tasks: OpenDash Overlays Documentation Hub

**Input**: Design documents from `specs/003-docs-hub/`
**Branch**: `docs/docs-hub`
**Prerequisites**: plan.md ✓, spec.md ✓, data-model.md ✓, contracts/ ✓, research.md ✓, quickstart.md ✓

**Tests**: No test tasks — not requested in spec. `mkdocs build --strict` serves as the integration test
harness for every phase.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Exact file paths included in all task descriptions

---

## Phase 1: Setup (Project Scaffolding and Toolchain)

**Purpose**: Create the docs toolchain foundation — Python dependencies, MkDocs config skeleton, and
custom domain file.

- [x] T001 Create `scripts/docs/requirements.txt` with content `mkdocs-material>=9.5`
- [x] T002 Create minimal `mkdocs.yml` at repo root with: `site_name`, `site_url: https://docs.opendashoverlays.com`, `theme: {name: material}`, `markdown_extensions` (admonition, pymdownx.details, pymdownx.superfences, attr_list, md_in_html), `plugins: [search]`, and `hooks` list referencing `hooks/gallery.py`, `hooks/deprecation.py`, `hooks/validate_structure.py`
- [x] T003 [P] Create `docs/CNAME` with single line content `docs.opendashoverlays.com`

---

## Phase 2: Foundational (Python Hooks and CI/CD Workflow)

**Purpose**: Build-time validation hooks and automated deployment pipeline — MUST be complete before any
documentation content can be verified with `mkdocs build --strict`.

**⚠️ CRITICAL**: No user story content can pass build validation until this phase is complete.

- [x] T004 Implement `hooks/validate_structure.py` — MkDocs `on_nav` hook that iterates all top-level nav entries, skips entries named Home, Common Setup, and Contribute, then for each remaining overlay entry verifies that exactly five sub-entries exist with filenames matching the Required Page Set (index.md, requirements.md, installation.md, configuration.md, troubleshooting.md); raises `SystemExit` with message `"Overlay validation failed — docs/{overlay}/: missing {page}. Each overlay section must contain: index.md, requirements.md, installation.md, configuration.md, troubleshooting.md"` on any failure
- [x] T005 [P] Implement `hooks/gallery.py` — MkDocs `on_page_markdown` hook that fires only when the current page is the homepage (`docs/index.md`); scans all subdirectories of `docs/` for those containing `index.md`, excludes `common-setup`, `contribute`, `overrides`, and any directory whose name starts with `_` or `.`; reads `title`, `description`, and `deprecated` from each overlay `index.md` YAML front-matter; generates MkDocs Material grid-cards markdown (one card per overlay, sorted alphabetically by app_name, deprecated entries include a `Deprecated` badge); replaces the `<!-- APP_GALLERY -->` marker in the page markdown with the generated grid
- [x] T006 [P] Implement `hooks/deprecation.py` — MkDocs hook using two events: `on_files` builds a set of deprecated overlay directory names by reading `index.md` front-matter (`deprecated: true`) from each overlay dir; `on_page_context` checks if the current page's `src_path` starts with any deprecated overlay name and if so prepends `!!! danger "This overlay is deprecated"\n    This overlay is no longer actively maintained.\n` as the first content element on the page
- [x] T007 Create `.github/workflows/deploy-docs.yml` per CI/CD workflow contract: trigger `on.push` to `main` with path filter covering `docs/**`, `mkdocs.yml`, `hooks/**`, `.github/workflows/deploy-docs.yml`, plus `workflow_dispatch`; `build` job on `ubuntu-latest` with `actions/setup-python@v5` (`python-version: '3.14'`, `cache: 'pip'`), runs `pip install -r scripts/docs/requirements.txt` then `mkdocs build --strict --verbose`, uploads `site/` via `actions/upload-pages-artifact@v3`; `deploy` job depends on `build`, uses `actions/deploy-pages@v4` with `environment: github-pages`; top-level `permissions: {contents: read, pages: write, id-token: write}`; `concurrency: {group: pages, cancel-in-progress: false}`

**Checkpoint**: Foundation ready — hooks and CI/CD pipeline in place. WheelOverlay documentation content can now be created and validated.

---

## Phase 3: User Story 1 - First-Time User Finds and Installs an Overlay (Priority: P1) 🎯 MVP

**Goal**: WheelOverlay documentation is live with a functional App Gallery; a first-time user can
discover the overlay, navigate to its section, and complete the full install journey without leaving
the site.

**Independent Test**: Run `mkdocs build --strict` — must pass with zero errors. Open `mkdocs serve`,
navigate to homepage, confirm WheelOverlay card appears in App Gallery, click through to Overview page,
navigate Requirements → Installation → Configuration → Troubleshooting, and confirm all steps are
present, unambiguous, and link to Common Setup where applicable.

### Implementation for User Story 1

- [x] T008 [US1] Create `docs/index.md` — hub homepage with H1 "OpenDash Overlays Documentation", brief intro paragraph (one to two sentences, user-focused), and the `<!-- APP_GALLERY -->` marker on its own line where the gallery will be injected; include YAML front-matter with `title: "OpenDash Overlays"` and `description: "Documentation hub for OpenDash overlay applications"`
- [x] T009 [P] [US1] Create `docs/wheel-overlay/index.md` — Overview page with YAML front-matter (`title: "WheelOverlay"`, `description: "<one-sentence user-focused summary>"`, `deprecated: false`), H1 "WheelOverlay", and user-facing overview content describing what the overlay does and who it is for; unique SEO title per FR-014
- [x] T010 [P] [US1] Create `docs/wheel-overlay/requirements.md` — Requirements page with H1 "Requirements", semantic H2/H3 sections for system requirements (Windows 10/11 64-bit) and hardware prerequisites; migrate relevant content from `docs/wheel-overlay/getting-started.md` §Prerequisites section; user-focused language, no class names or file paths
- [x] T011 [P] [US1] Create `docs/wheel-overlay/installation.md` — Installation page with H1 "Installation", step-by-step numbered install instructions and first-launch verification; migrate content from `docs/wheel-overlay/getting-started.md` §Installation and §First Launch sections; include explicit link to `/common-setup/` for the Windows OS shared prerequisite (FR-005 compliance)
- [x] T012 [P] [US1] Create `docs/wheel-overlay/configuration.md` — Configuration page with H1 "Configuration"; document all configurable settings, their default values, and how to change each one; migrate content from `docs/wheel-overlay/usage-guide.md`; if no configuration is required, state "This overlay requires no configuration." explicitly per FR-003
- [x] T013 [P] [US1] Create `docs/wheel-overlay/troubleshooting.md` — Troubleshooting page with H1 "Troubleshooting"; migrate content from `docs/wheel-overlay/troubleshooting.md` and fold in tips content from `docs/wheel-overlay/tips.md`; organize by symptom/solution pattern under H2 headings
- [x] T014 [US1] Update `mkdocs.yml` nav — replace stub nav with Phase 3 nav: `Home: index.md` and WheelOverlay five-page entry (Overview/Requirements/Installation/Configuration/Troubleshooting) only; do NOT add Common Setup or Contribute entries yet (their source files don't exist until Phase 5 and Phase 6 respectively — adding them now would break `mkdocs build --strict`); add `theme.features` list: navigation.instant, navigation.tracking, navigation.top, search.suggest, search.highlight, content.code.copy
- [x] T015 [US1] Delete legacy docs files no longer needed after content migration: `docs/wheel-overlay/getting-started.md`, `docs/wheel-overlay/usage-guide.md`, `docs/wheel-overlay/tips.md` (content has been migrated into the five template pages above)
- [x] T016 [US1] Run `mkdocs build --strict` — verify zero errors, App Gallery card renders for WheelOverlay, validate_structure.py confirms five pages present, all internal links resolve; fix any issues before proceeding

**Checkpoint**: User Story 1 complete — WheelOverlay documentation is independently browsable and
verifiable. `mkdocs build --strict` passes with zero errors.

---

## Phase 4: User Story 2 - Returning User Searches Across All Documentation (Priority: P2)

**Goal**: Site-wide search is functional and configured correctly; a returning user can find any page
by keyword without knowing which overlay section it lives in.

**Independent Test**: Run `mkdocs build --strict`, open `mkdocs serve`, type a known keyword from
WheelOverlay configuration into the search field, confirm the correct page appears in results. Type a
nonsense query and confirm a "no results" state is visible with navigation back to homepage available
via the nav bar.

### Implementation for User Story 2

- [x] T017 [US2] Verify and finalize search and navigation feature configuration in `mkdocs.yml` — confirm `search.suggest`, `search.highlight`, `navigation.instant`, `navigation.tracking`, and `navigation.top` are all present in `theme.features`; confirm the `search` plugin is listed under `plugins`; confirm nav structure allows reaching any page within three clicks from homepage (FR-008)
- [x] T018 [US2] Review `docs/index.md` — confirm homepage App Gallery and nav bar together provide a clear recovery path for users arriving from a failed search (no separate "no results" page needed; MkDocs Material handles the no-results UI automatically)

**Checkpoint**: User Story 2 complete — search indexes all content pages and navigation supports
three-click access to any page from the homepage.

---

## Phase 5: User Story 3 - User Completes a Shared Prerequisite Once (Priority: P3)

**Goal**: Common Setup section is live and contains all globally shared prerequisites; per-overlay
installation pages link to it rather than duplicating content.

**Independent Test**: Navigate from `docs/wheel-overlay/installation.md` to the Common Setup section
and confirm the Windows 10/11 64-bit requirement is documented there. Confirm `installation.md` links
to `/common-setup/` rather than duplicating the OS requirement inline. Run `mkdocs build --strict` —
the Common Setup internal link must resolve.

### Implementation for User Story 3

- [x] T019 [US3] Create `docs/common-setup/index.md` — Shared Prerequisites page with H1 "Common Setup", intro explaining this section covers requirements shared across all OpenDash overlay applications, H2 "Operating System" with Windows 10/11 64-bit requirement and any other globally shared prerequisites; user-focused language, no internal references, semantic headings (FR-015)
- [x] T020 [US3] Add `Common Setup: common-setup/index.md` nav entry to `mkdocs.yml` — insert between Home and WheelOverlay entries; this is the first time this entry appears in the nav (it was intentionally omitted from T014 because the file didn't exist yet); validate_structure.py excludes this entry from the five-page check by name
- [x] T021 [US3] Verify `docs/wheel-overlay/installation.md` — confirm the Windows OS requirement links to `/common-setup/` rather than stating it inline (FR-005); run `mkdocs build --strict` to confirm the link resolves

**Checkpoint**: User Story 3 complete — Common Setup is live; WheelOverlay installation page links to
it; no prerequisite content is duplicated.

---

## Phase 6: User Story 4 - Developer Adds a New Overlay's Documentation (Priority: P4)

**Goal**: Contribution guide is live; a developer can add a new overlay section by following the guide;
the build rejects incomplete sections with a clear error.

**Independent Test**: Follow `docs/contribute/index.md` to add a stub overlay (`docs/test-overlay/`)
with five required pages plus a nav entry. Run `mkdocs build --strict` — confirm the stub overlay
appears in App Gallery. Remove one required page and re-run — confirm the build fails with the expected
error message. Delete `docs/test-overlay/` and its nav entry when done.

### Implementation for User Story 4

- [x] T022 [P] [US4] Create `docs/contribute/index.md` — Documentation Contribution Guide per FR-017 with H1 "Contributing Documentation", sections covering: (1) adding a new overlay section (create `docs/{app-name}/` with five required files, copy front-matter schema from overlay-template-contract.md, add nav entry to `mkdocs.yml` as the one permitted external file change, run `mkdocs build --strict` to validate); (2) linking to Common Setup for shared prerequisites; (3) marking an overlay as deprecated (`deprecated: true` in `index.md` front-matter); (4) pre-push validation command (`mkdocs build --strict`); user-focused, no internal file paths in prose, semantic headings
- [x] T023 [US4] Add `Contribute: contribute/index.md` nav entry to `mkdocs.yml` — append as the last nav entry; this is the first time this entry appears in the nav (intentionally omitted from T014 because the file didn't exist yet); validate_structure.py excludes this entry from the five-page check by name
- [x] T024 [US4] Verify `hooks/validate_structure.py` error output — temporarily add an incomplete test overlay nav entry (missing pages) and run `mkdocs build --strict`; confirm the error message matches the contract format `"Overlay validation failed — docs/{overlay}/: missing {page}."` and names the specific missing file; restore nav to pre-test state

**Checkpoint**: User Story 4 complete — contribution guide is live; developers can self-serve overlay
documentation onboarding; build validation rejects incomplete sections with a clear, actionable error.

---

## Phase 7: Polish and Cross-Cutting Concerns

**Purpose**: Final content quality review, mandatory CHANGELOG update (constitution Principle IV), and
end-to-end validation.

- [x] T025 [P] Review all documentation pages for content standards compliance per overlay-template-contract.md §Content Standards: user-focused language (no internal class names, file paths, or namespace references in user-facing content), unique SEO titles on each overlay `index.md` (FR-014), semantic heading structure on every page (H1 → H2 → H3, no skipped levels) (FR-015)
- [x] T028 [P] Follow `quickstart.md` contributor procedures end-to-end — run one-time setup (`pip install -r scripts/docs/requirements.txt`), `mkdocs serve` (confirm live preview), `mkdocs build --strict` (confirm passes); update `quickstart.md` if any step is inaccurate
- [x] T029 [P] Create `docs/404.md` — custom 404 page with H1 "Page Not Found", a brief message explaining the page may have moved or been removed, and a direct link back to the App Gallery homepage (`/`); MkDocs copies this file to `site/404.html` at build time and GitHub Pages serves it for unresolved URLs (covers edge case EC-1: user arrives at deep-link for removed/renamed overlay)
- [x] T026 Update `CHANGELOG.md` under `[Unreleased]` — add user-facing entry: "Added documentation hub at docs.opendashoverlays.com with App Gallery, WheelOverlay documentation section, Common Setup shared prerequisites, and contributor guide" (mandatory per constitution Principle IV and plan.md constitution check gate)
- [x] T027 Run final `mkdocs build --strict --verbose` — confirm zero errors and zero warnings; verify: App Gallery renders WheelOverlay card, validate_structure.py confirms WheelOverlay five-page structure, Common Setup and Contribute entries are present in nav but excluded from structure validation, all internal links across all pages resolve

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user story content validation**
- **US1 (Phase 3)**: Depends on Phase 2 completion — independently shippable after this phase alone
- **US2 (Phase 4)**: Depends on Phase 3 (search needs indexed content); can run in parallel with US3/US4 after Phase 3
- **US3 (Phase 5)**: Depends on Phase 3 (installation.md must exist to add Common Setup links); can run in parallel with US2/US4 after Phase 3
- **US4 (Phase 6)**: Depends on Phase 2 (validate_structure.py must exist to test error output); can run in parallel with US2/US3 after Phase 3
- **Polish (Phase 7)**: Depends on all user story phases complete

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational phase — primary deliverable; all other stories build on its content and infrastructure
- **US2 (P2)**: Depends on US1 content existing for search to index; configuration started in Phase 1/2
- **US3 (P3)**: Depends on US1 `installation.md` existing to verify Common Setup link compliance
- **US4 (P4)**: Depends on Phase 2 hook existing to verify error message format; Contribute nav entry added in T023 (not T014)

### Within Each User Story

- WheelOverlay content pages (T009–T013) are all [P] and can be written simultaneously
- Nav update (T014) must follow content creation (T009–T013)
- Legacy file deletion (T015) must follow content migration (T010–T013)
- Build validation is always the last task in each story checkpoint

### Parallel Opportunities

- Phase 1: T003 [P] can run alongside T001 and T002
- Phase 2: T005 [P] and T006 [P] can run in parallel with each other; T004 and T007 are independent
- Phase 3: T009, T010, T011, T012, T013 all [P] — five WheelOverlay pages simultaneously
- After Phase 3: US2, US3, and US4 phases can all begin in parallel

---

## Parallel Example: User Story 1 (WheelOverlay Content Pages)

```bash
# All five WheelOverlay content pages can be written in parallel:
T009: docs/wheel-overlay/index.md         (Overview — front-matter + content)
T010: docs/wheel-overlay/requirements.md  (migrate from getting-started.md §Prerequisites)
T011: docs/wheel-overlay/installation.md  (migrate from getting-started.md §Installation)
T012: docs/wheel-overlay/configuration.md (migrate from usage-guide.md)
T013: docs/wheel-overlay/troubleshooting.md (migrate from troubleshooting.md + tips.md)

# Then sequentially:
T014: mkdocs.yml nav update (depends on above pages existing)
T015: Delete legacy files (depends on content migrated into T010–T013)
T016: mkdocs build --strict validation
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T007) — CRITICAL, blocks all story work
3. Complete Phase 3: User Story 1 (T008–T016)
4. **STOP and VALIDATE**: `mkdocs build --strict` passes; WheelOverlay docs fully browsable at `mkdocs serve`
5. This alone is a shippable documentation hub at `docs.opendashoverlays.com`

### Incremental Delivery

1. Setup + Foundational → build pipeline and hooks ready
2. US1 → WheelOverlay docs live, App Gallery visible → deploy to GitHub Pages (MVP)
3. US2 → confirm search works end-to-end across all sections
4. US3 → Common Setup live, shared prerequisites documented once
5. US4 → Contribution guide live, developer onboarding self-serve
6. Polish → CHANGELOG updated, final end-to-end `mkdocs build --strict --verbose` passes

### Parallel Team Strategy

With multiple contributors after Phase 2 completes:
- Developer A: US1 — WheelOverlay content pages (T008–T016)
- Once US1 is done: Developer B handles US2+US3 in sequence; Developer C handles US4

---

## Notes

- No C# / .NET code changes in this feature — all tasks are Markdown, Python, YAML, and GitHub Actions
- `mkdocs build --strict` is the integration test — run it at the end of every phase
- `validate_structure.py` excludes Home, Common Setup, and Contribute from the five-page check — these sections are structural, not overlay apps
- App Gallery auto-populates from the filesystem — no extra config required when a new overlay adds its five pages plus a nav entry
- Legacy files (`getting-started.md`, `usage-guide.md`, `tips.md`) must be deleted after content migration (T015) to avoid stale content and broken nav references
- GitHub repository settings (Pages source → GitHub Actions, custom domain, Enforce HTTPS) are one-time setup required outside this feature's tasks — see CI/CD workflow contract §GitHub Repository Settings
