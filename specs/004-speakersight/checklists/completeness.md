# Completeness Checklist: SpeakerSight

**Purpose**: Validate that spec.md is complete, internally consistent, and has no gaps before v0.1.0 ships
**Created**: 2026-04-21
**Feature**: [spec.md](../spec.md)

---

## Requirement Completeness — Missing FRs

- [ ] CHK001 Is there a functional requirement for single-instance enforcement? Tasks (T008) implement a Mutex guard but no FR documents this as a requirement or specifies the failure behavior when a second instance is launched. [Gap, Spec §FR-*]
- [ ] CHK002 Is the "Show on startup" setting covered by a functional requirement? `OverlaySettings` models a `ShowOnStartup` field and T027 wires it, but no FR specifies this setting exists, its default, or its semantics. [Gap]
- [x] CHK003 Is font size adjustment covered by a functional requirement? T010 and T029 implement a `FontSize` setting (range 8–32, default 14), but no FR or OverlaySettings field description in the spec documents this capability. [Gap] → **Resolved**: FR-016 and FR-016a added — font size applies to all text elements; overlay dimensions fixed to 8 rows at widest 32-char glyph width; settings panel shows 8 placeholder speakers as live preview.
- [ ] CHK004 Is there a requirement covering behavior when Windows Credential Manager is unavailable or access is denied (e.g., locked device, enterprise policy)? FR-002 specifies *where* to store tokens but not what the app must do when the store is inaccessible. [Gap, Spec §FR-002]
- [ ] CHK005 Is session-lock / screen-off behavior covered by a functional requirement? The edge cases section describes the expected behavior (pause rendering, resume on unlock) but no FR mandates it. [Gap, Spec §Edge Cases]
- [ ] CHK006 Is there an FR for system tray icon double-click behavior? T033 adds double-click toggle but no FR defines this interaction or its expected state transitions. [Gap]
- [ ] CHK007 Is uninstall behavior specified? No requirement covers what happens to `%APPDATA%\SpeakerSight\` files and Windows Credential Manager tokens when the MSI is uninstalled. [Gap, Spec §FR-015]

---

## Requirement Clarity — Ambiguous or Under-specified Requirements

- [x] CHK008 Is "first 8" in FR-006 defined with a deterministic ordering rule? FR-006 caps display at 8 active speakers but does not specify which 8 are shown when more than 8 are simultaneously active (e.g., most-recently-activated, longest-active, alphabetical). [Ambiguity, Spec §FR-006] → **Resolved**: FR-006 updated to "8 most-recently-activated speakers".
- [ ] CHK009 Is the reconnection time cap in SC-006 traceable to a corresponding FR? SC-006 states "reconnects automatically within 10 seconds of a recoverable IPC drop" but FR-010 specifies only exponential backoff without a maximum elapsed-time guarantee. [Ambiguity, Spec §FR-010, §SC-006]
- [ ] CHK010 Is the opacity animation frame rate specified in requirements? FR-004a defines the animation start condition and duration but leaves the update interval undefined. Tasks specify ~30 fps (33 ms ticks) but this implementation detail has no requirements backing. [Clarity, Spec §FR-004a]
- [ ] CHK011 Is the behavior of the `+N more` indicator specified when the overflow count changes during an active session (e.g., a speaker from the overflow group stops speaking)? FR-006 defines appearance but not the update semantics for the count when the overflow pool changes. [Clarity, Spec §FR-006]
- [ ] CHK012 Are avatar CDN URL format and caching behavior defined in requirements? FR-014b specifies which avatar hash to prefer but does not specify the CDN URL format, image size, or whether avatars should be cached locally to survive network outages. [Clarity, Spec §FR-014b]
- [ ] CHK013 Is the term "immediately" in FR-004b quantified? FR-004b states the overlay "MUST immediately restore full opacity" when a fading speaker resumes — but SC-003 sets the 500 ms threshold for *activation* from Discord event. Is the same 500 ms SLA implied for resumption, or is it a different target? [Ambiguity, Spec §FR-004b, §SC-003]
- [ ] CHK014 Is "clear re-authorization prompt" in User Story 1 acceptance scenario 4 defined with measurable criteria? The scenario states the app shows "a clear re-authorization prompt" but no FR specifies what this prompt must contain or how it is surfaced (overlay text, settings panel, tray notification). [Clarity, Spec §US1, §FR-010c]

---

## Requirement Consistency — Conflicts Between Sections

- [x] CHK015 Is the grace period range consistent across all spec sections? Clarifications (§2026-03-30) document the grace period as "range 0–10s" while FR-004a, OverlaySettings, and SC-003 all specify "range 0–2 seconds". If 0–2 is the authoritative range, the Clarifications entry should be updated to prevent implementation confusion. [Conflict, Spec §Clarifications, §FR-004a, §OverlaySettings] → **Resolved**: Clarifications updated to "range 0–2s".
- [x] CHK016 Is the color theme option set consistent between User Story 3 and the OverlaySettings entity? US3 describes "dark/light" (two options) while the `OverlaySettings` entity and T010 implement a three-way `Dark/Light/System` enum. If System is intentional, US3's description should reflect it. [Conflict, Spec §US3, §OverlaySettings] → **Resolved**: US3 updated to "dark/light/system".
- [ ] CHK017 Is the stale note in `checklists/requirements.md` ("Scope bounded to v0.1.0: single voice channel, display names only (no avatars)") consistent with the current spec? The spec has extensive avatar support (FR-014b, FR-014b-layout, SC-010). The checklist note is a documentation consistency issue. [Conflict, checklists/requirements.md §Notes]
- [ ] CHK018 Are the two acceptance scenarios numbered "6" in US6 (lines 118–119 and 120–121 of spec.md) intentional? Both scenarios carry the label `6.` — one covers emoji in custom names, the other covers custom name persistence after Discord name change. Duplicate numbering could cause reference ambiguity during review or testing. [Clarity, Spec §US6]

---

## Scenario Coverage — Missing or Incomplete Flows

- [ ] CHK019 Is there a requirement or acceptance scenario covering the transition from Retrying back to Connected mid-session? FR-010 and SC-006 address reconnection but no acceptance scenario in US1 defines the exact behavior: which subscriptions are re-established, whether the current voice session is restored, and how the overlay state is reconciled. [Coverage, Spec §US1, §FR-010]
- [ ] CHK020 Is there an acceptance scenario for the "all-members" display mode when the user joins a channel (initial population before any speaking event)? US2 scenarios address speaking start/stop but not how the member list is seeded in all-members mode on channel join. [Coverage, Spec §US2]
- [ ] CHK021 Are requirements defined for the overlay's behavior during the first-run authorization flow? No scenario specifies what the overlay shows (hidden, placeholder, or idle state) while the authorization dialog is pending in the Discord client. [Coverage, Gap, Spec §US1]
- [ ] CHK022 Is there a requirement for surfacing the "borderless windowed mode" constraint (FR-011a) to the user? FR-011a states exclusive fullscreen is unsupported but no requirement mandates a user-visible explanation, in-app tooltip, or documentation link when the overlay is not visible. [Coverage, Spec §FR-011a]
- [ ] CHK023 Are requirements defined for what happens when the MSI is upgraded over an existing installation? No FR addresses settings migration, Credential Manager token preservation, or alias file compatibility across versions. [Coverage, Gap, Spec §FR-015]
- [ ] CHK024 Is there an acceptance scenario for ChannelMember avatar updates? FR-014b specifies avatar resolution priority (guild > global) but no acceptance scenario validates that a member's avatar changes on the overlay when their guild avatar hash changes between sessions. [Coverage, Spec §FR-014b]

---

## Non-Functional Requirements — Coverage

- [ ] CHK025 Are accessibility requirements specified for the settings panel? No NFR or FR covers keyboard navigation, screen reader compatibility, or WCAG conformance targets for the SpeakerSight settings window. [Coverage, Gap]
- [ ] CHK026 Is a startup time requirement defined? SC-001 covers connection time but no requirement specifies the maximum time from application launch to a usable tray icon or overlay-ready state. [Gap]
- [ ] CHK027 Is installer size or dependency footprint specified? No NFR addresses the maximum MSI size, .NET runtime bundling policy, or prerequisites that users must have installed. [Gap]
- [ ] CHK028 Are security requirements defined for the bundled `client_id`? The clarification notes that `client_id` is a public identifier committed to the repo, but no requirement addresses obfuscation, rotation strategy, or what happens if the Discord application is compromised or deregistered. [Gap, Spec §Assumptions]

---

## Acceptance Criteria Quality

- [ ] CHK029 Can SC-007 (CPU < 2%, RAM < 100 MB) be objectively verified with a defined measurement methodology? The success criterion exists but no requirement specifies the measurement tool, duration, or baseline voice session configuration used to assess it, making it difficult to produce a repeatable result. [Measurability, Spec §SC-007]
- [ ] CHK030 Is the success criterion for alias resolution (SC-010) complete? SC-010 covers custom name display and avatar toggle but does not define a measurable criterion for name reversion speed when switching between contexts with and without custom names. [Measurability, Spec §SC-010]

---

## Notes

- Items CHK015 and CHK016 are the highest-priority findings — both are internal spec conflicts that could produce inconsistent implementations.
- CHK018 (duplicate scenario numbering) is a low-risk editorial issue but should be corrected before the spec is shared externally.
- CHK001, CHK002, CHK003 reflect implementation choices already made in tasks.md that lack corresponding FRs — these can be resolved by either adding FRs or explicitly calling them out as implementation decisions outside the spec's scope.
- Mark items `[x]` when the gap is resolved (either by updating the spec or explicitly accepting the gap with a documented rationale).
