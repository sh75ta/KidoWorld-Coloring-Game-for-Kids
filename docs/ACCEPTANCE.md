# Acceptance checklist

> **First Play implementation update:** See [BUILD_STATUS.md](BUILD_STATUS.md) and the root README for current source/artwork status and actual validation results. This earlier planning baseline remains for traceability.

All checks are currently **not run**, except documentation/catalog validation described in the handoff. Passing these requires the actual Unity game and, where specified, physical devices/store sandboxes.

## M0 — Environment

- [ ] Exact installed Unity 6.3 patch recorded; matches project version file.
- [ ] Required Android and iOS build components confirmed.
- [ ] Project imports with no compile errors.
- [ ] Empty Android build installs on a test device.
- [ ] iOS export builds/signs in Xcode and installs on a test device.
- [ ] Git excludes caches, build outputs, credentials, and signing keys.

## Coloring and interaction

- [ ] Tapping inside a region fills only that region.
- [ ] Taps on outlines/background are handled predictably without paint leaks.
- [ ] Slow/fast brush movement produces continuous strokes.
- [ ] Brush start region remains correct when crossing a boundary.
- [ ] Eraser removes paint without damaging outlines.
- [ ] Glitter, pattern, and rainbow behavior survives save/reload.
- [ ] Tool and palette taps do not accidentally paint underneath the UI.
- [ ] Multitouch, canceled gestures, interruptions, and touches outside the canvas are handled safely.
- [ ] Undo reverses one full action, including an entire stroke.
- [ ] Reset Cancel changes nothing; Confirm clears only the selected canvas.
- [ ] Done saves partial artwork and returns to the correct menu location.
- [ ] Feedback is responsive, bounded, and does not obstruct coloring.

## Persistence and recovery

- [ ] Partially color picture A, return to menu, reopen: identical saved paint.
- [ ] Color picture B; picture A remains unchanged.
- [ ] Fully quit and relaunch: both canvases restore correctly.
- [ ] Background/resume during and after a stroke: no corrupted state.
- [ ] Force termination during a save: last committed state is recoverable; no silent blank reset.
- [ ] Simulate malformed/truncated save: backup recovery or clear recoverable error.
- [ ] Simulate low storage/write failure: existing save remains intact.
- [ ] Reset and restart: reset persists and does not affect another picture.
- [ ] Menu thumbnail/status matches the stored painting.
- [ ] Schema/art revision changes follow a tested migration or preserve the previous artwork safely.

## Menu and layout

- [ ] Exactly 30 entries, 10 per category, with stable unique IDs.
- [ ] Two rows on phone and tablet layouts.
- [ ] All tabs land on the intended category.
- [ ] Horizontal scrolling traverses every category continuously.
- [ ] Active tab and background stay synchronized without flicker at boundaries.
- [ ] Free/paid state is accurate and understandable.
- [ ] Safe areas and both landscape directions, if approved, preserve controls.
- [ ] Text/icons remain legible and touch targets usable on the smallest supported phone.

## Purchases and ads

- [ ] Final free IDs, product IDs, prices, placements, frequency, and SDK versions recorded.
- [ ] Purchase, support, and external navigation use the agreed parent flow.
- [ ] Store sandbox purchase unlocks all 30; cancellation does not.
- [ ] Pending purchase, interrupted purchase, retry, refund/revocation, and offline state behave as specified.
- [ ] Restore on same-store/account reinstall or second supported device works.
- [ ] Restore never claims to transfer ownership between Apple and Google accounts.
- [ ] Full-access users receive no ad requests/displays according to the approved SDK lifecycle.
- [ ] Ads never cover painting or trigger accidental child taps.
- [ ] Ad network unavailable/offline: free content remains usable.
- [ ] Actual SDK data flows, privacy disclosures, child settings, creative review, and store policies validated.

## Release quality

- [ ] All 30 production pictures reviewed, with valid closed regions and no unintended fill connections.
- [ ] No placeholder artwork or nonfunctional commerce controls remain.
- [ ] Minimum device/OS matrix is agreed and tested on both platforms.
- [ ] Frame times, brush latency, memory, load time, and long-session stability measured on that matrix.
- [ ] Sound/VFX settings and behavior match the final specification.
- [ ] Parent-child testing confirms discoverability, recoverability, and understandable feedback.
- [ ] Store age/target-audience declarations, privacy policy, support details, screenshots, and review notes reflect the actual app.

