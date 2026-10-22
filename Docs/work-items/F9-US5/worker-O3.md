# Worker record - F9-US5 O3 (AC2 verification and cleanup)

## New spec
`src/GooseWebsite.Client/src/app/features/editor/editor-design.spec.ts` (10 specs):
- For EditorPage, StoryFormPage, ProjectFormPage and ImageControls: no legacy alias (`--paper`, `--accent`, `--accent-soft`, `--ink`, `--muted`, `--line`, `--line-strong`, `--mono`, `--display`, `--body`, `--sage`, `--sand`) and no hex/rgb/hsl literal in the inline styles, and no legacy alias in the template. Karma does not serve `.ts` as text, so the check reads the compiled component definition (`ɵcmp.styles`, `ɵcmp.template.toString()`, `ɵcmp.consts`); the rendered markup of all three pages is also checked for legacy aliases and colour literals in `style` attributes. A runtime script check was not needed.
- Draft and Published state text on the workspace rows, the story form badge and the project form badge.
- Every button, link, input, textarea and select on the three pages has an accessible name (text, aria-label, or a `label[for]`).
- The Stories/Projects toggle exposes `aria-pressed` on both buttons and moves it on click.

Red step: observed for the style check by mutation (temporarily added `color: var(--accent); background: #fff` to the EditorPage styles: the EditorPage spec failed, 1 FAILED of 10; reverted). The other specs assert behaviour that O1/O2 already implemented, so they passed on first run (no separate red observed).

## Cleanup
- editor-page.spec: accessible-name spec renamed to "...and uses the shared button style" (no longer claims alias/colour checks, which editor-design.spec covers).
- story-form-page.spec: "labelled badge ... for draft and for published" renamed to the draft-only claim it asserts (published is already its own spec).
- image-controls.spec: now asserts, for cover-file, cover-alt, inline-alt, inline-file, that each input is inside `.gcv-field`, has a `label[for]` in the same field with text, and that the "Remove the cover image" button appears once a cover exists.
- Nested `<header class="gcv-editor__body">` (and the two nested `<section class="gcv-editor__body">`) replaced by the new `gcv-editor__group` class (no top margin or max-width duplication) in editor-page, story form and project form.
- Removed the unused `.tech, .gcv-tech { margin: 0 }` from the story form (left in the project form, which uses it for the technologies list; `.gcv-tech` already has margin 0 in components.scss).

## 320 px measurement
Method: built-in browser, viewport emulated at 320x700; `npm start` dev server (client-dev, port 4300); after the app loaded, `XMLHttpRequest` and `fetch` were replaced in the page with a stub (auth/me returns an administrator; stories/projects lists and story/project 1 return fixtures with a very long unbroken title, excerpt, code block, link and technology name), then the router was driven with `history.pushState` + `popstate`, so the real guard and components ran. No backend, no real session. Values are innerWidth / `document.documentElement.scrollWidth` / rightmost element edge:
- /editor stories 320/320/304; with delete confirmation open on the long-title row 320/320/304; projects 320/320/304
- /editor/stories/new 320/320/304; preview 320/320/304
- /editor/projects/new 320/320/304; preview 320/320/304
- /editor/stories/1 (long published) edit 320/320/304, preview 320/320/309
- /editor/projects/1 (long) edit 320/320/304, preview 320/320/309

## Overflow found and fixed (components.scss, editor rules only, tokens untouched)
Before the fixes: delete confirmation with a long title measured 589; previews with long unbroken text/code measured 510 and 1845; a long technology chip measured 597. Added: `.gcv-editor__group` (single-column `minmax(0,1fr)` grid, `overflow-wrap: anywhere` on children), `.gcv-editor .gcv-status` / `.gcv-lede` / `.gcv-preview *` / `h3` / `.gcv-chip` wrap (chip `white-space: normal`), `.gcv-preview pre` `pre-wrap`. After: all numbers above.

## Results
- Karma (ChromeHeadlessNoSandbox): 96 of 96 SUCCESS.
- `npm run build`: succeeds.
- Not measured: a real signed-in session through the container stack; the viewport was emulated, not a physical phone. Keyboard focus visibility was not re-measured (shared focus style unchanged).
