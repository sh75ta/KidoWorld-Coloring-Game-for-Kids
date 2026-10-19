# Rebuild status — 19 October 2026

## Approved decisions

- Thirty transparent canvases in three worlds; scalable vector masters retained.
- Fifteen patterns, including Candy Cane Stripes, Christmas Trees & Stars, Christmas Gifts.
- Vibrant 30 palette approved on 19 October.
- Glitter concept D: Fairy Dust with subtle white sparkles, slowly twinkling and fading inside the paint. Strong grain, gel, foil and jewel flakes were not selected.
- Done is green, bottom right, and saves/returns; Undo and Reset sit nearby. No separate coloring-screen Back button.
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
| Figma screens | BLOCKED; actual screens/icons not retrieved | Fetch frame hierarchy, screenshots, exported assets |
| Native platforms | Existing Unity version retained | Compile, EditMode suite, Xcode export, Android build |

## Figma handoff

URL: https://www.figma.com/design/tKMLc3YnLj2fez5lIisF98/Game-Icon?node-id=2215-2752

The user installed Figma during this task and installation was confirmed. The session still exposed no callable Figma design-reading tools after repeated capability discovery. The public URL was also unreadable through the web reader. Do not ask the user to install Figma again. Continue with its tools when exposed; alternatively use user-provided frame PNGs and individual transparent icon/button exports. No private browser session was accessed.

Before implementing the final UI, inspect the node and nearby frames, record reference frame dimensions, export actual layer artwork, and preserve aspect ratios/9-slice borders where applicable. Use a common safe-area root. Validate both tablet and landscape-phone layouts against the source at matching aspect ratios. Keep labels/actions consistent with the approved requirements.

## Region review

Masks are built at 1024 square using mask-only one-pixel dilation and label extension beneath ink. The four page corners identify outside space, allowing truly edge-clipped shapes to be labeled. This does not close every open line in the art. Open water, seaweed/branches, snow/ground and road details still require review; small Santa/elf/gift details need age-appropriate touch testing. The main characters/vehicles have paintable regions. The mask report records 22–93 regions per canvas. Do not claim every decorative area is already isolated.

## Validation completed

`node --test tools/test-approved-content.cjs` — 5 passed, 0 failed. These inspect the actual assets and resource metadata; they are not substitutes for C# tests.

`git diff --check` — passed.

Unity/.NET/C# compiler executables were unavailable. Native tests, shader output and mobile performance were not run. All performance numbers in PERFORMANCE.md are estimates or targets, never measurements.
