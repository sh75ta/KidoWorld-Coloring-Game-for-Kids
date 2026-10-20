# Figma UI integration — 19 October 2026

Latest menu/interaction status: `../MENU_UPDATE_2026-10-20.md`. The new frame
`tKMLc3YnLj2fez5lIisF98 / 2218:2946` returned connector access errors for design
context, screenshot, metadata and export. The new menu is a functional draft
from the attached video with League Gothic, pending original category mascots
and exact frame tokens. Historical notes below describe earlier source versions.

Source file: `ORpZoUySkEUioAZ7bU0ULd`. Phone frames `6:36` / `6:99`; tablet frames `6:46` / `6:62`. Retrieved high-fidelity design context is retained alongside this note. These references describe layout, not a React dependency.

## Implemented in Unity source

- Uniform height-based scaling inside the existing safe area. Separate phone and tablet reference dimensions, selected at aspect ratio 1.55; wide phones gain drawing-area whitespace rather than stretched icons.
- Left square coloring surface, full-height single-column scrolling palette, pink right toolbar. Eight swatches are visible; all 30 approved colors remain available in their approved order. Pattern mode uses the same column for 15 patterns.
- Six original PNG icons from the attached ZIP, unchanged on disk, with display transforms matching the Figma screen orientation. Order: Eraser, Fill, Brush, Glitter, Pattern, Rainbow. Selected tool uses a purple circle border.
- Original green Done artwork at top-left; original Undo and Reset artwork at the upper-right edge of the drawing area. This follows the latest Figma frames and supersedes the earlier bottom-right instruction. Undo and Reset have larger transparent touch targets around their visible controls.
- Supplied 1154×914 logo, fitted without distortion into the exact 577×457 design bounds, plus pink/yellow splash decorations and one reused purple-dot texture; a progress bar reflects preview loading. The identical three dot files are consolidated, with hashes recorded in `ApprovedArt/figma-ui-manifest.json`.
- UI textures are not CPU-readable and have no mipmaps. Only current-screen UI textures are retained; switching screens releases them. No SDK, package, Editor version or ProjectSettings change.

## Asset completion and visual follow-up

The second ZIP supplies Done (`6:114`), Undo (`6:115`) and Reset (`6:128`). All three retain their original PNG bytes and exact 229×98, 95×97 and 102×99 frame dimensions. They are bound to save-and-return, undo, and reset confirmation respectively. Button state feedback targets the actual artwork; no text or substitute shapes cover it.

The separate transparent logo attachment is now integrated as `Kido/UI/Figma/Logo`. Its 1154×914 dimensions exactly match the aspect ratio of the 577×457 Figma logo slot. The splash text placeholder has been removed. Native visual sign-off remains pending.

The supplied paint splashes are already clipped node exports at 327×403 and 336×284. Their natural aspect ratios are preserved at the corners. They match the tablet crop; the phone reference has a slightly different visible crop that cannot be fully reconstructed from these clipped files. Full unclipped decorations or phone-specific exports are needed for exact phone matching. The tablet reference also puts one purple dot over the logo; retained as authored.

Native circle backgrounds are built-in UI geometry. The PNG source artwork is never re-rasterized or recolored. Runtime button states apply the standard pressed/disabled tint. Tool dimensions preserve each supplied icon's aspect ratio; the source tool-sheet exports have slightly different bounds from the cropped icons in the final frame, so exact pixel-level comparison remains pending in Unity.

## Validation and remaining gates

- Thirteen resource files match their respective source ZIPs or individual attachment byte-for-byte; PNG headers, dimensions and transparency inspected. No failed network responses remain in Assets.
- Five existing offline content/metadata checks pass, including unique metadata for the new resources. `git diff --check` passes.
- Phone/tablet layout coordinates reviewed against the retrieved frames. Native rendering is **not verified**.
- Unity, .NET and a C# compiler are unavailable here. Compile and run `KidoGame.unity` in the existing Unity 6000.3.14f1 installation. Check 1600×900, 1422×1067, 4:3 tablets and wide phones with safe-area insets. Verify tool orientation, full palette scrolling, scrolling without accidental color selection, pattern mode, undo/reset/done, resume saves, and Fairy Dust clipping.
- The gallery and confirmation dialogs retain their prior design; this source file does not define a finished gallery UI. Native tests, GPU profiling, mobile builds and final visual approval remain outstanding.
