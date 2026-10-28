# Security review - O3

Verdict: PASS. No blocking or medium findings.

## Checked
- Document title (`shared/site-title-strategy.ts`, `project-page.ts`, `not-found-page.ts`): titles are set only through Angular's `Title.setTitle`, which writes `document.title` as plain text. The project title (API data, owner-authored) goes in through a template string and is never parsed as HTML. `grep` finds no `document.title`, `innerHTML` or `bypassSecurityTrust*` in the O3 files. Result: no injection path. PASS.
- Router scroll config (`withInMemoryScrolling`): only scroll position and fragment scrolling; no data or auth effect. Anchor scrolling takes the fragment from the URL and looks up an element id (no HTML or script evaluation). PASS.
- Nav `routerLink` (`site-header.html`, `app.ts`): the route values are hard-coded constants in `navItems` (`/projects`), not request or user data. The project index link uses array form `['/projects', entry.slug]`, which Angular encodes. No `javascript:` or open-redirect vector. PASS.
- `scrollTo` fallback: `navigateByUrl('/')` with a constant URL; element id comes from fixed nav items. PASS.
- `plainSummary` move: pure string function, output rendered through interpolation (text), not `innerHTML`. Move changed no behaviour. PASS.
- No secrets, logging, storage, or authorization changes. Editor/auth routes untouched and still listed as shell-free.

## Findings
- INFO: pre-existing `[innerHTML]` sites (project overview, blog detail, editor previews) rely on Angular's sanitizer through `renderMarkdown`; not changed by O3 and out of its scope.
- INFO: wildcard route returns a soft 404 (HTTP 200); already recorded as a decision for Gustavo. No security impact.
