# Reviewer verdict - O1 and O2 (F5-US1)

Verdict: **accurate**, with one defect to fix before acceptance and several advisories. Read-only review of the uncommitted diff and untracked files. I did not re-run the test suites; the totals in the worker report are unverified by me.

## What I checked and found sound
- Drafts: the client reads only `GET /api/projects` and `/api/projects/{slug}`. The API test `ProjectEditorApiTests` asserts a draft returns 404 publicly (lines 86, 105, 117), and the client does no filtering of its own, so it cannot leak a draft. The Karma stub cannot prove draft exclusion, and the worker does not claim it can.
- Detail order: title, overview, contribution, technologies, cover image, links. The template matches AC2 and the Karma test compares DOM positions.
- Links: demo and source use `target="_blank"`, `rel="noopener noreferrer"`, underlined `gcv-link` styling, and an "(opens in a new tab)" text plus an arrow. They are distinguishable from the technology chips. The API requires http(s) for demo and source URLs (`ProjectAuthoringService.cs:135`), and Angular sanitizes `[href]`. The overview is rendered with markdown-it with raw HTML off, then sanitized again.
- Not-found: the API 404 on an unknown slug shows `NotFoundPage` in place. The wildcard route covers unknown URLs. Both are tested in Karma and the browser spec.
- Shell modes: `isAuthenticationRoute` is unchanged, so sign-in, verify-email, contact/verify, access-denied and the editor still show no header or footer. Home (`/`) keeps its sections. Order of the route array is correct (`**` last).
- Content: no new claims about Gustavo. The only added text is "What Gustavo has built, and the part he played in each.", which is neutral. The mention of "Gustavo's contribution" is a field label.
- Docs match the code (architecture.md shell modes, folder table, card target; backlog evidence; status stays In Progress, not Done; project.md row; learning-log entry).

## Defects (ranked)
1. **No scroll reset on route changes (medium, user-visible).** `app.config.ts` uses `provideRouter(routes)` with no `withInViewTransitions`/`withRouterConfig` and no `scrollPositionRestoration: 'enabled'`. Clicking the Projects card, "All projects", or an index entry from a scrolled position keeps the previous scroll offset, so the visitor can land at the middle or bottom of the new (shorter) page, and the footer's "back" links land mid-page. Neither the Karma nor the browser tests would catch it (they load pages directly). Fix: enable scroll restoration, and add a test that navigates from a scrolled home to `/projects`. This is within the "update the shell" part of O1.

## Advisories (ranked)
1. **Header/footer navigation from a routed page is untested.** The `scrollTo` branch that goes to `/` and then scrolls via `afterNextRender` has no test. The home sections load their data after rendering, so the target offset can shift. At minimum add an `app.spec.ts` case that clicks a header link on `/projects` and expects the URL to become `/` with the home sections present.
2. **Shell-mode tests cover `/projects`, unknown URL, `/` and `/sign-in` only.** `/editor`, `/contact/verify`, `/verify-email` and `/access-denied` are not asserted to stay shell-free. The logic is unchanged, so risk is low, but the order text says these routes must not break.
3. **Unknown nested paths under authentication routes** (for example `/editor/nope`, `/sign-in/x`) now fall to the wildcard and show `NotFoundPage` without a header or footer, because `/editor/*` counts as an authentication route. It works, but the page has no shell and its "home" link is the only exit. Previously these were blank. Acceptable; note it.
4. **Header "Projects" nav item** still scrolls to the home `#work` preview rather than `/projects` (the worker raised this). Product decision for Gustavo; with only three previews it is arguably the wrong target now.
5. **Unknown URLs return HTTP 200** (the SPA shell) for `/projects/nope` and any unknown path. This matches the story as written ("the site's not-found state") and the runtime check asserts 200, but it is a soft 404 for search engines. Record it as a known property.
6. **No document title** is set on the new pages (the tab title stays the same on `/projects`, project pages and not-found). Minor accessibility/SEO gap (WCAG 2.4.2 for page titles); axe did not flag it.
7. **Index thumbnail has `alt=""`** with the title link beside it. Defensible (decorative), but the cover alt text is available and could be used. Cross-feature import of `plainSummary` from `features/home/selected-work` should move to `shared/`, as the worker noted.
8. **A real published project is verified only by Karma** (stub), not in a browser or in the container. The worker states this. F5-US1 task 3 (seed draft and published data) is correctly left unticked; the story must not be marked Done until a runtime check with a published project is decided.
9. **Index "unframed" test** accepts a hairline top border on each entry; the story says "compact and unframed". A separator line is reasonable, but Gustavo should confirm at review.
10. **Karma 404 warnings** for the fake cover path are noise; use a data URI or none.

## Order-to-work mapping
- O1: all listed items present (routes, lazy components, states, order, links, not-found, card target, home preview of 3 plus "All projects", shell, Karma, Playwright, runtime check). The one gap is the scroll behavior noted above.
- O2: architecture, backlog evidence, project.md row and learning-log are present and consistent with the code. The backlog correctly says "In Progress".
