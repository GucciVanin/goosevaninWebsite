# Reviewer verdict - O3 (F5-US1 defect and advisories)

Verdict: **accurate**. No defects. Read-only review of the uncommitted diff and untracked files. I re-ran Karma: `npm test -- --watch=false --browsers=ChromeHeadless` gives 165 of 165 SUCCESS, matching the worker. I did not re-run dotnet or `verify.ps1`; their totals (175 and 649) are unverified by me.

## Earlier findings, checked
- **Defect 1, scroll reset: fixed and tested.** `app.config.ts` uses `withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' })`. The test in `app.spec.ts` ("App routing configuration") bootstraps the real `appConfig`, scrolls to 800 px on a 4000 px page, navigates to `/projects` and expects `scrollY` 0. It is meaningful: it uses the production providers, and the worker reports it failing red and failing again with the option set to `disabled`. I did not repeat the mutation. It restores the DOM in `finally`.
- **Advisory 1 (scrollTo branch): covered.** A header "Stories" click on `/projects` ends at `/`, shows `app-home-page`, drops the index page and calls `scrollIntoView` on `#blog`.
- **Advisory 2 (shell modes): covered.** `/contact/verify`, `/verify-email`, `/access-denied` and `/editor` (admin, cookie marker, `/api/auth/me` flushed) assert no header, footer or home page. The editor test cleans up its cookie.
- **Advisory 6 (titles): fixed.** `SiteTitleStrategy` gives `<page> - Gustavo Couto Vanin`, or the site name alone. Routes `projects`, `projects/:slug` and `**` carry titles. `ProjectPage` sets the project title once loaded and `NotFoundPage` sets its own, so the 404-inside-project case is right. `projects.spec.ts` asserts all five states plus the restore on `/`.
- **Advisory 4 (Projects nav item): done as ordered.** `NavigationItem.route` is optional. The template renders a `routerLink` in both the header and the drawer, and the drawer closes. Other items keep `href="#id"` and emit `navigate`. `site-header.spec.ts` covers the link, the closing and that other items still emit. The footer does not use `navItems`, so nothing else changes. The app-level test confirms `href="/projects"` and navigation from `/`.
- **Advisory 7 (`plainSummary` location): done.** It now lives in `shared/plain-summary.ts`, and both users import it. The test moved with it.
- Items left alone as ordered: empty thumbnail alt, soft 404, hairline border. Verified they are unchanged.

## Regressions
None found. The hero "work" scroll is unchanged. The routing, the shell template and the verify additions (`screens.ts`, `30-pages.sh`) are consistent with the earlier review. The architecture text matches the code (shell modes, `route`, scroll config, title strategy, `plainSummary` path).

## Advisories
1. **Title flash on a project page.** The title is "Project - Gustavo Couto Vanin" until the data arrives, then the project title. The strategy writes at navigation end and the page writes after the HTTP response, so the order is safe today. If the API response were ever cached or synchronous, the strategy could overwrite the project title. The test would catch that.
2. **Active state for Projects.** `activeSection` is set to `work` on `/projects`. Going back to `/` through the Projects link leaves it at `work` until a scroll or other click changes it. The worker flagged this. Cosmetic.
3. **Home Projects section has no header entry any more.** The header "Projects" item no longer scrolls to the home preview. This is the decided behaviour; the hero button still reaches `#work`.
4. **Scroll restoration on back** also restores the previous position, which is intended. `anchorScrolling` is enabled, but the header and footer use their own `scrollTo`, so it is for URL fragments only.
5. **Karma 404 warnings** for the fake `/uploads/...png` cover remain (earlier advisory 10, unchanged). The test uses a bogus path; a data URI would remove the noise.
6. Carried over, not an O3 item: a published project is verified by Karma stubs only. Do not mark the story Done until the runtime check with published data is decided.

## Docs
`Docs/architecture.md` matches the diff. The worker's backlog and `project.md` count changes were not re-verified line by line; status should remain In Progress.
