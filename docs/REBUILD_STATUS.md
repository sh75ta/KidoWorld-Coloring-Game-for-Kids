# Rebuild status — 19 October 2026

## Approved decisions

- Thirty transparent canvases in three worlds; scalable vector masters retained.
- Fifteen patterns, including Candy Cane Stripes, Christmas Trees & Stars, Christmas Gifts.
- Vibrant 30 palette approved on 19 October.
- Glitter concept D: Fairy Dust with subtle white sparkles, slowly twinkling and fading inside the paint. Strong grain, gel, foil and jewel flakes were not selected.
- Done is green at top-left and saves/returns; Undo and Reset sit at the upper-right of the canvas as in the latest Figma frames. No separate coloring-screen Back button.
- Supplied Figma is the authority for splash, coloring layout, brushes, buttons and icons. Earlier UI kits were rejected.

## Implementation versus validation

| Area | Source status | Validation still needed |
| --- | --- | --- |
| Content | 30 approved artworks and 15 patterns imported | Touch-region cleanup; small-region usability |
| Palette | Exact 30 approved hex values | Device color review |
| Fairy Dust | GPU texture, local white twinkle, saved effect mask, motion toggle | Shader compile, appearance and GPU cost |
| Painting | Region-constrained strokes; per-region bounded fill; one gesture per undo | Native touch and edge behavior |
| Persistence | Compressed V2 payload; SHA-256; two alternating slots; one worker; owned snapshots | App interruption, disk-full, actual save latency |
| Memory | One active full artwork, bounded undo, read-only outlines/thumbnails, released art | Profiler captures and repeated navigation test |
| Figma screens | Phone/tablet contexts retrieved; supplied original logo/icons/buttons/decorations integrated | Native visual validation and phone splash crop |
| Native platforms | Existing Unity version retained | Compile, EditMode suite, Xcode export, Android build |

## Figma handoff

URL: https://www.figma.com/design/ORpZoUySkEUioAZ7bU0ULd/KidoColoring?node-id=0-1

The new file is readable through Figma. Phone/tablet frames and screenshots were retrieved. Direct image downloads and the browser returned Site Unavailable; the user then supplied KidoColoring.zip containing six icons and three unique splash decorations. These originals are integrated unchanged on disk. A second ZIP supplied Done, Undo and Reset, now integrated as original PNGs. The separately attached transparent logo has also replaced the splash text placeholder. No additional Figma permission change or installation is required for the supplied assets.

See `docs/Figma/INTEGRATION.md` for source frame IDs, implemented layout, asset mapping, crop limitations and validation steps. Menu/dialog styling remains from the earlier prototype. No claim of complete Figma visual fidelity or native runtime validation is made.

## Region review

Masks are built at 1024 square using mask-only one-pixel dilation and label extension beneath ink. The four page corners identify outside space, allowing truly edge-clipped shapes to be labeled. This does not close every open line in the art. Open water, seaweed/branches, snow/ground and road details still require review; small Santa/elf/gift details need age-appropriate touch testing. The main characters/vehicles have paintable regions. The mask report records 22–93 regions per canvas. Do not claim every decorative area is already isolated.

## Validation completed

`node --test tools/test-approved-content.cjs` — 5 passed, 0 failed. These inspect the actual assets and resource metadata; they are not substitutes for C# tests.

`git diff --check` — passed.

Unity/.NET/C# compiler executables were unavailable. Native tests, shader output and mobile performance were not run. All performance numbers in PERFORMANCE.md are estimates or targets, never measurements.
