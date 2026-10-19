# Kido Coloring — continuing agent instructions

Read `HANDOFF.md`, then `README.md`, `docs/Figma/INTEGRATION.md`, `docs/REBUILD_STATUS.md`, and `docs/PERFORMANCE.md` before making changes.

- Continue the existing Unity project. The user wants direct edits to the local project used by their Unity Editor. Confirm the actual accessible path and Editor connection; permission granted in chat is not evidence of a live connection.
- The user does not want installations or project configuration changes. Preserve `Packages/` and `ProjectSettings/`. Verify the installed Editor and build modules without modifying them. Do not run the existing **Configure and open game** menu action, which changes settings.
- Preserve local user changes and Git history. Inspect working-tree status before editing or importing this handoff into another checkout.
- Approved assets, palette and patterns are in this project. Do not request the logo, Done, Undo or Reset again: all four are now included and integrated.
- The latest Figma frames take precedence over older UI instructions: green Done top-left, Undo/Reset upper-right within the drawing area, palette and tool rail on the right, no separate Back button. Use the supplied artwork without recreating it.
- Preserve the exact approved 30 colors, all 15 patterns (three Christmas patterns), all 30 transparent artworks and SVG masters, and Fairy Dust with subtle white animated sparkles confined to glitter paint.
- Use the native Unity implementation under `Assets/KidoColoring`. `Preview/` and `ArtSource/` are archived first-play material, not the current implementation or its test oracle.
- Preserve saved artwork and the versioned save format. Do not delete saves or silently map old prototype artwork onto the revised outlines.
- The prior cloud session did not compile or run Unity. Its five passing offline checks are not native test results. Start by compiling and inspecting actual errors, then run relevant native tests and visual checks. Report what was actually run.
- Avoid installing SDKs, enabling ads/purchases, publishing, signing, or removing the existing production-build guard as part of routine UI verification. Commerce decisions remain unresolved.
- Make authorized reversible fixes without repeated confirmation. Ask only when a needed action conflicts with the user's installation/configuration restriction or requires a genuinely unresolved decision.
