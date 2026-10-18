# Kido Coloring

Development planning baseline • 18 October 2026

This local Git repository captures the user's game specification, the reference wireframe, the previously agreed 30-picture catalog, and the work required to reach the first playable build.

**Status: planning and environment verification only. This is not yet a runnable Unity project. No Unity compilation, mobile build, SDK integration, or device test has been performed.**

## Start here

1. Read [the scope](docs/SCOPE.md), which supersedes earlier suggestions about subscription monetization, an ad-free free tier, or required animated characters.
2. Read [the implementation plan](docs/IMPLEMENTATION_PLAN.md) for the proposed navigation, coloring, saving, and commerce architecture.
3. Complete [the Unity environment check](docs/ENVIRONMENT.md). The exact installed Editor patch and mobile build modules must be verified before creating the Unity project.
4. Use [the acceptance checklist](docs/ACCEPTANCE.md) to validate each milestone.

## Contents

| Path | Purpose |
| --- | --- |
| `docs/SCOPE.md` | Requirements baseline and unresolved decisions |
| `docs/IMPLEMENTATION_PLAN.md` | Milestones and proposed implementation behavior |
| `docs/ENVIRONMENT.md` | Actual environment findings and developer-machine verification |
| `docs/ACCEPTANCE.md` | Functional, persistence, purchase, and device checks |
| `content/canvases.json` | Stable IDs and briefs for all 30 planned pictures |
| `references/menu-and-canvas-wireframe.png` | User-provided wireframe, copied unchanged |
| `references/baby-dinosaur-style-sample.png` | Earlier generated style sample; approval and production preparation pending |

## Next milestone

After verifying Unity, create the project with one prepared baby-dinosaur canvas. Implement tap fill, a solid brush, eraser, palette, undo, reset confirmation, Done, save/resume, and a thumbnail reflecting saved work. Establish the two-row continuous catalog structure with labeled placeholders; do not present missing artwork as finished content. Add the other brush effects, polished feedback, and all 30 production assets in later milestones.

The launch scope remains all 30 canvases, animated splash/interaction feedback, ads for the free tier subject to platform requirements, and one non-consumable purchase for full access without ads. Early milestones are implementation order, not a reduction of launch scope.

## Ownership and execution

- The user owns branding decisions, store accounts, prices, business declarations, and physical-device/parent-child testing access.
- The assistant can prepare source, assets, configuration, documentation, and fixes; Editor execution and mobile validation require access to the relevant tools.
- Keep changes in local Git. No remote repository or push is configured.
- Existing art samples are references. Their presence does not mean production-ready coloring masks or finalized rights/asset reviews exist.

