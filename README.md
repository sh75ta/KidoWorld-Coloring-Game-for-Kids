# Kido Coloring — approved-content rebuild

For continuation in a new local Codex session, read `AGENTS.md` and `HANDOFF.md` first.

Updated 19 October 2026. Unity **6000.3.14f1**. Branch `rebuild/approved-art-v2`.

This package contains the rebuilt Unity source, approved artwork, and the supplied original Figma logo, tool icons, action buttons and splash decorations. Phone/tablet coloring layouts are implemented in source. **The splash logo and all three action controls now use your supplied artwork. This source has not yet been compiled or run in Unity.** See `docs/Figma/INTEGRATION.md` for visual validation details and the remaining phone-decoration crop difference.

## Open the current rebuild

1. Extract the complete `KidoColoring` folder and open it using the existing Unity 6000.3.14f1 installation.
2. Open `Assets/KidoColoring/Scenes/KidoGame.unity` directly and press Play after import.
3. Run **Window → General → Test Runner → EditMode → Kido.Tests**. Record compilation errors and results before mobile builds.
4. Use the existing Editor menu to verify installed build modules. No Editor installation, package addition, or project-setting change was made in this rebuild. The older Configure menu changes project settings and is not required just to open this scene.

## Implemented in source

- All 30 approved designs: transparent 2048px outline textures, 192px gallery thumbnails, 1024px painting-region maps, and the scalable SVG masters.
- The approved vibrant 30-color palette; stable positions in a scrollable single-column palette with eight visible swatches.
- All 15 patterns: twelve tintable designs and three Christmas designs with fixed festive colors.
- Fill, brush, eraser, pattern and rainbow tools; Fairy Dust texture and slow white twinkles restricted to glitter-painted pixels.
- Undo restores both color and glitter; reset is confirmed and undoable. Done is green at top-left; Undo and Reset are at the upper-right of the canvas, following the latest Figma frames. There is no separate coloring-screen Back button.
- Compressed, checksummed alternating save slots, a single background save writer, pause/focus/Done flushes, and cached gallery previews.
- Memory-bounded tile undo, GPU outline compositing, small gallery assets, explicit active-art release and no per-frame pixel readback for glitter.

The native source is the current implementation. `Preview/` and the previous art tools are **archived first-play references**, containing old artwork and controls. They are not a preview or test of this rebuild. Current vector sources are in `ApprovedArt/`, not the legacy `ArtSource/` directory.

## What was verified here

Five offline content checks passed: catalog and vector masters; transparent outlines/thumbnails; region labels/dimensions/exterior; fifteen patterns; unique Unity metadata. `git diff --check` passed. All 30 region masks were visually reviewed together.

**Not verified:** C# compilation, shader compilation/rendering, Unity tests, iOS/Android builds, touch behavior, lifecycle saves on devices, frame time, thermal behavior or memory use. Sixteen native EditMode tests are included, but were not executed. See `docs/REBUILD_STATUS.md` and `docs/PERFORMANCE.md`.

## Remaining work

- Run native visual validation against Figma file `ORpZoUySkEUioAZ7bU0ULd`. Full unclipped or phone-specific splash decorations would allow the phone crop to match exactly.
- Close remaining open decorative region boundaries and review very small coloring regions. The diagnostic contact sheet is `docs/ArtQA/regions-contact-sheet.png`; its bright colors are diagnostic labels, not prescribed coloring.
- Run Unity and physical-device validation, then fix findings before release.
- Finish the previously scoped store purchase/restore and child-appropriate advertising decisions/integration. No commerce SDK or live transaction was added.

Artwork revision 2 uses a separate `KidoPaintings-v2` save directory. Old prototype paintings are left untouched because their outlines and region coordinates differ; they are not silently applied to new artwork. There is no cloud save sync.

A local Git history is included. No remote, credentials or signing material is included.
