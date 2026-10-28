# Reviewer verdict - O4 (F5-US2)

**Verdict: worker-O4.md is accurate. No defects. Four advisories.** I read the code against the F5-US2 tests, `decisions.md` (D2, D5, F5-AC3, F5-AC4) and the order. I re-ran Karma myself: 182 of 182 pass (matches the report). I did not re-run `dotnet test` or `verify.ps1`; those totals are taken from the report.

## Checks

| Check | Result |
| --- | --- |
| Drafts never shown | OK. `GetPublishedAsync` filters `IsPublished`. `GetPublishedBySlugAsync` filters `IsPublished`, so a draft slug returns 404 even for an administrator. The client only calls these two endpoints (`PublicStoryApi`, read-only). |
| Detail layout | OK. `<h1>` title, then the optional cover (with `coverImageAlt`), then the Markdown body (`renderMarkdown`, the editor's safe renderer, via `innerHTML`), then category, `<time>` and tags in a `.meta` block after the text. The column is 70ch. Unknown slug (404) shows `NotFoundPage`; other errors show the failure message. The document title is the story title. |
| Tag filter | OK. `?tag=` is read from the query map. The match is case-insensitive and exact, and it combines with search and category. The "Showing stories tagged #x" line with a "Show all stories" link (plain `/stories`) is correct. "No stories tagged #x yet." appears only when stories exist but none match. "No story found. Try another search." appears otherwise. An empty site shows the "will appear" text. |
| Home preview | OK. Latest 3 via `pageSize=3` (the API orders by publication date, newest first). Each entry is a `routerLink` to `/stories/<slug>`, plus an "All stories" link. The in-page reading view is gone. |
| Link targets | OK. Cards: `/projects`, `/stories`, `/stories?tag=competition` (real `href`s from `serializeUrl`, click handled by the router). Header and drawer: the Stories nav item has `route: '/stories'` and renders as `routerLink`. The footer has no Stories link, only home and contact, so nothing is dangling. The `#blog` anchor still exists on the home page but nothing targets it now (harmless). |
| StoryFilter removed | OK. No reference remains in `src`, `verify`, `tests` or the live docs. The only hits are `dist/test-out` build output, past work-item records, and the architecture and backlog sentences that describe the removal. |
| Editor reminder | OK. `.publish-reminder` is a plain paragraph. It is not an alert, has no controls, and shows only when `!isPublished()` and the load did not fail. The Publish button is untouched and `publish()` is still only called by its click. Three Karma tests cover this. |
| Header highlight | OK. `PageState.notFound` is set by `NotFoundPage` and cleared on destroy, so `/projects/x` and `/stories/x` unknown pages mark no section current. This also resolves the earlier Testing minor. |
| Dates | OK. `storyDate` formats UTC as `22 Sep 2026`, so it does not depend on the browser's locale. It falls back to `createdAtUtc`, and returns an empty string for an invalid date. The editor stores the date as `T00:00:00Z`, so the UTC day matches what Gustavo picked. |
| Docs match code | OK. The architecture, backlog (In Progress, not Done, task 3 left unticked, honest "not yet verified") and project.md row agree with the code. The "5 min" read-time removal is stated in the worker report. |
| Invented content | None. The leads are "Stories Gustavo has published." and the neutral empty/failure texts. There is no hobby list, game imagery or invented credentials. The search placeholder "try systems or tools" is carried over from the previous home section, not new. |

## pageSize=50 cap
The API really does cap: `BlogController` uses `Math.Clamp(pageSize, 1, 50)`, so any larger request returns 50. Past 50 published stories, the older ones are silently missing from `/stories`, and the search, category and tag filters cannot find them either, because they run in the client. **Advisory, not a defect today.** The site publishes nothing yet, and 50 stories is far beyond the plan. It does contradict AC1 ("every published story appears") above 50, so it must be recorded as a known limit (the worker's doubt covers it) and fixed with paging, or by server-side search, category and tag, when the count nears about 30.

## Advisories, ranked
1. The 50-story ceiling above. Record it in the backlog or architecture known gaps; no code change needed for O4.
2. No story with real content has been seen in a browser (the stack publishes none). The detail page and the home preview with entries are covered by Karma only. Before marking Done, publish a test story in the container and look at the page, including the tag chips and the cover image. The unticked task 3 already says this.
3. Tag chips are plain text, so a reader cannot click `#competition` to filter. This matches the order, which did not ask for it, and it is a cheap later improvement.
4. Soft 404: an unknown story address returns HTTP 200 with the not-found page, the same decided property as for projects. A crawler sees a 200. Revisit in the SEO work, not here.

No blocking item for approval. The story must stay In Progress until Gustavo judges test 5 (the tone of a real story) and a published story is checked in the container.
