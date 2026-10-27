# Worker record - TE4 orders O1, O2, O3 (2026-10-26)

## O1 - Stories section from the API only
Files changed:
- `src/GooseWebsite.Client/src/app/features/blog/blog-page.ts`: removed the three hard-coded posts; `posts` starts empty; `status` signal (`loading`, `loaded`, `failed`); `categories` is now a computed `All` plus the sorted categories present in the data; new `open(post)` loads `../editor/markdown` with a dynamic import on first reading and renders the body.
- `blog-page.html`: categories read from `categories()`; empty list shows one of "Loading stories...", "Stories could not be loaded right now. Please try again later.", "Stories will appear here as soon as Gustavo publishes them.", or the old "No story found. Try another search." (data present, no match); reading view body is `<div class="content gcv-preview" [innerHTML]="detailHtml()">`.
- `blog-page.scss`: removed `white-space: pre-line` from `.content`.
- New `blog-page.spec.ts` (9 specs, stubbed `HttpTestingController`, invented titles Alpha and Beta): loading state; none published; failure (search stays usable); two stories list plus derived categories; search; category filter; reading view and back; Markdown body rendered safely (heading and bold rendered, `<script>` not added and not run, `javascript:` link not an anchor).

Markdown decision: render with the existing safe renderer (html disabled), because the API returns Markdown and `pre-line` would show raw `##` and `**`. It is loaded with a dynamic import, so `markdown-it` stays out of the main bundle (the build shows it as a lazy `markdown` chunk, 92 bytes, shared with the editor chunks); Angular sanitizes the bound HTML again.

Red observed (before implementing): Karma ran the new spec against the old component and 5 specs failed: loading state, empty-state wording, failure wording, derived categories (got the hard-coded `All, Security, Field notes, Build log`) and Markdown rendering. The failure spec also raised an unhandled `HttpErrorResponse` that disconnected the browser (no error handler), which the implementation removed. After implementing, 2 specs still failed because the dynamic import resolves after `whenStable`; I fixed the spec with a small `rendered()` wait (polls until the body has children), not the component. Then green.

## O2 - Remove /design
- Deleted `features/design/` (only `design-page.ts`).
- `app.routes.ts`: removed the `design` route. `app.ts`: removed `/design` from the standalone-route rule. `verify/runtime/30-pages.sh`: removed `/design` from the route loop.
- Nothing else imported `features/design`. `ProjectPanel`, `BrandLogo`, `ArtScene` are still used (selected-work, shell header/footer, auth and editor pages, brand.spec).
- `Docs/architecture.md` section 6.1: review-page bullet removed. `Docs/product-backlog.md` F9-US1 evidence says the page was approved 2026-10-20 and removed 2026-10-26 by TE4. `Docs/project.md` F9-US1 row notes the removal.

## O3 - Sweep, docs, operator note
Searched the repository (excluding node_modules, .git, bin, obj, wwwroot, .angular, dist, `Docs/work-items`, reports) for: the three story titles, Pulseboard, Atlas Finance, Fieldnotes, lorem, placeholder, sample, demo post, "Untitled signal", plus a look at API `seed`/`demo` hits and the home sections (toolkit, approach, hero). Found no sample content other than the removed items. Renamed the fallback title `Untitled signal` to `Untitled story` in `blog-page.ts`.
Left, and why: HTML `placeholder=` attributes on the search and contact fields (form hints, not content); "demo" in the Projects module/migrations (demo link field); "sample" in `tokens.scss` ("sampled with an image tool") and `Docs/email-templates/README.md` ("sample values"); test fixtures in the new spec (Alpha, Beta). The About and Skills text and the three principles on the home page are Gustavo's own profile copy, not demo content; I left them (Gustavo should confirm if he considers any of it invented).
Docs: `Docs/architecture.md` section 11 known-gaps row rewritten (demo posts may remain in existing local databases) with the operator note: back up `Data/goosewebsite.db`, then delete in the editor or `DELETE FROM BlogPosts WHERE Title IN (...)` after listing with `SELECT Id, Title FROM BlogPosts;`; the verification stack volume is reset each run. `Docs/project.md` TE4 row marked done 2026-10-26. `Docs/product-backlog.md`: F4 "starting point" sentence updated and a decision-log row added (placed first in the table).

## Final run
- Karma: 128 of 128 success (was 119 before; +9 new).
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 175 passed, 0 failed.
- `./scripts/verify.ps1 -SkipCi`: 574 passed, 0 failed, 0 skipped, 0 known (`verify/browser/known-issues.json` empty; logs `verify-results/latest.log`, `latest.json`).
- No git write commands were run. Note: the working tree already had uncommitted Docs changes (backlog, F9 work-item statuses) before I started; my backlog edits sit alongside them.
