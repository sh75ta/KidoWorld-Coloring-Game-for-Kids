# Performance architecture and device verification

## Chosen architecture

Keep authoritative painting state in a 1024 × 1024 CPU color buffer and a byte-per-pixel effect mask. A 2048 × 2048 transparent outline is composited on the GPU; the original SVG master remains available for future export sizes without adding a vector-rendering package. The gallery uses separate 192px textures.

The shader keeps glitter grain in artwork coordinates and adds sparse white four-point highlights with roughly four-second phases. It runs only inside the paint effect mask, below the black outlines. Motion-off freezes twinkle time while retaining the texture. No particle GameObjects, allocations, paint writes, or GPU readbacks are needed to animate glitter.

Painting uploads are limited to 30 per second while changed, and zero while idle. Unity Texture2D.Apply still uploads the full paint/effect textures: this is a deliberate simple baseline, NOT a claim of partial GPU uploads. At 1024 square, paint plus effect data is about 5 MiB per upload. Profile bandwidth and latency; if necessary introduce measured GPU stamping/dirty-tile rendering without changing the save model.

Undo copies only touched 64px tiles. Retained snapshots are capped at 16 MiB and 24 gestures for the shipped resolution, evicting oldest gestures as a new gesture grows. A whole-page gesture requires up to 5 MiB. Small allocations occur when a new tile is first touched, not for every paint pixel.

Only one save worker runs. Its owned snapshot is copied into reusable buffers on the main thread before background compression, checksum and disk IO. No Unity objects are accessed by the worker. Pause, focus loss and Done flush outstanding writes before leaving. A temporary file is flushed/verified before replacing the older of two slots. A failed write preserves the other generation. Preview PNGs are disposable caches and cannot invalidate a valid painting.

Startup reads cached previews, not thirty full painting buffers. Outlines and gallery textures have Read/Write disabled and mipmaps disabled. Pattern textures remain readable for sampling. Active outlines, region TextAssets, materials and dynamic textures are explicitly released. Low-memory notifications drop hidden saved previews while preserving paint/undo state.

## Approximate owned memory, not device measurements

| Data | Approximate size |
| --- | --- |
| Active region labels | 2 MiB CPU |
| Paint and effect arrays | 5 MiB CPU |
| Reusable save snapshot | 5 MiB CPU |
| Dynamic texture CPU copies | 5 MiB CPU |
| Undo snapshots | At most 16 MiB retained at shipped resolution |
| Active uncompressed outline | 16 MiB GPU |
| Paint and effect textures | 5 MiB GPU |
| Fifteen 256px patterns | About 3.75 MiB each on CPU and GPU, plus selected sample buffer |
| Thirty 192px gallery images | Up to about 4.22 MiB GPU per set; saved previews may form a second set |

Compression/hash/readback create temporary buffers. Unity, driver, audio, UI, managed heap capacity and retained GC generations add overhead not included above. Track peak resident memory, not just texture estimates. Device data should determine platform texture compression and painting resolution changes.

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

Nine original UI PNG resources are read-only on the CPU, without mipmaps. Duplicate dot files share one texture. Only six tool textures are loaded while coloring; splash assets are released on screen changes. Uniform design-space scaling preserves icon proportions without runtime image resampling. Layout recalculates on safe-area size changes, not every frame. A single cached circle texture supplies native round backgrounds. These are source-level choices, not measured performance results. Profile UI batches, ScrollRect scrolling and shader cost in Unity before release.
