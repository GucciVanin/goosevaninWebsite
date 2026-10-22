# Worker record - F9-US5 O1, O2

## Files changed (all under `src/GooseWebsite.Client/src/app`)
- `shared/styles/components.scss`: new shared editor-shell classes (token values only): `gcv-editor`, `gcv-editor__top`, `gcv-editor__body(--form)`, `gcv-actions`, `gcv-btn--danger`, `gcv-toggle`, `gcv-chip--published`, `gcv-chip--state`, `gcv-list*`, `gcv-filters`, `gcv-banner`, `gcv-preview`, `gcv-tech`, `gcv-problems`. File normalised to CRLF. The page-background gradient reuses the rgba pattern already in `.gcv-auth`.
- `features/editor/editor-page.ts` (O1), `pages/story-form-page.ts`, `pages/project-form-page.ts`, `components/image-controls.ts` (O2): templates and styles re-skinned; component styles reduced to `:host`/image sizing with `--gcv-*` tokens. No legacy alias remains in these files (grep checked). Class code (signals, API calls, routes, guard, CSRF, delete confirmation, save-never-publishes) untouched, except `BrandLogo` added to `imports`.

## What changed per order
- O1: logo header and "Back to Gustavo's site"; `gcv-heading`/`gcv-eyebrow`; Stories/Projects switch is `.gcv-toggle` (still `aria-pressed`; the pressed button also shows a check mark via CSS, so not colour alone); search and status are `gcv-field`; rows are `li.gcv-card` (class `story-list` kept) with chip "State: Draft"/"State: Published"; delete confirmation is an alert-style text plus "Yes, delete" (danger) and "Keep it" buttons; New/Account links are `gcv-btn` (New solid).
- O2: story and project forms use the same shell; status is a visible badge `p.gcv-chip[role=status]` "Status: Draft" / "Status: Published on ..."; Save solid, Preview/Publish/Return to draft plain `gcv-btn` (Publish distinguished by its label); banner `gcv-banner` keeps "Draft preview: this story/project is not public"; errors are `gcv-status--alert` with `role="alert"`; inputs/textareas wrapped in `gcv-field`; image controls fieldset uses `gcv-field`, `gcv-btn`, `gcv-status` alert/status. Actions and rows use flex-wrap; bodies have `min-width: 0`, titles `overflow-wrap: anywhere`.

## Tests
Specs written first, then the markup. Honest note: I wrote the specs and the implementation before the first Karma run, so I did not observe the red state separately.
Added: editor-page (4: shell and toggle aria-pressed, chip labels and cards, delete confirmation, accessible names), story form (5: badge draft/published, button variants, fields and preview banner, alert style), project form (1), image controls (1). Existing specs unchanged.

## Results
- Karma (ChromeHeadlessNoSandbox): 86 of 86 SUCCESS.
- `npm run build`: succeeds, no errors.
- Not done (O3): the 320 px measurement in the browser.
