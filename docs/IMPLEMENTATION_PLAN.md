# Implementation plan

> **First Play implementation update:** See [BUILD_STATUS.md](BUILD_STATUS.md) and the root README for current source/artwork status and actual validation results. This earlier planning baseline remains for traceability.

All entries below describe proposed work. They are not claims that these systems have been implemented or tested.

## Milestones

| Milestone | Deliverable | Exit evidence |
| --- | --- | --- |
| M0 — Environment | Verified Unity patch, modules, local Git, initial project | Editor opens; empty mobile build/export succeeds |
| M1 — Coloring and save | One prepared dinosaur canvas; fill, solid brush, eraser, palette, undo, reset, Done | Saved paint survives reopening, restart, and background/resume |
| M2 — Discovery | Three themed sections, 30 entries, two rows, category tabs, previews | Tabs and continuous swipe agree; correct preview/state opens |
| M3 — Tools and feedback | Six reference tools, splash, responsive animation/VFX/SFX | Effects never block drawing; performance measured on devices |
| M4 — Content and commerce | All 30 production canvases; agreed free set; full-access purchase/restore; approved ad integration | Paid users see all content without ads; restore and failure cases pass |
| M5 — Release | Device QA, store metadata/disclosures, signed builds | Acceptance tests pass on the agreed device matrix |

Do not make a store-launch schedule until M0/M1 provide real build and device evidence. The project scope remains 30 canvases; the one-canvas milestone validates the costly drawing/save behavior first.

## Proposed screen structure

Splash → category menu → selected canvas → Done → menu at remembered scroll position.

Reset opens a confirmation overlay; Cancel preserves the artwork. Parent area handles purchases, restore, support, and optional settings. Empty/background taps must not trigger purchases or destructive actions.

### Continuous category navigation

Use one horizontal catalog with **15 columns × 2 rows**, grouped into three sections of five columns each. Fill each category column top-to-bottom, then move right. The global index order stays stable.

- A top tab scrolls directly to the corresponding section.
- Swiping across a boundary continues into the next category, with no forced stop or loop.
- The active tab follows the category at the viewport's center, with hysteresis near boundaries to prevent flicker.
- Background transitions follow that same category. During scrolling, the menu remains interactive.
- Save selected canvas ID and scroll position separately from paint data.
- Scale the number of visible columns with screen width; keep two rows and usable touch targets.
- Use short visible labels Christmas / Animals / Vehicles, with icons; preserve full category names in the catalog.

This resolves the apparent tension between category tabs and cross-category scrolling without creating two competing lists.

### Responsive coloring layout

Follow the left canvas / right tool rail reference in landscape. Keep artwork aspect ratio intact. On narrow phones, simplify or scroll the tool rail instead of shrinking controls below usable sizes. Respect safe areas, camera cutouts, system gesture zones, and both landscape orientations. Confirm orientation policy before implementation.

## Proposed Unity systems

| System | Responsibility |
| --- | --- |
| App flow | Screen changes, loading state, navigation memory |
| Content catalog | Stable category/canvas IDs, ordering, asset references, configurable access |
| Coloring surface | Touch-to-canvas mapping, drawing, region fill, clipping, undo |
| Tool definitions | Fill, solid, eraser, glitter, pattern, rainbow parameters |
| Save service | Versioned per-canvas paint/state, previews, backup/recovery |
| Progress evaluator | Not started / in progress / completed after agreed completion rules |
| Feedback | UI animation, bounded effects, SFX and sound settings |
| Access service | Free selection and verified full-access entitlement |
| Purchase adapter | Store purchase, pending/cancel/failure, restore and revocation handling |
| Ad adapter | Agreed placements, eligibility, ad-free state, offline/error behavior |

Use Unity and C# with the Editor-compatible versions of required UI/input packages. Record actual package versions after M0. Do not install an advertising or analytics SDK merely because it ships with a template. Select the purchase and ad integrations after their data handling and platform behavior have been reviewed.

## Coloring asset contract

For each production canvas, prepare:

1. Crisp outline artwork with generous, closed regions.
2. Aligned region-ID map. Background is distinct; masks must preserve exact IDs during import.
3. Optional brushable-area masks and clearly specified region adjacency for complicated subjects.
4. Clean uncolored thumbnail and an artwork revision identifier.

Paint is a separate layer beneath outlines. Bucket fill changes the selected region; solid/eraser/texture tools modify paint without destroying line art. The starting region anchors a constrained stroke. UI touches must never also draw on the picture. Handle canceled touches and interruptions explicitly.

Glitter is stored as paint/effect data, pattern as a repeatable texture, and rainbow as reproducible stroke colors. Reopening artwork must reproduce the visible result; random effects cannot silently regenerate into a different picture.

The dinosaur raster sample is a style reference. It has not been transformed into validated masks or reviewed as approved production art. The 30 catalog records are asset briefs, not 30 finished illustrations.

## Persistence design

- Stable canvas IDs, never display names or array positions, key the save data.
- Store a schema version and artwork revision to support migrations.
- Save after completed strokes/fills with bounded coalescing, on Done, and on app pause/focus loss. Do not rely solely on app quit.
- Write new state to a temporary file, validate it, then commit with a platform-tested replacement/backup strategy. Recover the last valid save if a write is interrupted.
- Keep an artwork snapshot plus metadata and a derived thumbnail. A recoverable journal of actions since the snapshot can bound loss during forced termination; measure write costs before choosing the final format.
- Failed saves should preserve the previous valid file and retain/retry pending work. Never overwrite a recoverable save with a blank canvas on load failure.
- Undo is per action: a complete brush stroke is one action. Bound history by memory and measure its cost on devices.
- Reset removes paint for the current canvas only after confirmation, writes the new state, and updates its thumbnail/status. Decide whether Reset itself is undoable before implementation.
- Menu preview derives from saved paint; do not show an unrelated fully colored marketing thumbnail as the user's work.
- Done always saves partial work. It must not automatically imply a completed drawing until completion semantics are agreed.

Proposed completion rule to evaluate with children: expose simple states and let the child finish without requiring perfect pixel coverage. If automated completion is used, define/test region coverage and thresholds explicitly. Avoid presenting an arbitrary percentage as educational achievement.

Local app storage covers sessions/restarts. Reinstall recovery, cloud backup, and cross-device art sync are separate decisions. Store entitlement restoration does not restore local artwork.

## Commerce boundaries and remaining decisions

Use one non-consumable full-access product per store. Pricing comes from the store, not a hard-coded currency string. Keep entitlement state independent of the artwork saves.

- Finalize free IDs, price, provider, placements, frequency, and purchase UX before implementation.
- Parent-facing purchase and restore actions are distinct from child coloring controls.
- Successful verified purchase removes ad opportunities and unlocks all 30.
- Cancel/pending/failure never fabricate an entitlement or discard paint.
- Offline play uses previously valid local entitlement state according to the agreed verification policy.
- Restore is proposed within the same storefront/account on supported devices. An Apple purchase is not assumed to unlock Google Play automatically; cross-store access needs an explicit account/backend decision.
- Do not silently lock or delete a child's saved art when access changes; define access/revocation behavior separately.

### Advertising feasibility

Apple's Kids Category generally excludes third-party advertising; its limited contextual-ad exception requires documented Kids practices and human review of creative suitability. A generic child-directed flag is insufficient. Purchase/link opportunities belong behind a parental gate. [Apple App Review Guidelines, 1.3](https://developer.apple.com/app-store/review/guidelines/).

Google requires Families Self-Certified Ads SDKs for ads served to children, excludes interest-based advertising/remarketing, and sets specific format and dismissal restrictions. [Google Play Families policies](https://support.google.com/googleplay/android-developer/answer/9893335?hl=en).

These sources were checked on 18 October 2026. The required ad-supported model remains in scope, but neither platform's integration is approved merely by this document. If a proposed provider cannot satisfy both stores, choose a compliant platform-specific approach with the user before implementation. A parental gate alone does not establish advertising or privacy compliance. Review applicable US/UK obligations against actual data flows before release.

