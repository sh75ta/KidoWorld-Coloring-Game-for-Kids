# Figma UI integration — 19 October 2026

Source file: `ORpZoUySkEUioAZ7bU0ULd`. Phone frames `6:36` / `6:99`; tablet frames `6:46` / `6:62`. Retrieved high-fidelity design context is retained alongside this note. These references describe layout, not a React dependency.

## Implemented in Unity source

- Uniform height-based scaling inside the existing safe area. Separate phone and tablet reference dimensions, selected at aspect ratio 1.55; wide phones gain drawing-area whitespace rather than stretched icons.
- Left square coloring surface, full-height single-column scrolling palette, pink right toolbar. Eight swatches are visible; all 30 approved colors remain available in their approved order. Pattern mode uses the same column for 15 patterns.
- Six original PNG icons from the attached ZIP, unchanged on disk, with display transforms matching the Figma screen orientation. Order: Eraser, Fill, Brush, Glitter, Pattern, Rainbow. Selected tool uses a purple circle border.
- Green Done at top-left; Undo and Reset at the upper-right edge of the drawing area. This follows the latest Figma frames and supersedes the earlier bottom-right instruction. Undo and Reset have larger transparent touch targets around their visible controls.
- Supplied pink/yellow splash decorations and one reused purple-dot texture; a progress bar reflects preview loading. The identical three dot files are consolidated, with hashes recorded in `ApprovedArt/figma-ui-manifest.json`.
- UI textures are not CPU-readable and have no mipmaps. Only current-screen UI textures are retained; switching screens releases them. No SDK, package, Editor version or ProjectSettings change.

## Incomplete visual items

The ZIP does **not** contain the logo or Done/Undo/Reset images. Existing text and native buttons remain explicitly provisional. This is an integration checkpoint, not final Figma visual sign-off. Supply four transparent PNG/SVG exports: logo (`6:37` or `6:47`), Done (`6:114`), Undo (`6:115`), Reset (`6:128`).

The supplied paint splashes are already clipped node exports at 327×403 and 336×284. Their natural aspect ratios are preserved at the corners. They match the tablet crop; the phone reference has a slightly different visible crop that cannot be fully reconstructed from these clipped files. Full unclipped decorations or phone-specific exports are needed for exact phone matching. The tablet reference also puts one purple dot over the logo; retained as authored pending logo integration.

Native circle backgrounds are built-in UI geometry. The PNG artwork is never re-rasterized or recolored. Tool dimensions preserve each supplied icon's aspect ratio; the source tool-sheet exports have slightly different bounds from the cropped icons in the final frame, so exact pixel-level comparison remains pending in Unity.

## Validation and remaining gates

- Nine resource files match the source ZIP byte-for-byte; PNG headers, dimensions and transparency inspected. No failed network responses remain in Assets.
- Five existing offline content/metadata checks pass, including unique metadata for the new resources. `git diff --check` passes.
- Phone/tablet layout coordinates reviewed against the retrieved frames. Native rendering is **not verified**.
- Unity, .NET and a C# compiler are unavailable here. Compile and run `KidoGame.unity` in the existing Unity 6000.3.14f1 installation. Check 1600×900, 1422×1067, 4:3 tablets and wide phones with safe-area insets. Verify tool orientation, full palette scrolling, scrolling without accidental color selection, pattern mode, undo/reset/done, resume saves, and Fairy Dust clipping.
- The gallery and confirmation dialogs retain their prior design; this source file does not define a finished gallery UI. Native tests, GPU profiling, mobile builds and final visual approval remain outstanding.
