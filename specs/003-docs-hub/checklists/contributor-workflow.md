# Contributor Workflow Checklist: OpenDash Overlays Documentation Hub

**Purpose**: Validate that requirements governing how contributors add, update, and deprecate overlay documentation are complete, clear, consistent, and measurable — before planning begins.
**Created**: 2026-03-25
**Feature**: [spec.md](../spec.md)
**Depth**: Lightweight (pre-plan self-review)
**Focus**: Contributor workflow — adding new overlays, build validation, deprecation handling

## Contributor Entry Point

- [x] CHK001 - Is the "documentation contribution guide" referenced in Stories 3 and 4 defined as an explicit deliverable requirement, or only implied? [Gap, Spec §User Story 3, Story 4]
  > **PASS**: FR-017 explicitly requires "The site MUST include a contribution guide page" — it is a normative deliverable, not implied.
- [x] CHK002 - Are requirements defined for *where* the contribution guide lives (e.g., a page on the docs site itself, a CONTRIBUTING.md, or a README)? [Completeness, Gap]
  > **PASS**: FR-017 specifies a "contribution guide page" (i.e., on the docs site); plan.md confirms `docs/contribute/index.md`.
- [x] CHK003 - Is there a requirement specifying that the contribution guide must be kept in sync with the template definition when the template changes? [Completeness, Gap]
  > **OUT OF SCOPE**: Guide-template sync is a code-review responsibility, not a spec requirement. With a single template and small contributor base, automated enforcement is not warranted. Intentionally excluded.

## Template Definition Requirements

- [x] CHK004 - Is the canonical list of required template pages (Overview, Requirements, Installation, Configuration, Troubleshooting) enumerated as a normative requirement, or only described narratively? [Clarity, Spec §FR-003, FR-009]
  > **PASS**: FR-003 enumerates all five pages with filenames and labels itself "*(the Required Template Page List — normative source for FR-010)*".
- [x] CHK005 - Are minimum content requirements defined for each template page (e.g., must have at least one heading, must not be empty), or is the presence of the file sufficient? [Completeness, Gap]
  > **PASS**: Spec + contract define the rule: file presence is sufficient for applicable pages; inapplicable pages MUST contain an explicit "not applicable" statement. The policy is clear.
- [x] CHK006 - Is placeholder/stub content explicitly permitted or prohibited for template pages at the time of initial PR merge? [Clarity, Gap]
  > **OUT OF SCOPE**: Placeholder content in applicable pages is explicitly permitted at initial merge. Content quality is a code-review concern, not a build-time requirement.
- [x] CHK007 - Is the required naming convention for `docs/{app-name}/` folder names specified (case, separator, character set)? [Clarity, Gap]
  > **PASS**: `contracts/overlay-template-contract.md` §Folder Naming specifies: all lowercase, hyphen-separated, no underscores, no camelCase, with examples.

## Scalability Constraint Precision

- [x] CHK008 - Does FR-009 explicitly identify `mkdocs.yml` as the one permitted external file, or does it leave "site navigation configuration" ambiguous enough to permit other interpretations? [Clarity, Spec §FR-009]
  > **PASS**: FR-009 names "`mkdocs.yml` nav" explicitly — no ambiguity.
- [x] CHK009 - Is the causal mechanism between adding a `mkdocs.yml` nav entry and the overlay appearing in the App Gallery stated as a requirement, or only assumed in the Assumptions section? [Consistency, Spec §FR-001, FR-009, Assumptions]
  > **PASS**: Plan Phase 0 Research documents the mechanism (gallery.py scans `docs/` filesystem; nav entry is for navigation). Spec Assumptions also documents the intent. Both sources are consistent.
- [x] CHK010 - Is SC-003 ("exactly one file outside docs/{app-name}/") objectively verifiable from the spec alone without running a build? [Measurability, Spec §SC-003]
  > **PASS**: SC-003 names the one file explicitly (`mkdocs.yml` nav entry). Verifiable from PR diff without running a build.
- [x] CHK011 - Are requirements consistent between Story 4's acceptance scenario 1 ("appears in App Gallery") and FR-009 ("no other source files require modification")? Is the gallery population mechanism treated as automatic or manual? [Consistency, Spec §FR-001, FR-009, User Story 4]
  > **PASS**: Consistent. Gallery auto-populates from filesystem (creating `docs/{app-name}/index.md` is sufficient); nav update is a separate requirement for navigation. US4 scenario 1 and FR-009 address different concerns.

## Build Validation Requirements

- [x] CHK012 - Is the error message format or minimum information content specified for FR-010 build failures (e.g., must name the missing page, must identify the overlay)? [Clarity, Spec §FR-010]
  > **PASS**: `contracts/overlay-template-contract.md` §Build Validation specifies the exact error message format, including `{overlay}` and `{page}` placeholders and the full five-page list.
- [x] CHK013 - Is "all required template pages" in FR-010 traceable to a single normative list, or does it rely on the reader cross-referencing FR-003? [Traceability, Spec §FR-010, FR-003]
  > **PASS**: FR-003 is explicitly labeled as the normative source for FR-010 with a parenthetical cross-reference inline.
- [x] CHK014 - Are requirements defined for local pre-push validation (i.e., must a contributor be able to run the same validation locally before opening a PR)? [Coverage, Gap]
  > **PASS**: `contracts/ci-cd-workflow-contract.md` §Local Equivalence documents `mkdocs build --strict` as the local equivalent and states "Developers MUST run this before opening a PR."
- [x] CHK015 - Is "deployment cycle" duration bounded in Story 4 acceptance scenario 2 ("live within one automated deployment cycle"), or is the cycle duration undefined? [Measurability, Spec §User Story 4]
  > **RESOLVED**: Acceptance scenario 2 deleted from spec. Documentation is mandatory for all overlays; when it is made available is not a gated requirement. FR-018 added: docs must be reviewed and kept current alongside each feature release.

## Deprecation Workflow Requirements

- [x] CHK016 - Is there a requirement specifying *how* a contributor marks an overlay as deprecated (e.g., a metadata field, a nav config flag, a front-matter property)? [Completeness, Gap]
  > **PASS**: Spec Clarifications, FR-016, and `contracts/overlay-template-contract.md` §Deprecation Protocol all specify `deprecated: true` in `docs/{app-name}/index.md` front-matter.
- [x] CHK017 - Is the deprecation action restricted to specific contributors (e.g., maintainers only) or open to any contributor? [Completeness, Gap]
  > **PASS**: Deprecation is a repo-owner action. Access control is enforced via GitHub branch protection (PR review required); no additional spec requirement needed.
- [x] CHK018 - Are requirements defined for what the deprecation badge must visually communicate (e.g., must include a reason, a date, or a replacement link)? [Clarity, Spec §FR-016]
  > **PASS**: Contract specifies: App Gallery shows "Deprecated" badge next to overlay title; all section pages show `!!! danger "This overlay is deprecated"` / "This overlay is no longer actively maintained." No date/reason/replacement link is required — this scope is intentional per the clarifications.
- [x] CHK019 - Is "prominent deprecation notice" in FR-016 quantified with placement or visibility criteria, or is "prominent" left subjective? [Clarity, Spec §FR-016]
  > **PASS**: Contract specifies placement as "the first content element on every page in the section" using a `!!! danger` admonition — objectively verifiable.

## Edge Cases in Contributor Workflow

- [x] CHK020 - Are requirements defined for renaming an existing overlay's documentation folder (URL stability, redirect requirements)? [Coverage, Gap]
  > **RESOLVED**: Only the root domain `docs.opendashoverlays.com` must remain stable. Slug and path changes are permitted; no redirects required. FR-002 updated accordingly.
- [x] CHK021 - Is there a requirement addressing what happens when two contributors simultaneously add different overlays in separate PRs (merge conflict in `mkdocs.yml`)? [Coverage, Edge Case, Gap]
  > **OUT OF SCOPE**: Handled by manual process — the more important PR merges first; the second contributor rebases and resolves the trivial `mkdocs.yml` nav conflict. No spec requirement needed at current scale.
- [x] CHK022 - Are requirements defined for removing a deprecated overlay's documentation entirely (if ever permitted), including URL handling? [Coverage, Gap]
  > **RESOLVED**: Permanent removal is permitted at repo owner's discretion. No URL preservation or redirect required. FR-016 updated to remove the HTTP 200/stable URL constraint and explicitly permit removal.

## Notes

- Check items off as completed: `[x]`
- Items marked `[Gap]` indicate requirements not yet present in the spec — add them or explicitly document the omission as out of scope.
- Items marked `[Clarity]` indicate existing requirements that may need tighter wording before planning.
- Items marked `[Measurability]` indicate success criteria or acceptance scenarios that need a quantifiable threshold to be testable.
