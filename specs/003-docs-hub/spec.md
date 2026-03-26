# Feature Specification: OpenDash Overlays Documentation Hub

**Spec Folder**: `specs/003-docs-hub/` | **Branch**: `docs/docs-hub` *(secondary format — cross-cutting docs, no version target)*
**Created**: 2026-03-25
**Status**: Draft
**Input**: User description: "Generate a PRD for the OpenDash Overlays Documentation Hub. A centralized portal at docs.opendashoverlays.com — hub and spoke architecture, standardized onboarding template, shared resources, global search, App Gallery, MkDocs-Material with GitHub Pages, custom CNAME, Quick Start under 5 mins, SEO optimization, scalable folder-based structure."

## Clarifications

### Session 2026-03-25

- Q: When an overlay is marked as deprecated, how should it appear in the App Gallery and its documentation section? → A: Visible with a deprecation badge in the App Gallery and a prominent notice on all its pages.
- Q: How does a contributor trigger deprecated status for an overlay? → A: Front-matter field (`deprecated: true`) in `docs/{app-name}/index.md`; build pipeline reads it and applies badge and notices.
- Q: Should the documentation site collect any page-view analytics or usage data? → A: No analytics — pure static site, no tracking scripts.
- Q: Should the site support multiple versions of an overlay's docs (e.g., v0.7 and v0.8 tabs)? → A: No — single current version only; old versions not archived on the site.
- Q: What is the expected availability requirement for docs.opendashoverlays.com? → A: Best-effort — availability matches GitHub Pages' own reliability; no custom SLA.
- Q: When adding a new overlay's docs, what is the maximum number of config files outside docs/{app-name}/ that a contributor may touch? → A: One — mkdocs.yml nav update only.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - First-Time User Finds and Installs an Overlay (Priority: P1)

A sim racer hears about "WheelOverlay" and visits the documentation site for the first
time. They land on the homepage, spot the overlay in the App Gallery, click through to
its dedicated section, and follow the Quick Start guide to get the overlay running —
all within five minutes.

**Why this priority**: This is the single most common and highest-value journey. If a
new user cannot discover and self-serve installation without assistance, every other
feature is irrelevant. Completing this story alone delivers a shippable documentation
site.

**Independent Test**: Can be fully tested by opening the live site in an incognito
browser, locating an overlay in the App Gallery, navigating to its Quick Start page,
and verifying that all steps are present, unambiguous, and completable without external
research.

**Acceptance Scenarios**:

1. **Given** a user arrives at `docs.opendashoverlays.com`, **When** they view the
   homepage, **Then** they see a visual App Gallery listing every published overlay
   (active and deprecated) with a name, brief description, deprecation badge where
   applicable, and a direct link to its documentation.
2. **Given** a user clicks an overlay in the App Gallery, **When** the page loads,
   **Then** they arrive at that overlay's Overview page, with clear navigation to
   Requirements, Installation, Configuration, and Troubleshooting sections.
3. **Given** a user follows the Installation section for any overlay, **When** they
   complete all listed steps, **Then** the overlay is fully operational on their system
   with no additional research required.
4. **Given** a new user with no prior knowledge, **When** they follow the Quick Start
   path end-to-end, **Then** the elapsed time from first page load to a working overlay
   is five minutes or less.

---

### User Story 2 - Returning User Searches Across All Documentation (Priority: P2)

A user who has already installed one overlay wants to find a specific configuration
option or troubleshooting answer. They use the site-wide search to find the relevant
page without knowing which overlay section it lives in.

**Why this priority**: Search is the primary way power users and returning visitors
navigate documentation. Without it, users must guess which overlay section contains the
answer, creating friction and support requests.

**Independent Test**: Can be fully tested by entering a known keyword (e.g., the name
of a configuration key) into the search field and verifying that the correct page
appears in results within two seconds.

**Acceptance Scenarios**:

1. **Given** a user types a keyword into the search field, **When** results appear,
   **Then** all matching pages across all overlay sections (including deprecated ones)
   are included in the results.
2. **Given** a user clicks a search result, **When** the target page loads, **Then**
   they land directly on the relevant section, not just the page top.
3. **Given** a search query that matches nothing, **When** results are displayed, **Then**
   the user sees a clear "no results" message and a link back to the homepage.

---

### User Story 3 - User Completes a Shared Prerequisite Once (Priority: P3)

A user installs a second overlay and discovers it requires a dependency (e.g., a
specific OBS configuration) that they already set up for the first overlay. The
documentation makes it clear that the shared step only needs to be done once and links
them directly to the Common Setup section.

**Why this priority**: Without shared resources, users who install multiple overlays
repeat identical setup steps and may apply conflicting configurations. Centralizing
prerequisites reduces support tickets and improves user confidence.

**Independent Test**: Can be fully tested by navigating from any overlay's Installation
page to the Common Setup section and confirming that all globally required steps are
documented there, with no duplication in the per-overlay sections.

**Acceptance Scenarios**:

1. **Given** a user is on any overlay's Installation page, **When** a shared
   prerequisite applies, **Then** the page links to the Common Setup section rather
   than duplicating the steps inline.
2. **Given** a user visits Common Setup, **When** they review the content, **Then** all
   global dependencies shared across two or more overlays are documented there with
   step-by-step instructions.
3. **Given** a developer adds a new overlay that reuses an existing shared dependency,
   **When** they follow the documentation contribution guide, **Then** they are directed
   to link to Common Setup rather than copy its content.

---

### User Story 4 - Developer Adds a New Overlay's Documentation (Priority: P4)

A developer creates a new overlay app (e.g., `discord-chat`) and needs to publish its
documentation to the hub. They add a folder under `docs/discord-chat/`, fill in the
standardized template, update the site navigation, and the new overlay appears in the
App Gallery automatically after the next deployment.

**Why this priority**: The scalability requirement depends entirely on how easy it is
to add new overlays. If adding documentation requires editing multiple configuration
files or manual coordination, the hub will fall behind new releases.

**Independent Test**: Can be fully tested by adding a new `docs/{app-name}/` folder
with the required template files, triggering a local build, and confirming the new
overlay appears in the gallery and navigation without any other configuration changes.

**Acceptance Scenarios**:

1. **Given** a developer creates `docs/{new-app}/` with the required template pages,
   **When** the site is built, **Then** the new overlay appears in the App Gallery on
   the homepage.
2. **Given** the required template pages are missing or incomplete, **When** the site
   build runs, **Then** the build fails with a clear error identifying the missing
   content — it does not silently publish an incomplete section.

---

### Edge Cases

- A user arrives at a deep-link URL for an overlay that has been removed or renamed —
  GitHub Pages serves its default 404 page; no redirect or custom recovery path is
  required. The custom `docs/404.md` page provides a link back to the App Gallery.
- A user's browser does not support JavaScript — all navigation and core page content
  remain accessible (search may degrade gracefully to a static fallback).
- An overlay has no configuration options — the Configuration page is still present and
  explicitly states that no configuration is required, rather than being absent.
- Two overlays share a dependency that has version-specific differences — Common Setup
  documents the version requirements clearly and links each overlay's page to the
  relevant subsection.
- A new developer submits a documentation PR missing a required template section — the
  CI build rejects it with a specific error identifying the missing section.
- A deprecated overlay's pages remain fully accessible and searchable; only the
  deprecation badge and notice distinguish them from active overlays.

## Requirements *(mandatory)*

### Functional Requirements

**Hub Architecture**

- **FR-001**: The site MUST present a homepage that functions as an App Gallery — a
  visual directory listing every overlay (active and deprecated) with its name, a
  one-sentence description, a deprecation badge where applicable, and a direct link to
  its documentation section.
- **FR-002**: Each overlay MUST have its own dedicated documentation section, accessible
  from a URL path under the main documentation domain (e.g., `docs.opendashoverlays.com/{app-name}/`).
  Slug and path changes are permitted; no redirects are required. Only the root domain
  `docs.opendashoverlays.com` must remain stable.
- **FR-003**: Every overlay documentation section MUST contain exactly the following
  pages, in this order *(the Required Template Page List — normative source for FR-010)*:
  1. `index.md` — Overview
  2. `requirements.md` — Requirements
  3. `installation.md` — Installation
  4. `configuration.md` — Configuration
  5. `troubleshooting.md` — Troubleshooting

  No page may be omitted; if a page's content is inapplicable, it MUST explicitly state so.
- **FR-016**: Deprecated overlays MUST display a prominent deprecation notice on every
  page of their documentation section. Deprecated status MUST be declared via a front-matter
  field in the overlay app's `docs/{app-name}/index.md` file (e.g., `deprecated: true`);
  the build pipeline reads this field to apply the badge and notices site-wide for
  that overlay. Deprecated overlays may be permanently removed from the site at the
  repo owner's discretion; no URL redirect or preservation requirement applies.

**Shared Resources**

- **FR-004**: The site MUST include a "Common Setup" section containing all
  dependencies and configuration steps shared across two or more overlays.
- **FR-005**: Per-overlay Installation pages MUST link to Common Setup for shared
  prerequisites rather than duplicating the content.

**Discovery and Navigation**

- **FR-006**: The site MUST provide full-text search across all documentation pages,
  including pages in all overlay sections (active and deprecated) and the Common Setup
  section.
- **FR-007**: Search results MUST be returned within two seconds of a query submission
  under normal load.
- **FR-008**: Navigation MUST allow users to reach any page within three clicks from
  the homepage.

**Scalability**

- **FR-009**: Adding a new overlay's documentation MUST require only creating a folder
  under `docs/{app-name}/` with the standard template files and updating exactly one
  external configuration file (`mkdocs.yml` nav) — no other source files may require
  modification. The App Gallery populates automatically from the filesystem (any
  `docs/{app-name}/index.md` present at build time); the `mkdocs.yml` nav entry is
  required for site navigation and is the one permitted external change.
- **FR-010**: The documentation system MUST validate at build time that every overlay
  section includes all five pages defined in the Required Template Page List (FR-003),
  and MUST fail the build with an error naming the missing page and overlay if any are
  missing.

**Publishing and Deployment**

- **FR-011**: The site MUST be accessible at the custom domain `docs.opendashoverlays.com`
  with a valid HTTPS certificate.
- **FR-012**: The site MUST be automatically rebuilt and redeployed whenever
  documentation source files are updated on the main branch — with no manual
  publish steps required.
- **FR-013**: The build process MUST fail visibly (blocking deployment) if any
  internal link on any page is broken.

**Contributor Workflow**

- **FR-017**: The site MUST include a contribution guide page documenting how to: add a
  new overlay section (folder structure, required template pages, `mkdocs.yml` nav
  update), link to Common Setup for shared prerequisites, and mark an overlay as
  deprecated using the `deprecated: true` front-matter field in `index.md`.
- **FR-018**: Documentation is mandatory for every overlay app published to the hub.
  Every feature release or breaking change to an overlay MUST include a corresponding
  documentation review; the documentation section for that overlay MUST be updated
  before or alongside the release to reflect the current behaviour.

**SEO**

- **FR-014**: Each overlay's Overview page MUST carry a unique page title and
  description that includes the overlay's name, making it independently discoverable
  via web search.
- **FR-015**: The site MUST use semantic heading structure on every page to support
  search engine indexing and screen readers.

### Key Entities

- **Overlay App**: A single published overlay (e.g., `wheel-overlay`). Has a name,
  short description, status (active/deprecated), and a set of documentation pages
  following the standard template. Deprecated overlays display a deprecation badge
  and notice on every page while present; they may be permanently removed from the
  site at the repo owner's discretion.
- **Documentation Section**: The complete set of pages for one overlay app — Overview,
  Requirements, Installation, Configuration, Troubleshooting.
- **Common Setup**: A shared section containing prerequisites and configuration steps
  that apply to multiple overlays. Not owned by any single overlay.
- **App Gallery**: The homepage directory of all overlay apps (active and deprecated).
  Derived automatically from the set of published overlay documentation sections.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time user with no prior knowledge can discover an overlay in the
  App Gallery, navigate to its documentation, and complete the full installation process
  in five minutes or less.
- **SC-002**: *(Qualitative design intent — not measurable without analytics, which are explicitly
  prohibited by spec assumption.)* The Quick Start path for every overlay is self-sufficient: all
  required steps, prerequisites, and common pitfalls are documented on-site so that users are not
  forced to seek external help to complete installation.
- **SC-003**: Adding a complete documentation section for a new overlay requires
  changes to exactly one file outside the new `docs/{app-name}/` folder — the
  `mkdocs.yml` nav entry.
- **SC-004**: Site-wide search returns relevant results for any documented feature or
  configuration option within two seconds.
- **SC-005**: Every overlay's Overview page appears in web search results for queries
  containing the overlay's exact name within 30 days of initial publication.
- **SC-006**: A broken internal link or missing required template page causes the
  automated build to fail before any changes reach the live site — zero broken links
  on the published site at any time.
- **SC-007**: The live site loads its first page in under two seconds on a standard
  broadband connection, ensuring the documentation does not itself become a barrier
  to getting started.

## Assumptions

- The initial launch covers `wheel-overlay` as the only overlay section; the Common
  Setup section and hub architecture must be ready to accommodate additional overlays
  from day one.
- "Common Setup" content scope (which shared prerequisites to document) will be
  determined during the planning phase based on the actual dependencies of current and
  near-future overlays.
- Search is provided by MkDocs-Material's built-in search plugin (client-side,
  index generated at build time); no server-side search infrastructure is required.
- The App Gallery is generated by scanning the `docs/` directory at build time —
  any subdirectory containing `index.md` (excluding structural sections) is
  automatically included. The `mkdocs.yml` nav entry is the authoritative registry
  for site navigation, not for gallery inclusion.
- GitHub Pages with a CNAME file provides the `docs.opendashoverlays.com` custom
  domain; DNS configuration is managed outside this feature's scope.
- No authentication or access control is required — the documentation site is fully
  public.
- No analytics or tracking scripts are included. The site MUST NOT embed any
  third-party analytics, telemetry, or cookie-setting scripts. No consent banner is
  required.
- Documentation is single-version only — each overlay's section reflects the current
  release. Historical versions of docs are not archived or served on the site. The
  site structure MUST NOT preclude adding versioning in a future iteration.
- Availability is best-effort, inheriting GitHub Pages' reliability. No custom uptime
  target, monitoring, or incident response process is in scope.
