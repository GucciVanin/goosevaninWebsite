# Worker report - O3 (reviewer defect and advisories on O1)

## What changed
Client (`src/GooseWebsite.Client/src/app/`):
- `app.config.ts`: `provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }))` (defect 1) and `SiteTitleStrategy` as the `TitleStrategy`.
- New `shared/site-title-strategy.ts`: `<page> - Gustavo Couto Vanin`, site name alone for routes without a title. Routes `projects` ("Projects"), `projects/:slug` ("Project"), `**` ("Page not found") carry a `title`. `ProjectPage` sets "<project title> - ..." once loaded; `NotFoundPage` sets its own title (it also appears inside the project page).
- `shared/models/navigation-item.ts`: optional `route`. `app.ts`: Projects item has `route: '/projects'`. `layout/site-header/site-header.html`: an item with a route is a `routerLink` (header and drawer; the drawer closes); other items keep scrolling. The hero's "work" scroll is unchanged.
- `plainSummary` moved to `shared/plain-summary.ts` (imports updated in `selected-work.ts`, `projects-index-page.ts`); its test moved to `shared/plain-summary.spec.ts`.
- Docs: `Docs/architecture.md` (nav route, scroll, titles, plainSummary location), backlog evidence counts, `Docs/project.md` row count.

## Red-first tests
Written before the code; the first Karma run failed to compile (missing `plain-summary`, `site-title-strategy`). Behaviour was then checked red/green:
- `app.spec.ts`: scroll reset (bootstraps the real `appConfig`, because the router's scroll handling only starts with a bootstrapped app; failing under a TestBed first, and failing again when I set `scrollPositionRestoration: 'disabled'` as a mutation check); header link on `/projects` goes home and calls `scrollIntoView` on `#blog`; header Projects item on `/` goes to `/projects`; shell-free `/contact/verify`, `/verify-email`, `/access-denied`, `/editor` (admin).
- `site-header.spec.ts`: routed item is a real link in header and menu, emits nothing, closes the menu; other items still emit.
- `projects.spec.ts`: titles for the index, project (placeholder then project title), 404 inside the project page, wildcard, and the site title restored on `/`.
- `shared/plain-summary.spec.ts` (moved test plus a Markdown case).

## Final totals
- Karma: 165 of 165 pass.
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 175 passed, 0 failed (client rebuilt first).
- `./scripts/verify.ps1 -SkipCi`: 649 passed, 0 failed, 0 skipped, 0 known (162 browser tests; `known-issues.json` still empty). No contact message was submitted.

## Left as decided for Gustavo (not changed)
Thumbnail empty alt, the soft 404 (HTTP 200 for unknown URLs), the hairline border between entries.

## Doubts
- A project page titled "Project - ..." shows until the data arrives, then the project title.
- The header Projects item is now a page link, so on `/projects` and back to home `activeSection` stays `work` until another item is chosen (existing behaviour of `activeSection`).
- Scroll restoration also restores the position on the back button (intended).
- The footer has no Projects link, so nothing else needed changing there.
- No git write commands used; story not marked Done.
