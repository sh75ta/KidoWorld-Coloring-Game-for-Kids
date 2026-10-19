# Kido Coloring — approved-content rebuild

Updated 19 October 2026. Unity **6000.3.14f1**. Branch `rebuild/approved-art-v2`.

This package contains the rebuilt Unity source and approved artwork. **The Figma visual implementation is unfinished, and this source has not yet been compiled or run in Unity.** The installed Figma connection did not expose design-reading tools in this session; no Figma frames or icons were fetched. The current splash and tool graphics are provisional. Do not treat them as the approved Figma design.

## Open the current rebuild

1. Extract the complete `KidoColoring` folder and open it using the existing Unity 6000.3.14f1 installation.
2. Open `Assets/KidoColoring/Scenes/KidoGame.unity` directly and press Play after import.
3. Run **Window → General → Test Runner → EditMode → Kido.Tests**. Record compilation errors and results before mobile builds.
4. Use the existing Editor menu to verify installed build modules. No Editor installation, package addition, or project-setting change was made in this rebuild. The older Configure menu changes project settings and is not required just to open this scene.

## Implemented in source

- All 30 approved designs: transparent 2048px outline textures, 192px gallery thumbnails, 1024px painting-region maps, and the scalable SVG masters.
- The approved vibrant 30-color palette; stable positions in a scrollable two-column palette.
- All 15 patterns: twelve tintable designs and three Christmas designs with fixed festive colors.
- Fill, brush, eraser, pattern and rainbow tools; Fairy Dust texture and slow white twinkles restricted to glitter-painted pixels.
- Undo restores both color and glitter; reset is confirmed and undoable. Done is green, at bottom right with Undo and Reset. There is no separate coloring-screen Back button.
- Compressed, checksummed alternating save slots, a single background save writer, pause/focus/Done flushes, and cached gallery previews.
- Memory-bounded tile undo, GPU outline compositing, small gallery assets, explicit active-art release and no per-frame pixel readback for glitter.

The native source is the current implementation. `Preview/` and the previous art tools are **archived first-play references**, containing old artwork and controls. They are not a preview or test of this rebuild. Current vector sources are in `ApprovedArt/`, not the legacy `ArtSource/` directory.

## What was verified here

Five offline content checks passed: catalog and vector masters; transparent outlines/thumbnails; region labels/dimensions/exterior; fifteen patterns; unique Unity metadata. `git diff --check` passed. All 30 region masks were visually reviewed together.

**Not verified:** C# compilation, shader compilation/rendering, Unity tests, iOS/Android builds, touch behavior, lifecycle saves on devices, frame time, thermal behavior or memory use. Sixteen native EditMode tests are included, but were not executed. See `docs/REBUILD_STATUS.md` and `docs/PERFORMANCE.md`.

## Remaining work

- Read Figma file `tKMLc3YnLj2fez5lIisF98`, starting at node `2215:2752`, export its actual splash/coloring-screen assets, and implement those layouts accurately.
- Close remaining open decorative region boundaries and review very small coloring regions. The diagnostic contact sheet is `docs/ArtQA/regions-contact-sheet.png`; its bright colors are diagnostic labels, not prescribed coloring.
- Run Unity and physical-device validation, then fix findings before release.
- Finish the previously scoped store purchase/restore and child-appropriate advertising decisions/integration. No commerce SDK or live transaction was added.

Artwork revision 2 uses a separate `KidoPaintings-v2` save directory. Old prototype paintings are left untouched because their outlines and region coordinates differ; they are not silently applied to new artwork. There is no cloud save sync.

A local Git history is included. No remote, credentials or signing material is included.
