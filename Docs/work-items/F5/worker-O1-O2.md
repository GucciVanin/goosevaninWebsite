# Worker report - O1 and O2 (F5-US1)

## What changed
Client (`src/GooseWebsite.Client/src/app/`):
- New `features/projects/public-project-api.ts` (read-only `PublicProjectApi`, `PublicProject`), `projects-index-page.ts` (`/projects`), `project-page.ts` (`/projects/:slug`), `features/not-found/not-found-page.ts`.
- `app.routes.ts`: lazy `projects`, `projects/:slug`, an explicit empty home route, and the last `**` route to `NotFoundPage`.
- `app.ts` / `app.html`: three shell modes (auth/editor: no header or footer; home `/`: the sections; any other URL: routed page between header and footer through the one `router-outlet`). Header/footer links from a routed page go to `/` then scroll (`afterNextRender`). `activeSection` is `work` on `/projects*`.
- `home/categories/categories.ts`: Projects card now has `route: '/projects'` (real href, navigates through the router; still clears the story tag).
- `home/selected-work/*`: previews at most 3 projects (const `previewCount`) and an "All projects" link to `/projects`.

Verification: `verify/browser/helpers/screens.ts` (new `projects` screen: load, axe, 320 px, keyboard on 3 engines), new `verify/browser/tests/projects.spec.ts`, `verify/runtime/30-pages.sh` (`/projects`, an unknown project, unknown URL serve the app).

Docs (O2): `Docs/architecture.md` (diagram, folder table, shell modes, card target), `Docs/product-backlog.md` (F5-US1 status "In Progress", two tasks ticked, evidence; NOT Done), `Docs/project.md` (status row), `Docs/learning-log.md` (one entry).

## Tests added (red first)
Written before the code; first Karma run failed to compile (missing `projects-index-page`, `project-page`, `not-found-page`), i.e. red because nothing existed. Then the behaviour failures fixed in the code or the test setup:
- `features/projects/projects.spec.ts`: index (two projects, compact/unframed, empty, loading, failure), detail (order, external links, optional parts, 404 -> not-found, 500), NotFoundPage, routes (lazy pages + wildcard).
- `app.spec.ts`: shell modes for `/projects`, unknown URL, `/`, `/sign-in`.
- `landing.spec.ts` updated (Projects card -> `/projects`, router navigation test); `selected-work.spec.ts` (3 previews, All projects link).
- Browser: `projects.spec.ts` (direct load, 375/1280 px no overflow, axe, for empty `/projects` and not-found `/projects/nope`), new `projects` screen.
Red-then-fixed during development: detail tests initially failed because the Markdown module was loaded by dynamic `import()` (not awaited by `whenStable`); I imported the renderer statically (the page is its own lazy chunk). The app-shell tests initially failed because `RouterTestingHarness` renders into its own outlet; they now use `Router.navigateByUrl`. Unframed test first asserted no top border; changed to no side/bottom border, radius, or fill (a hairline above each entry separates them).

## Final totals
- Karma: 156 of 156 pass.
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 175 passed, 0 failed (client built first).
- `./scripts/verify.ps1 -SkipCi`: 640 passed, 0 failed, 0 skipped, 0 known (162 browser tests, `known-issues.json` still `[]`).

## Deviations / uncertainties
- Header "Projects" nav item still scrolls to the home `#work` section (O1 only named the card, panel and section); say if it should go to `/projects`.
- Index thumbnail uses `alt=""` (decorative; the title link is beside it). Detail cover uses the real alt text.
- Contribution is rendered as plain text (matches the editor preview); overview as safe Markdown (`markdown-it`, raw HTML off).
- `ProjectsIndexPage` imports `plainSummary` from `features/home/selected-work` (a cross-feature import; could move to `shared/`).
- The not-found screen's expected API 404 is filtered by exact id in the browser spec; the stack publishes nothing, so a detail page with real content was verified only by Karma, not in a browser.
- F5-US1 task 3 (seed draft+published data) left unticked; noted in the backlog.
- Karma logs harmless 404 warnings for the fake cover image path.
- No git write commands used. `src/GooseWebsite.Api/wwwroot` was rebuilt (ignored by git).
