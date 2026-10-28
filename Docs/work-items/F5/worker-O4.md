# Worker report - O4 (F5-US2)

## What changed
Client (`src/GooseWebsite.Client/src/app/`):
- New `features/stories/`: `public-story-api.ts` (read-only `PublicStoryApi`: `GET /api/blog?pageSize=`, `GET /api/blog/{slug}`), `stories-index-page.ts` (`/stories`: compact unframed entries, search, category buttons, `?tag=` filter, loading/empty/failure/no-tag-match states in words), `story-page.ts` (`/stories/:slug`: title, optional cover with alt, safe Markdown body, then category, date and tags; 404 shows `NotFoundPage`; document title is the story title).
- `app.routes.ts`: lazy `stories` ("Stories") and `stories/:slug` ("Story"), ahead of `**`.
- `features/blog/blog-page.*`: now the home Stories preview (latest 3, `pageSize=3`, entries are links to `/stories/<slug>`, "All stories" link; the in-page reading view, search and filters are gone).
- `features/home/categories/categories.ts`: cards are plain links built from the router (`/projects`, `/stories`, `/stories?tag=competition`); the `navigate` output and the `StoryFilter` use are gone.
- `shared/story-filter.ts` deleted. `layout/site-header/site-header.ts` no longer clears a tag; `app.ts` Stories nav item has `route: '/stories'`, and the header marks Stories current on `/stories*`.
- Reviewer minor (Projects active on unknown `/projects/<x>`): new `shared/page-state.ts`; `NotFoundPage` sets `notFound` while it is shown and `App.activeSection` is empty then. Covers unknown stories too.
- `shared/story-date.ts` (UTC `22 Sep 2026`, publication date or creation date).
- Editor: `story-form-page.ts` shows a `.publish-reminder` paragraph above the buttons while a story is not published (personal or sensitive details; informs only; Publish stays an explicit click).
Verification: `verify/browser/tests/stories.spec.ts` (new), `stories` screen in `verify/browser/helpers/screens.ts`, `/stories`, `/stories?tag=competition`, `/stories/<unknown>` in `verify/runtime/30-pages.sh`.
Docs: `Docs/architecture.md` (diagram, features/stories, routes, shell, StoryFilter removal, reminder), `Docs/product-backlog.md` (F5-US2 In Progress, tasks, evidence; NOT Done), `Docs/project.md` row.

## Red-first tests
Written before the code. First Karma run failed to compile (`Cannot find module './stories-index-page'` and `'./story-page'`), i.e. red because nothing existed. After implementation all 182 passed on the first run, so I ran a mutation check: removing `notFound.set(true)` and the reminder `@if` made exactly four tests fail (header highlight on unknown project/story; the three reminder tests); both restored.
- `features/stories/stories.spec.ts` (index: two stories, none, loading, failure, search, category, tag exact/case-insensitive, "Show all stories", no-match message, tag change in the address; detail: order, cover alt, safe Markdown, 404 -> not-found, 500, title; routes).
- `features/blog/blog-page.spec.ts` rewritten for the preview; `landing.spec.ts` (card hrefs and navigation); `site-header.spec.ts` (Stories item is a `/stories` link in header and menu; the old tag-clearing test is removed with `StoryFilter`); `app.spec.ts` (shell on `/stories`, header highlight rules, Stories item from home; the existing home-bound link test now uses About); `story-form-page.spec.ts` (3 reminder tests).
- Browser: `stories.spec.ts` for `/stories`, `/stories?tag=competition` and `/stories/nope` (direct load with header/footer, 375 and 1280 px no overflow, axe) plus the `stories` screen (load, axe, 320 px, keyboard on 3 engines).

## Final totals
- Karma: 182 of 182 pass.
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 175 passed, 0 failed (client rebuilt first).
- `./scripts/verify.ps1 -SkipCi`: 717 passed, 0 failed, 0 skipped, 0 known (210 browser tests; `known-issues.json` still `[]`). No contact message was submitted.

## Doubts
- A story page with real content was verified only by Karma (the stack publishes none), as with projects. F5-US2 task 3 (seed draft and published stories) stays unticked.
- The home preview asks the API for `pageSize=3` and the index for `pageSize=50` (the API maximum); more than 50 stories would need paging.
- The dates are now UTC and use "Sep" (deterministic, independent of browser ICU), not the earlier locale format; the old "5 min" read-time placeholder is gone from the list and page (it was a constant, not a measure).
- Tag chips are plain text, not links to `?tag=`.
- The soft 404 (HTTP 200 for unknown story addresses) is the same decided property as for projects.
- Test 5 (tone of a real story) needs Gustavo's eyes.
- No git write commands used (I mistakenly included a `git rm --cached` in one shell command; the permission layer refused the whole command, so nothing ran; I removed `story-filter.ts` with a plain file delete). Story not marked Done.
