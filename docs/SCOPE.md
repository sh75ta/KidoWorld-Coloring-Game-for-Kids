# Kido Coloring — Requirements baseline

Source: user specification and supplied wireframe, 18 October 2026. This is a faithful structured restatement, with implementation proposals and open decisions explicitly separated below.

## Confirmed requirements

| Attribute | Requirement |
| --- | --- |
| Name | Kido Coloring |
| Audience | Children aged 3–6 |
| Initial markets | US and UK, from the preceding brief |
| Genre | Interactive Coloring Adventure |
| Engine | Unity 6.3; verify installed Editor version before development |
| Version control | Local Git repository |
| Platforms | iOS and Android |
| Devices | Mobile phones and tablets |
| Content | 30 canvases, 10 per category |
| Monetization | Ad-supported free selection; one-time full-access purchase removes ads |

### Content and identity

1. Christmas & Winter — 10 canvases.
2. Animals, Dinosaurs, Fish & Birds — 10 canvases.
3. Vehicles & Big Adventures — 10 canvases.

Each category has a distinct theme and background. The earlier agreed 30-picture list is recorded in `content/canvases.json`; names are normalized for spelling, without changing subjects. Production art remains to be created and reviewed.

### Loading screen

- An animated splash establishes the game's identity with playful animation and sound effects.
- Loading must be smooth and appropriate for both phones and tablets.

### Menu and progress screen

- All 30 canvases are discoverable across the three categories.
- Category tabs/icons appear at the top.
- Thumbnails use two rows with horizontal scrolling (2 × N).
- Horizontal discovery continues across category boundaries.
- The category background changes to match the content being explored.
- Each thumbnail shows saved progress and completion status.
- Opening a previously used canvas restores its applied colors.

### Coloring screen

- Most space belongs to the canvas on the left.
- Brush selection and palette appear on the right.
- Brush set and palette support selecting tools and colors.
- Undo reverses the most recent coloring action.
- Reset clears only the current canvas after confirmation.
- Done saves progress and returns to the menu.
- Brush selection, color choice, coloring actions, and completion receive suitable animation, VFX, and SFX without disrupting drawing.

### Persistence

- Save each canvas automatically during use.
- Retain unfinished and finished work when leaving the canvas.
- Restore saved paint across sessions and app restarts.
- Update menu previews and status after changes.
- Reopening a canvas must never silently clear previous work.

### Commerce

- A selection of canvases is free; the number is not yet decided.
- Free access uses ads subject to child-safety and platform requirements.
- Ad placements remain separate from coloring interactions.
- One payment unlocks all 30 canvases and removes ads.
- Restore eligible purchases after reinstall or on a supported device.
- Free selection, ad placements/provider, price, and purchase flow must be finalized before their implementation.

### Quality

- Discoverable, intuitive controls for ages 3–6.
- Responsive drawing and efficient loading on phones and tablets.
- Optimize textures, animations, VFX, and SFX for memory and performance.
- Configure and validate iOS/Android build modules.
- Test on supported physical devices before release.
- Meet applicable children's privacy, advertising, and purchase requirements.

## Wireframe interpretation

The attached reference is available as `references/menu-and-canvas-wireframe.png`. It shows:

- Three landscape device frames: splash/loading, menu, and coloring.
- Top menu labels for Christmas, Animals, and Vehicles.
- Two rows of thumbnails, with horizontal continuation.
- A large left drawing area; right palette and tool column.
- Tool labels: Eraser, Bucket Fill, Solid Brush, Solid Glitter Brush, Pattern Brush, Rainbow Brush.
- Done at the top right; Reset and Undo at the bottom right.

These six tools are retained in the intended launch backlog. Their brush widths, clipping behavior, patterns, and effect settings remain implementation decisions. The wireframe is a layout reference, not final art or exact tap-target sizing.

## Decisions and proposed defaults

| Item | Status / proposal | Needed by |
| --- | --- | --- |
| Exact Unity patch and modules | Unverified on user's computer | Before creating Editor project |
| Landscape orientation | Proposed from the supplied wireframe; support both landscape directions and safe areas | First layout implementation |
| Minimum OS/device support | Not specified; select using available test devices and verified Editor support | Build configuration |
| Art style | Baby-dinosaur sample available; not yet approved | Production art batch |
| Brush behavior | Propose bucket fill and brush constrained to the region where the stroke starts | Coloring prototype |
| Palette and brush widths | Propose 12 easily distinguished starter colors; finger-tested tool sizing | Coloring prototype |
| Completion semantics | Needs definition; Done must save even incomplete work | Menu progress/completion logic |
| Free selection | Unset; keep catalog access fields null until decided | Access/commerce implementation |
| Pricing | Unset, separate US/UK storefront configuration | Purchase setup |
| Ads | Placement, provider, SDK/version, frequency, and platform eligibility unresolved | SDK implementation |
| Restore scope | Propose same-store/account restoration; cross-store entitlement is not assumed | Commerce design |
| Artwork portability | Local persistence is proposed; reinstall recovery/cloud sync not specified | Persistence policy |
| Animated feedback vs characters | UI/splash/VFX/SFX included; per-character animation remains deferred | Art/animation production |
| Sound and reduced-motion controls | Recommended parent settings, not yet a confirmed requirement | Feedback polish |

## Explicit scope boundaries

- Subscription monetization is superseded by the user's one-time purchase requirement.
- The earlier recommendation for an ad-free free tier is superseded; ads remain conditional on compliant implementation.
- The earlier animated-world concept is not assumed to be required. Splash/UI feedback is required; animating every drawing is not.
- No public gallery, chat, account registration, multiplayer, or live image generation was requested.
- Saving artwork across app restarts is required. Cross-device artwork synchronization is a separate feature, not implied by purchase restoration.

## Definition of success

Children discover themed canvases, color with intuitive tools, receive satisfying feedback, and resume their work without losing progress. The game runs reliably on supported iOS/Android phones and tablets with a clear distinction between free and fully unlocked access.

