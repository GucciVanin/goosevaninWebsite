# Decisions - TE4 (remove sample content)

Intent: before any deployment the site must show only what Gustavo published; no invented posts or projects.

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-26 | Part of the R1 close-out pull request (`r1-closeout`), one commit per item. The backlog line only says what to remove, with no acceptance criteria; the Captain drafted them below for Gustavo's approval. | Pending approval. |
| 2026-10-26 | The Projects section already shows only published projects (done in F9). Left: the three hard-coded posts in the Stories section (`features/blog/blog-page.ts`), and the demo posts in local development databases. | The Stories section loads published stories from `/api/blog` only and has an empty state; the demo rows are removed by an operator action documented in the work item, not by an automatic migration (they live in local databases, not in the repository). |

Draft acceptance criteria:
- **TE4-AC1** The client contains no hard-coded story, project or profile-sample content: the Stories section starts empty and shows only what `/api/blog` returns; with none published it says so (an empty state in words); a failure to load says so.
- **TE4-AC2** A search, a category filter and the reading view still work with API data (tests with a stubbed API returning two stories), and the browser tests (TE5) pass on every screen with the empty state and axe finds no serious violation in it.
- **TE4-AC3** The `/design` review page (sample content by design) is removed from the routes and the shell rule, its component deleted, and its checks removed from the verification (`verify/runtime/30-pages.sh`) so no sample content is served.
- **TE4-AC4** No sample or demo content remains in the repository: a search for the known sample strings (the three post titles, "Pulseboard", "Atlas Finance", "Fieldnotes") finds none outside `Docs/` history and test fixtures; the runtime tests that create content clean up after themselves (the verification stack data volume is reset each run).
- **TE4-AC5** A short operator note (in `Docs/architecture.md` known gaps) says how to remove demo posts from an existing local database (delete them in the editor) and the architecture known-gaps row is updated.

Out of scope: seeding sample content, changing the editor, F5 browsing pages.

| 2026-10-26 | Checkpoint 1: Gustavo approved the drafted acceptance criteria, the F3-US1 evidence plan and the R1 gate checklist as drafted. | Moved to `plan`, then `execute`. |
