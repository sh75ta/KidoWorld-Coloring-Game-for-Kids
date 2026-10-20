# Kido Coloring — approved-content rebuild

For continuation in a new local Codex session, read `AGENTS.md` and `HANDOFF.md` first.

Updated 20 October 2026. Unity **6000.3.14f1**. Branch `rebuild/approved-art-v2`.

**Latest:** [Menu and interaction update](docs/MENU_UPDATE_2026-10-20.md).
The new horizontally scrolling menu is a functional video-based draft with
embedded League Gothic. Exact Figma category mascots remain blocked by file
access, so readable text tabs are used pending the original exports. Native
compilation and phone/tablet visual validation have not run here.

This package contains the rebuilt Unity source, approved artwork, supplied original Figma UI, and the supplied 30-second Jingle Bells background track. Phone/tablet coloring layouts are implemented in source. The macOS Metal shader keyword errors reported during the first native run were corrected; the updated build still needs a clean Unity compile and visual confirmation.

## Open the current rebuild

1. Extract the complete `KidoColoring` folder and open it using the existing Unity 6000.3.14f1 installation.
2. Open `Assets/KidoColoring/Scenes/KidoGame.unity` directly and press Play after import.
3. Run **Window → General → Test Runner → EditMode → Kido.Tests**. Record compilation errors and results before mobile builds.
4. Use the existing Editor menu to verify installed build modules. No Editor installation, package addition, or project-setting change was made in this rebuild. The older Configure menu changes project settings and is not required just to open this scene.

## Implemented in source

- All 30 approved designs: transparent 2048px outlines, 384px gallery thumbnails, 1024px source maps extended to a stable 1536px drawable page, and SVG masters.
- The approved vibrant 30-color palette, with infinite scroll and centered selection.
- All 15 patterns retain their original source colors, independent of the selected palette color.
- Fill, brush, eraser, pattern and rainbow tools; Fairy Dust texture and slow white twinkles restricted to glitter-painted pixels.
- The outside white page/background can be colored with every tool and restored through save/undo.
- Looping Jingle Bells background music plays across splash, gallery and coloring screens and follows the existing parent Sound setting.
- Gallery: four category destinations, two-row picture cards, live background blending and a Tracing Coming Soon section. Exact mascot tabs await accessible original assets.
- Undo restores both color and glitter; reset requires two taps within three seconds and is undoable. Done is green at top-left; Undo and Reset remain at upper-right. Done saves, celebrates and fades to the menu.
- Compressed, checksummed alternating save slots, a single background save writer, pause/focus/Done flushes, and cached gallery previews.
- Memory-bounded tile undo, GPU outline compositing, small gallery assets, explicit active-art release and no per-frame pixel readback for glitter.

The native source is the current implementation. `Preview/` and the previous art tools are **archived first-play references**, containing old artwork and controls. They are not a preview or test of this rebuild. Current vector sources are in `ApprovedArt/`, not the legacy `ArtSource/` directory.

## What was verified here

Six offline content checks passed, including catalog/vector masters, transparency/thumbnails, region labels, patterns, Metal reserved-name regression and unique Unity metadata. `git diff --check` passed. These checks do not compile the C# or shader.

**Not verified:** C# compilation, shader compilation/rendering, native tests, iOS/Android builds, touch behavior, lifecycle saves, frame time, thermal behavior or memory use. Native regression tests are included but not executed. See the latest update note and `docs/PERFORMANCE.md`.

## Remaining work

- Run native visual validation against Figma file `ORpZoUySkEUioAZ7bU0ULd`. Full unclipped or phone-specific splash decorations would allow the phone crop to match exactly.
- Close remaining open decorative region boundaries and review very small coloring regions. The diagnostic contact sheet is `docs/ArtQA/regions-contact-sheet.png`; its bright colors are diagnostic labels, not prescribed coloring.
- Run Unity and physical-device validation, then fix findings before release.
- Finish the previously scoped store purchase/restore and child-appropriate advertising decisions/integration. No commerce SDK or live transaction was added.

Artwork revision 2 uses a separate `KidoPaintings-v2` save directory. Old prototype paintings are left untouched because their outlines and region coordinates differ; they are not silently applied to new artwork. There is no cloud save sync.

A local Git history is included. No remote, credentials or signing material is included.
