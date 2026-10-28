# Reviewer verdict - O5

Verdict: ACCURATE. The worker report matches the diff. No blocking defects. Karma re-run by the Reviewer: 185 of 185 pass.

## Checks
1. Scroll test: deterministic. It sets inline `scrollBehavior = 'auto'` on `documentElement` (inline wins over the stylesheet's `smooth`), restores it in `finally`, still scrolls to 800, navigates for real through the bootstrapped config and asserts the real `window.scrollY` is 0. The bounded wait loop is kept and harmless. Not a tautology: the worker's mutation (`scrollPositionRestoration: 'disabled'`) turned it red. Behaviour untouched.
2. Header clearance: `.gcv-section--page` (padding-top 128 px via `--gcv-header-clearance`) is on the `main` of projects index, project page, stories index, story page and not-found (the project/story pages' not-found case renders `app-not-found-page`, which carries it). Old padding was `--gcv-space-8` = 72 px; header bottom is about 108 px, so the first child sat about 36 px under the header. The new Playwright check (`first child of main` top minus `.gcv-bar` bottom, must be >= 0) would therefore fail with the old padding, by reasoning from the CSS and the worker's measured 72 vs 108. Weakness: the red state was not run (worker says so), and the check covers only the empty-index and not-found pages of projects plus the stories states, not a loaded project or story at width.
3. headingOffset: markdown-it core rule rewrites `heading_open`/`heading_close` tags, capped with `Math.min(6, ...)`, default 0. Editor previews (`project-form-page`, `story-form-page`) call `renderMarkdown(x)` without options, so unaffected. `html: false` is unchanged and only tags of heading tokens change, so output stays safe; the existing script and `javascript:` link test still passes. Tests cover offset, h6 cap, one `h1` on story and project pages.
4. Reading styles: `.gcv-reading` uses tokens only (leading, text sizes, spaces, bg-1/bg-2, line, cyan, text-muted, font vars). No raw colours or sizes except `line-height: 1.8` and `2px` border (minor).
5. Placeholder: "Search by title or tag", neutral.

## Defects
None blocking.

## Advisories
- A1: `gcv-reading` is applied to the story body only. The project overview (also Markdown, also offset) uses `.overview gcv-preview` with no blockquote, code or heading styling, so a project overview with a quote or code block looks unstyled. Consider adding `gcv-reading` there (check `.overview` colour/margin overrides first).
- A2: `stories.spec.ts` lines 225-229 contain duplicated comment and duplicated `expect(h1 count).toBe(1)` lines; remove the duplicates.
- A3: The 128 px token is a fixed guess at the header height; only 375 and 1280 px are checked. Acceptable; the browser test will catch a taller header.
- A4: Not viewed in a real browser with a published story (stack publishes none); the reading look is unverified visually.

## Duplicate heading text on /projects and /stories
Cause confirmed from the templates: `<p class="gcv-section__label">Projects</p>` followed by `<h1>Projects</h1>` (same for Stories). Both are real text, so a screen reader reads the word twice, and sighted users see it twice (small magenta label, then the big title). Not an accessibility failure and not a functional defect, but redundant. Decision: low-severity design advisory, not a defect for O5. Suggested fix if wanted: make the label a different, useful word (for example "Index" or a count) or mark it `aria-hidden="true"` on those two pages. Other pages' labels differ from their h1 (404 / Page not found), and the home sections follow the same label-plus-heading pattern, so this is consistent with the site's style.
