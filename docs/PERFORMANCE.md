# Performance architecture and device verification

## Chosen architecture

The 20 October update keeps authoritative painting state in a 1536 × 1536 CPU color buffer and byte-per-pixel effect mask. The original 1024 × 1024 artwork region map sits inside 256px page gutters; screen margins map to these gutters so white space up to the palette is drawable without changing old artwork coordinates. A 2048 × 2048 transparent outline is composited on the GPU. SVG masters remain unchanged. The gallery uses separate 384px textures.

The shader keeps glitter grain in artwork coordinates and adds sparse white four-point highlights with roughly four-second phases. It runs only inside the paint effect mask, below the black outlines. Motion-off freezes twinkle time while retaining the texture. No particle GameObjects, allocations, paint writes, or GPU readbacks are needed to animate glitter.

Painting uploads are limited to 30 per second while changed, and zero while idle. Unity Texture2D.Apply still uploads full paint/effect textures, not dirty tiles. At 1536 square this is about 11.25 MiB per upload: roughly 337.5 MiB/s at the maximum rate, before driver overhead. This increase is a material cost of preserving original art coordinates while adding drawable margins. Profile bandwidth/latency on the chosen minimum devices before release; GPU stamping or measured resolution changes may be necessary.

Undo copies touched 64px tiles. Retained snapshots are capped at 16 MiB and 24 gestures for the shipped resolution. A whole-page gesture can require 11.25 MiB, so only one such gesture fits in the budget. Small brush gestures retain deeper undo. Allocations occur when a tile is first touched, not for every paint pixel.

Only one save worker runs. Its owned snapshot is copied into reusable buffers on the main thread before background compression, checksum and disk IO. No Unity objects are accessed by the worker. Done now yields while waiting for writes; pause/focus loss flush synchronously. Encoding uses its existing stream buffer for hashing/compression, avoiding two whole-page copies (about 22.5 MiB). A temporary file is flushed/verified before replacing the older slot; failures preserve the other generation. Preview PNGs are disposable caches and cannot invalidate a valid painting.

Startup reads cached previews, not thirty full painting buffers. Outlines and gallery textures have Read/Write disabled and mipmaps disabled. Pattern textures remain readable for sampling. Active outlines, region TextAssets, materials and dynamic textures are explicitly released. Low-memory notifications drop hidden saved previews while preserving paint/undo state.

## Approximate owned memory, not device measurements

| Data | Approximate size |
| --- | --- |
| Active region labels | 4.5 MiB CPU; original 2 MiB map temporarily exists while expanding |
| Paint and effect arrays | 11.25 MiB CPU |
| Reusable save snapshot | 11.25 MiB CPU |
| Dynamic texture CPU copies | About 11.25 MiB CPU |
| Undo snapshots | At most 16 MiB retained at shipped resolution |
| Active uncompressed outline | 16 MiB GPU |
| Paint and effect textures | About 11.25 MiB GPU; actual format allocation is platform-dependent |
| Fill reveal snapshot | 9 MiB GPU, allocated on first animated fill and released with canvas |
| Fifteen 256px patterns | About 3.75 MiB each on CPU/GPU, plus up to 3.75 MiB cached samples per canvas |
| Thirty 384px gallery images | Up to about 16.9 MiB GPU per set; menu-owned source thumbs explicitly released on navigation; app-owned saved previews may form another set |
| Palette cells and feedback | 30 color + 15 pattern cells reused; fixed 128-particle mesh; no per-gesture particle GameObjects |

Compression/hash/readback create temporary buffers. Unity, driver, audio, UI, managed heap capacity and retained GC generations add overhead not included above. Track peak resident memory, not just texture estimates. Device data should determine platform texture compression and painting resolution changes.

Do not describe these estimates as profiling results. The menu/font implementation, native tests and mobile frame time/memory remain unverified in Unity. See `MENU_UPDATE_2026-10-20.md` for the current acceptance pass and missing assets.

## Device test gates

- Use the existing Unity 6000.3.14f1 installation; inspect actual modules before build. Do not assume Android support from the iOS badge.
- Test a smaller iPhone, a supported iPad, a lower-memory Android phone and an Android tablet. The minimum supported set still needs a product decision.
- Capture CPU/GPU frame times, main-thread allocations, texture memory and total resident memory with the Unity Profiler on development builds. Target 60fps interaction; also test sustained 30fps fallback if required by the chosen minimum hardware.
- Draw dense glitter for five minutes. Check subtle motion, shader cost, temperature and battery behavior.
- Open and close all 30 canvases twice. Memory should settle rather than increase each cycle. Confirm only one full outline stays resident during painting.
- Fill, overwrite glitter, erase, undo and reset repeatedly; ensure undo restores both colors and effects.
- Background during a long stroke and during a save; force-stop/relaunch; simulate one damaged save slot and insufficient disk space. Confirm recoverable work is not overwritten.
- Verify large controls on the smallest target screen, safe areas, palette scrolling versus selection, multi-touch rejection and uninterrupted drawing.
- Keep final decorative Figma UI on static canvases; isolate frequently animated groups if profiling shows canvas rebuild cost. Add sprite atlasing after actual Figma exports exist.

## Primary references consulted

- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Texture2D.Apply.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.UnloadAsset.html
- https://create.unity3d.com/Unity-UI-optimization-tips

These guide resource lifetime and profiling decisions. They do not prove the uncompiled implementation meets its targets.

## Figma UI integration checkpoint

Thirteen original UI PNG resources are read-only on the CPU, without mipmaps. Duplicate dot files share one texture. Six tool textures and three small action-button textures are loaded while coloring; splash/menu assets are released on screen changes. The gallery thumbnails are now 384px to stay crisp at native phone/tablet size; profile actual Resources retention before deciding whether to move the gallery to explicit asynchronous loading. The supplied MP3 imports as a streaming clip with background loading. Uniform design-space scaling preserves icon proportions without runtime image resampling. Layout recalculates on safe-area size changes, not every frame. A single cached circle texture supplies native round backgrounds. These are source-level choices, not measured performance results. Profile UI batches, ScrollRect scrolling, audio streaming and shader cost in Unity before release.
