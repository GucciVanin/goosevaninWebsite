# Worker report - O5 (Testing findings on O4)

## What changed
Client (`src/GooseWebsite.Client/src/app/`):
1. Flaky scroll test (`app.spec.ts`, "scrolls to the top ..."): the test now sets `document.documentElement.style.scrollBehavior = 'auto'` for its duration (restored in `finally`), so the site's `scroll-behavior: smooth` cannot delay the scroll. Behaviour is unchanged. It still reads the real `window.scrollY`.
2. Header clearance: new token `--gcv-header-clearance: 128px` (`shared/styles/tokens.scss`; the fixed header measures about 108 px) and class `.gcv-section--page` (`shared/styles/components.scss`). Applied to the `main` of `/projects`, `/projects/:slug`, `/stories`, `/stories/:slug` and the not-found page.
3. Single `h1`: `features/editor/markdown.ts` gains `renderMarkdown(source, { headingOffset })` (a markdown-it core rule, capped at h6). The story and project pages pass `headingOffset: 1`, so `#` becomes `h2`. The editor preview keeps the author's levels (default 0).
4. Reading column: story body has class `gcv-reading` (global rules in `components.scss`): tight heading line height (`--gcv-leading-tight`) and heading sizes and margins, blockquote with a cyan left rule and muted text, inline code and code block with token backgrounds, borders and scrolling. Tokens only.
5. Search placeholder on `/stories` is "Search by title or tag".
Docs: `Docs/architecture.md` (features/stories row: heading offset, clearance class and token, `gcv-reading`).

## Tests
- Karma: `markdown.spec.ts` (heading offset, stops at h6), `stories.spec.ts` (body `#` gives one `h1` and an `h2`; placeholder), `projects.spec.ts` (overview `#` gives one `h1`). The heading tests failed before the code change (the `StoryPage` one was seen red: `## Heading` became `h3`; the others were written together with the helper).
- Browser: `verify/browser/tests/projects.spec.ts` and `stories.spec.ts` gain, for each page state, "the first line sits below the fixed header" at 375 and 1280 px (first child of `main` top >= `.gcv-bar` bottom). I did not run these against the old padding, so their red state is not shown; the measured values before the change were label top 72 px against header bottom 108 px.
- Flaky test proof: 10 consecutive full Karma runs (`npm test -- --watch=false --browsers=ChromeHeadless`, output to files): 10 of 10 `TOTAL: 185 SUCCESS`. Mutation: with `scrollPositionRestoration: 'disabled'` in `app.config.ts` the test fails (1 FAILED, 184 SUCCESS); restored.

## Final totals
- Karma: 185 of 185 pass.
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (client rebuilt first): 175 passed, 0 failed.
- `./scripts/verify.ps1 -SkipCi`: 750 passed, 0 failed, 0 skipped, 0 known (240 browser tests; `known-issues.json` still empty), exit 0. No contact message was submitted.

## Doubts
- Finding 5 of the Testing report (page heading text appearing twice in the page text) was not in O5 and was not investigated; most likely the CSS uppercase text of the label plus the heading.
- The fixed 128 px token assumes the header stays about 108 px; the browser test checks 375 and 1280 px only.
- `gcv-reading` is only seen in Karma as a class; its look was not viewed in a browser with a real story (the stack publishes none).
- No git write commands used. Stories not marked Done.
