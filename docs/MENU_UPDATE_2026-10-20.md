# Menu and interaction update — 20 October 2026

This is a source update, not a verified Unity build. It continues the earlier
interaction work and implements the menu behaviour in the supplied prototype.

## Menu implemented in source

- Continuous horizontal discovery: three worlds, ten distinct approved pictures
  per world, two rows. No duplicated Santa placeholders from the video.
- A fourth, reachable Tracing section with large **COMING SOON** text, no fake
  tracing canvases and no purchase action.
- Four category tabs scroll to their section. A new drag or wheel gesture cancels
  automatic scrolling. The visible category follows the viewport, and the page
  background blends while adjacent categories share the viewport.
- Square white, black-bordered artwork cards; compact progress bars; remembered
  gallery position when returning from a picture.
- Height-based layout recalculates for phone/tablet aspect ratios and safe areas.
- League Gothic Regular 1.601 is embedded as a font asset, with its OFL license.
  No font installation, package installation or project-setting change.
- Button tap SFX/bounce, and blocking quick fades between splash/menu/canvas.
  A fade blocks repeated taps while a screen is being replaced.
- The existing grown-up gate remains reachable from the menu.

## Exact visual match remains blocked

The requested frame is:
https://www.figma.com/design/tKMLc3YnLj2fez5lIisF98/Game-Icon?node-id=2218-2946

On 20 October, design-context, metadata, screenshot and asset-export requests all
returned the connector's file-access error. This is **not** a claim that Figma
view-only users generally cannot download assets. The previously accessible
KidoColoring file contained only the earlier frames.

The three attached menu videos have identical SHA-256:
`80c5081ec61b433abfe391ab63b47cd17a78649a3a3ebb1901bec5cfde323637`.

The supplied video establishes the two-row gallery, tab expansion and scrolling
behaviour. It does not provide usable original Santa/lion/rocket/letter-A assets
or exact Figma layout tokens. This implementation is therefore a **functional
video-based menu draft**, not an approved pixel-perfect Figma reconstruction.
The animal background color is provisional; exact colors/spacing need the frame.

Until the original exports are available, the menu deliberately uses readable
text tabs. It does not redraw, crop from the video, or substitute other mascots.
`GalleryMenu` supports original transparent exports at these resource paths:

| Resource path (omit extension in code) | Intended original asset |
| --- | --- |
| Kido/UI/Figma/Menu/CHRISTMAS | Santa category mascot |
| Kido/UI/Figma/Menu/ANIMALS | Lion category mascot |
| Kido/UI/Figma/Menu/VEHICLES | Rocket category mascot |
| Kido/UI/Figma/Menu/TRACING | Letter A category mascot |

With these textures present, the selected tab expands and shows its label;
unselected tabs show their mascots. Verify the actual exported bounds against
the Figma frame before treating this hookup as final. Export the frame and its
four original category assets, or copy the frame into the readable Figma file.

## Earlier interaction requests included in this source update

| Request | Source status / limitation |
| --- | --- |
| White area paintable up to palette | Stable 1536-square page around original 1024-square art; screen margins map to page gutters |
| Preserve existing paintings | Old saves copied into centered page coordinates on load; original save files remain untouched; new saves use `_page` IDs |
| Reset twice within three seconds | Inline prompt, no confirmation dialog; reset remains undoable |
| Bigger brush | Paint radius doubled from 28 to 56; no separate draggable cursor artwork was added |
| Circular fill reveal | 0.38-second shader reveal from touch, aspect-correct on the display; logical painting/saves update immediately |
| Lift-off sparkle + SFX | Bounded 128-particle mesh, selected-color stars lasting 1.4 seconds, including no-change touches |
| Color/pattern palettes | Circular positioning of existing cells; drag/inertia, soft detent tick, nearest-center snapping; no cloned texture sets |
| Tool and palette states | Selected tool enlarged/tinted, other tools reduced; palette translucent and disabled for Rainbow/Eraser |
| Pattern colors | Original texture colors retained, independent of selected swatch; sample data cached per open canvas |
| Glitter | More evident white star twinkles within the saved glitter mask; GPU appearance still needs device review |
| Done | Save first; top-down bounded confetti and SFX, then quick fade to menu |
| Intro decorations | Subtle float/rotation, resize-safe, follows Motion setting |
| BGM | Previously supplied Jingle Bells track continues globally |
| Letter-by-letter logo bounce | **Not implemented**: existing supplied logo is one flattened texture; original letter layers/exports needed |
| Kids' “KidoWorld” chorus | **No recording supplied**: optional `Kido/Audio/KidoWorldChorus` resource hookup exists; no synthesized children recording claimed |

## Technical/design audit and corrections

- Corrected section destinations, including the final tracing page, using actual
  content width rather than the old three-category normalized-scroll formula.
- Recomputed card geometry on resize and preserved gallery position.
- Kept category content independent of catalog ordering; each world has its own
  five columns of two canvases.
- Released menu-owned thumbnails and font when leaving the menu. Saved-preview
  textures remain owned by the application cache, not the menu.
- Removed per-selection checkmark creation and repeated pattern pixel reads.
- Reused a bounded particle mesh, corrected its fade curve, and made button
  animation respect the Motion setting.
- Circular fill uses display aspect ratio, not stretched page coordinates.
- Thumbnails use canonical square page coordinates rather than the current
  phone/tablet layout, so switching device aspect does not stretch saved previews.
- Added migration, page-mapping, palette wrapping, category destination, fixed
  pattern-color and extended-background native regression tests. Updated the
  undo-budget test to the new 1536-square page.

## What actually ran

- `node --test tools/test-approved-content.cjs`: **6 passed, 0 failed**.
- `git diff --check`: passed.
- Font parsed successfully: family **League Gothic**, style **Regular**.
- All new Unity assets have stable, unique `.meta` files.
- No changes to `Packages/` or `ProjectSettings/`.

**Not run here:** C# compilation, shader compilation, native EditMode tests,
Unity rendering, touch interaction, audio playback, device builds, lifecycle
stress tests, memory/frame-time/thermal profiling. No Unity or C# compiler is
available in this remote workspace. The new tests are included, not reported as
passing. No claim of “no crashes” or measured mobile performance is justified yet.

## Native acceptance pass

Use the installed Unity **6000.3.14f1**. Open
`Assets/KidoColoring/Scenes/KidoGame.unity` directly; do not run the Configure
menu. Run EditMode tests, then review 1600×900 phones, 1422×1067 tablets, 4:3
tablets and wide phones with safe areas.

Swipe through all 30 distinct pictures and Tracing; tap every category from both
ends; interrupt a tab scroll; resize; open a partially painted picture and return.
Check that the picture is not opened after a drag and that repeated taps cannot
start two transitions. Verify original save migration, full-page paint, Undo,
two-tap Reset, circular fill, palette wrap/center/disabled states and Done saving
before navigation. Review Sound/Motion off and actual SFX loudness. Profile peak
memory during save compression and rapid repeated canvas navigation.

## Font provenance

Official release:
https://github.com/theleagueof/league-gothic/releases/tag/1.601

Original TTF SHA-256:
`4c1e8b736c5baef057523627f8b5a4dbb19dbf9017f7d3b4cfd12dab6e8be59a`.
The original OFL is retained beside the font. The font binary was not modified.
