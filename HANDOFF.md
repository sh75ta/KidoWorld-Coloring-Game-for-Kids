# Kido Coloring — handoff to local Codex

## Latest checkpoint — 20 October 2026 (v4)

Read `docs/UPDATE_2026-10-20_v4.md` first: it supersedes the statements below on audio (Christmas Fun replaces Jingle Bells; intro chorus integrated), the saved-page size (now 2048x1536, `_page2` saves), the painting cursor (removed) and gallery progress bars (removed).

Read `docs/RECHECK_2026-10-20.md` and `docs/MENU_UPDATE_2026-10-20.md` first. They supersede historical statements
below about the menu, tintable patterns, reset confirmation and page dimensions.
Menu logic, League Gothic, scrolling palettes, full-page painting and interaction
feedback are updated in source. The four original menu mascots supplied in
`Game Icon (2).zip` are integrated. The flattened original logo is animated as
a continuous mesh with sequential letter-area bounce; independent letter layers
and the kids' chorus recording are absent. Offline checks pass; no native compile/test/profile
claim is made. `Packages/` and `ProjectSettings/` remain unchanged.

Prepared 19 October 2026. This is a project-and-decisions handoff, not a verbatim chat transcript. The complete current Unity source, assets, documentation and local Git history are included in this folder.

## Start here

The user wants to continue development directly in the Unity project on their computer, with changes visible in the Editor. They have authorized access and ordinary development work, but the prior session ran in a remote Linux workspace and had no local Unity connection. Do not assume the new session has that connection: verify the working directory, available tools, running Editor and installed version first.

The immediate next task is to compile this existing project, fix actual errors, open the coloring scene and validate the Figma UI and coloring behavior. No new design phase or asset request is needed before doing that work.

Suggested first message for the new session:

> Continue Kido Coloring in this local project. Read AGENTS.md and HANDOFF.md first. Verify the installed Unity Editor and project access, compile the existing project, fix errors, and test the current splash and coloring screens. Preserve the approved assets and saved artwork. Do not install packages or change project settings. Work directly in this folder and report actual validation results.

If this archive is being brought into an existing local project, inspect and preserve any local changes before merging. Extracting into a separate folder is safer than overwriting a working checkout blindly. The archive's parent folder contains the project root `KidoColoring`, which contains `Assets`, `Packages` and `ProjectSettings`.

## User preferences and constraints

- Target engine: Unity 6.3; this project records **6000.3.14f1**. Verify the actual installed Editor before development. Do not install/upgrade Unity or modules automatically.
- The user explicitly does not want package installations or project configuration changes. No `Packages` or `ProjectSettings` changes were made during the approved-art and Figma rebuilds.
- Keep a local Git repository; no remote, signing credentials, store accounts or API keys were configured by this work.
- Continue authorized work autonomously and keep the user informed. Earlier proposed UI kits were rejected; use the later Figma and supplied exports.
- The app should feel clear and spacious for ages 3–6. Avoid clutter and generic generated replacement icons. Preserve original art and icon proportions.

## Product scope

Working title: **Kido Coloring**. The supplied, approved splash artwork reads **kido world**; preserve that artwork. The application/product configuration was not renamed to match the logo.

Audience: children ages 3–6 in the US and UK. Platforms: iOS and Android, phones and tablets. Current UI is landscape with safe-area handling.

Thirty coloring canvases in three worlds, ten each:

1. Christmas & Winter — Santa, winter and gifts.
2. Animals, Dinosaurs, Fish & Birds.
3. Vehicles & Big Adventures — cars, planes, ships and monster trucks.

Required flow: animated loading/splash → gallery/progress screen → coloring canvas → Done saves and returns. Gallery has category tabs, continuous horizontal discovery, and a two-row grid. Persist every picture's coloring state and show progress/completion on gallery cards.

Coloring: Fill, Brush, Eraser, Glitter, Pattern and Rainbow tools; 30 approved vibrant colors; Undo; two-tap Reset safeguard; green Done. No separate Back button. Feedback includes sound, visual responses and completion celebration.

Later native feedback added these approved implementation changes: the white page/background is paintable; Fairy Dust sparkles are more visible while remaining soft; the supplied 30-second Jingle Bells MP3 loops across the whole game; gallery source thumbnails are 384px; and the gallery was re-themed to match the splash/coloring pink and purple visual language. The gallery has no final dedicated Figma frame, so it still needs user visual approval in Unity.

Monetization scope remains freemium with ads and one-time full/ad-free unlock plus purchase restore. Free-canvas count, pricing, ad provider/placements, purchase integration and child-privacy/platform compliance are not finalized. No ads or purchases are live. The prototype allows full access; the existing release guard prevents production builds until release requirements are implemented. Do not remove it simply to get a build through.

## Approved creative decisions

- All 30 transparent outline assets and scalable SVG masters are included. Full runtime outlines are 2048px, paint/region maps 1024px and gallery thumbnails 192px.
- The exact ordered palette is defined in `ApprovedArt/approvals.json` and `KidoUI.HexColors`: FF303B, FF5733, FF8A00, FF409C, FF78BE, FF7A6B, FFCE26, B7E833, 68CF36, 22B85D, 009D73, 00C7AE, 27D8F2, 38B5FF, 3978F6, 3D45D8, 8B42EA, C440DD, FFD9BA, F2B887, D99460, B87346, 8D4E32, 593524, FFFFFF, DCE5ED, 8998AC, 242836, FFF3CD, CDB7FF.
- All 15 patterns are approved. The first twelve are tintable; the last three use fixed Christmas colors: Candy Cane Stripes, Christmas Trees & Stars, Christmas Gifts. Original pattern SVGs are retained.
- Glitter concept **D — Fairy Dust** is approved: soft shimmer with subtle white star-like highlights, slowly twinkling inside painted areas. Avoid replacing it with coarse flakes, foil or jewel effects. Motion-off freezes animation while preserving texture.
- Character/canvas animations were initially deferred; do not add a new set of character animations without establishing its scope.

## Figma authority and asset status

Current source: https://www.figma.com/design/ORpZoUySkEUioAZ7bU0ULd/KidoColoring?node-id=0-1

| Screen | Node | Reference size |
| --- | --- | --- |
| Phone splash | 6:36 | 1600 × 900 |
| Tablet splash | 6:46 | 1422 × 1080 |
| Phone coloring | 6:99 | 1600 × 900 |
| Tablet coloring | 6:62 | 1422 × 1067 |

High-fidelity retrieved design contexts are stored in `docs/Figma/6-36.txt`, `6-46.txt`, `6-99.txt`, and `6-62.txt`. They are reference descriptions; the implementation is Unity UGUI, not React.

The latest Figma supersedes an earlier bottom-right-button requirement. Current layout: large white coloring area on the left; green Done at top-left; Undo and Reset near the top-right of the drawing area; a single vertical color strip; a pink right-hand tool rail. Tool order top to bottom: Eraser, Fill, Brush, Glitter, Pattern, Rainbow. The palette scrolls all 30 approved colors with eight visible at a time; pattern mode uses the same strip for 15 patterns.

All **13 required supplied UI textures are now integrated** in `Assets/KidoColoring/Resources/Kido/UI/Figma/`: six tools, three decorations, Logo, Done, Undo and Reset. Their hashes and original filenames are in `ApprovedArt/figma-ui-manifest.json`. The logo is 1154×914 and exactly fits the 577×457 reference ratio. Action images match their reference frame dimensions. Do not request these assets again.

The previous Figma file `tKMLc3YnLj2fez5lIisF98` returned an edit-access error through the connector. View-only export is generally possible; do not repeat the mistaken blanket claim that edit permission is required. The new file was readable, but Figma eventually reported its Starter tool-call limit and image URL/browser downloads returned Site Unavailable in that cloud environment. The user supplied the actual files, resolving the needed asset transfer. No browser login or permission change is required merely to use these local originals.

Known visual follow-up: the supplied splash decorations are already clipped tablet exports (327×403 and 336×284), so the phone crop differs slightly from its reference. Full unclipped or phone-specific exports would permit exact matching. Tool-sheet bounds differ slightly from final-frame crops; inspect orientation, proportions and spacing in Unity. Native circle backgrounds and selected/disabled feedback are implemented in UGUI. Native visual approval is still pending.

## Architecture and files

| Path | Responsibility |
| --- | --- |
| Assets/KidoColoring/Scenes/KidoGame.unity | Entry scene; runtime boot creates UI |
| Runtime/KidoApp.cs | Splash, gallery, coloring, controls, save orchestration and parent settings |
| Runtime/FigmaScreenLayout.cs | Uniform phone/tablet design-space scaling and size-change layout |
| Runtime/KidoUI.cs | UGUI helpers, palette, native shapes, safe area and press response |
| Runtime/DrawingSurface.cs | Pointer input, paint/effect textures, shader compositing and thumbnails |
| Runtime/PaintDocument.cs | Paint state, region-constrained tools, pattern sampling and bounded undo |
| Runtime/PaintStore.cs | Versioned compressed/checksummed save snapshots, recovery and alternating slots |
| Runtime/PatternData.cs | Pattern catalog and sampling |
| Runtime/Content.cs | Canvas catalog and region-map decoding |
| Runtime/Feedback.cs | Generated tones, celebration, sound/motion preferences |
| Runtime/AccessPolicy.cs | Prototype content access |
| Resources/Kido/Shaders/FairyDust.shader | White glitter twinkle and paint/outline composition |
| Editor/KidoSetup.cs | Verification/build menus, imports and production release guard |
| Tests/Editor/PaintTests.cs | Sixteen included native EditMode tests, not yet executed here |

Runtime/Resources/Editor paths above are relative to `Assets/KidoColoring/`.

Art masters are in `ApprovedArt/Canvases` and `ApprovedArt/Patterns`. Runtime catalogs and textures are in `Assets/KidoColoring/Resources/Kido`. Offline tooling is in `tools/`. `Preview/`, older generators and `ArtSource/` are archived early prototype references; they do not represent current game behavior or final artwork.

Memory/performance choices: load one full active artwork, keep separate small thumbnails, non-readable UI/outline textures, release screen-specific UI textures, use a 16 MiB / 24-action tile-undo budget, and update dirty paint textures at most 30 Hz. Texture2D.Apply still uploads the full paint/effect texture; no partial-GPU-upload claim was made. Fairy Dust animates in the shader without per-frame CPU paint writes or readbacks. These are architectural choices, not measured device performance results.

Save continuity: current artwork revision 2 uses `Application.persistentDataPath/KidoPaintings-v2`. The old prototype save directory is preserved. Paint RGBA and effect-mask snapshots are compressed and checksummed into alternating slots with a single background writer. Pause/focus loss/Done flush saves. Thumbnail caches are disposable. There is no cloud save or cross-platform purchase entitlement sync. Do not erase user paintings during testing.

## Verification completed and outstanding

Completed in the prior cloud workspace:

- Five `node --test tools/test-approved-content.cjs` checks passed: catalogs/vector sources; transparent outlines/thumbnails; region maps; approved patterns; unique Unity metadata.
- Original UI files verified byte-for-byte against supplied attachments; dimensions/transparency inspected.
- `git diff --check` passed; no changes to Packages or ProjectSettings during the rebuild.
- ZIP integrity checked. Local Git history retained.

Not completed: C# or shader compilation, running Unity, the sixteen native tests, native UI screenshots, actual touch testing, app lifecycle/disk-full save testing, iOS/Android builds, GPU/memory/thermal measurements. There was no Unity executable, .NET or C# compiler in the cloud workspace. Do not present any of these as already passing.

Region-map review found 22–93 paintable regions per canvas. Some open decorative water/seaweed/branches/snow/road boundaries and very small details need cleanup/usability review. `docs/ArtQA/regions-contact-sheet.png` is a diagnostic label-color image, not a prescribed palette. Approved outlines should not be silently redrawn.

## Recommended next work

1. Confirm local project path, working-tree status, Editor version and available connection. Verify installed iOS/Android modules without installing anything. If already running in Unity, use its existing instance/connection rather than launching conflicting processes.
2. Open `Assets/KidoColoring/Scenes/KidoGame.unity` directly. Avoid **Kido Coloring/2. Configure and open game**, which changes project settings. Compile, capture actual errors, and fix source issues.
3. Run relevant EditMode tests; check startup, gallery, all six tools, 30 colors, 15 patterns, Undo, confirmed Reset, Done and reopen/resume. Inspect Fairy Dust and a motion-off session.
4. Compare phone/tablet rendering against the stored Figma contexts and original user reference; check safe areas, touch targets, scroll behavior and repeated navigation. Confirm per-canvas progress survives app restarts.
5. Profile on actual supported devices and resolve measured memory/performance problems. Then finish art-region cleanup and the remaining menu/monetization/release decisions with the user.

## Git continuation

Branch: `rebuild/approved-art-v2`.

Key prior commits:

- `b91de72`: approved-art/palette/pattern persistence and Fairy Dust core rebuild.
- `62d9f08`: supplied tool icons/decorations and responsive Figma layout.
- `7ebe2e1`: original logo and action artwork replacing placeholders.

This handoff is committed after those changes. Use `git log` for the current exact head. No remote is configured as part of this handoff.

The user's original idea included customer/market analysis of a reference game named Coloring Club. No completed market-analysis deliverable is established by this project; do not invent findings or claim that research has been done. The current immediate priority is local Unity execution and validation.
