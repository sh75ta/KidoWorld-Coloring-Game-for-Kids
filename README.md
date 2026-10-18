# Kido Coloring — First Play

An initial implementation of the 30-picture coloring game, with Unity source and a separate self-contained browser review build. Updated 18 October 2026.

**This is a prototype, not an App Store/Google Play release.** The Unity source has not been compiled or run in this environment. The browser painting model passed automated tests; browser layout, IndexedDB integration, native builds, and physical-device behavior still require execution testing.

## Try the visual prototype

Open `Preview/Kido-Coloring-Preview.html` in a current browser with IndexedDB enabled. It contains all its artwork and scripts and makes no external network requests. All 30 pictures are available for review. If your browser blocks storage for downloaded files, serve the project locally (`python3 -m http.server 8000`) and open `http://localhost:8000/Preview/Kido-Coloring-Preview.html`.

- Choose a category or swipe through the continuous two-row gallery.
- Open a picture, choose a color, and use Tap fill, Brush, Eraser, Glitter, Pattern, or Rainbow.
- Undo reverses one complete action. Reset asks first and can itself be undone.
- Done saves and returns to the menu. Drawings are local to that browser/device/origin.
- The Parents button opens a prototype parent gate and sound/celebration settings.

The browser version is a design/play review companion. It is **not** an exported Unity build and does not verify iOS/Android behavior. Clearing browser data or opening a different origin loses access to those local saves; no cross-device synchronization is implemented.

## Open the Unity project

1. Extract this entire folder, including `Assets`, `Packages`, and `ProjectSettings`.
2. In Unity Hub, add the `KidoColoring` folder. Select an installed Unity **6.3** Editor.
3. The source baseline records `6000.3.0f1`; this is an explicit target, **not a claim about your installed Editor**. Use the appropriate supported 6.3 patch after checking your installed modules. Review and commit version/package changes made by that Editor.
4. Let Package Manager resolve the manifest. No fabricated package lock is included.
5. Run **Kido Coloring → 1. Verify Editor and modules**. Record the console output.
6. Run **Kido Coloring → 2. Configure and open game**. This sets landscape defaults, uses the legacy input backend, reimports readable artwork, and opens `KidoGame.unity`. Restart the Editor if it requests an input-backend restart.
7. Press **Play** in the Game view. Start with a landscape phone/tablet aspect ratio.
8. Open **Window → General → Test Runner**, choose EditMode, and run `Kido.Tests`.

The Android/iOS menu commands create development output only, if the appropriate build modules exist. iOS export still requires Xcode/macOS and signing to produce an installed app. Store signing, real bundle identifiers, and minimum OS/device support are not configured for release.

## Implemented in source

| Area | Current implementation |
| --- | --- |
| Content | 30 original editable SVG illustrations, PNGs, thumbnails, and closed-region maps |
| Menu | Three tabs; continuous 15-column × 2-row discovery; per-picture previews/progress |
| Coloring | Tap fill, region-constrained brush, eraser, glitter, pattern, rainbow; 12 colors |
| Actions | Per-action undo, confirmed/undoable reset, Done and return |
| Feedback | Animated splash indicator, button feedback, bounded confetti, generated soft tones |
| Persistence | Per-canvas painting; checksummed alternating native save slots; recovery from one corrupt slot |
| Parent controls | Prototype literacy gate; sound and celebration settings |
| Mobile layout | Landscape canvas-left/tools-right layout, safe-area handling, scalable UI |
| Commerce boundary | Configurable access policy; preview opens all 30; no live ad or purchase SDK |

Artwork is **first-pass vector art**, simpler than the earlier dinosaur raster sample. Editable sources and the sample reference are included. No claim of final art approval is made. The completion marker currently appears at 90% paintable coverage; this is configurable and remains a product decision.

## Validation actually performed

- Seven Node tests passed against the **browser painting engine** and all 30 real region maps.
- JavaScript syntax checked.
- Asset counts, catalog references, region-map dimensions/labels, artwork rendering, Unity metadata, and repository structure checked locally.
- All 30 vector illustrations inspected together in a contact sheet.

**Not performed:** Unity compilation/EditMode tests, rendered browser UI automation (browser executable absent), real browser save/reload, native build/signing, physical-device tests, live store purchases, or ads. Twelve Unity EditMode tests are included for the next environment. See [build status](docs/BUILD_STATUS.md) for exact limitations.

## Still required before release

Real device/UI/performance QA; approved final art and interaction tuning; actual free IDs/prices; store purchase/restore integration and entitlement validation; eligible advertising provider/placements/frequency; final parent gate/privacy/disclosures; release bundle IDs/signing; and store submission materials. Non-development Unity builds are deliberately blocked by `KidoReleaseGuard` until this work is complete.

## Project map

- `Assets/KidoColoring/Runtime` — C# game, UI, paint, save, access, feedback.
- `Assets/KidoColoring/Editor` — texture importer, setup/build menus, release guard.
- `Assets/KidoColoring/Tests/Editor` — native test suite (not yet executed).
- `Assets/KidoColoring/Resources/Kido` — all runtime assets/configuration.
- `ArtSource` — 30 original SVGs.
- `Preview` — self-contained HTML, its template and tested painting engine.
- `tools` — reproducible art/preview pipeline and Node tests.
- `docs` — scope, implementation notes, validation and remaining work.
- `references` — original wireframe and earlier dinosaur style sample.

Run browser engine checks with `node --test tools/test_preview.cjs`. Rebuilding artwork requires Python with NumPy, SciPy, Pillow plus Node with sharp; these are authoring dependencies only. Game assets are already generated. Neither Unity nor the standalone HTML needs Python/Node to play.

This local Git repository has no remote. It contains no store credentials, signing keys, or third-party ad/analytics SDK. Keep production signing material outside the repository.
