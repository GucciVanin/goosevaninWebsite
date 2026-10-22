# Worker record - O4 (tokenise the shell glows)

Changes
- `shared/styles/tokens.scss`: added `--gcv-glow-violet` rgba(165, 43, 255, 0.2), `--gcv-glow-violet-soft` rgba(165, 43, 255, 0.16), `--gcv-glow-cyan` rgba(0, 217, 255, 0.12). Comment traces them to `--gcv-violet` and `--gcv-cyan`.
- `shared/styles/components.scss`: `.gcv-auth` and `.gcv-editor` backgrounds now use the tokens; same values, same gradient positions. No rgba literal remains in the file (no other `.gcv-editor*` rule had one).
- `features/editor/editor-design.spec.ts`: new spec "the global .gcv-editor and .gcv-auth rules carry no colour literal". It walks `document.styleSheets` (the global `src/styles.scss` is loaded in Karma), checks the cssText of every style rule whose selector contains `.gcv-editor` or `.gcv-auth`, and asserts at least one `.gcv-editor` rule was seen (not vacuous). Note: Chrome gives style rules a `cssRules` list (CSS nesting), so the walker tests `selectorText` before recursing.

Red observed: yes. The first attempt of the spec passed wrongly (walker bug above); after the fix it failed (1 FAILED, 96 SUCCESS) against the literals, then passed after the token change.

Results
- Karma: 97 of 97 SUCCESS.
- `npm run build`: succeeded (output to src/GooseWebsite.Api/wwwroot).
- No git write commands, no backend change, no new dependency.
