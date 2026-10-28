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

## Amendment 2026-10-27 (Gustavo, at review of the first TE4 commit)

Gustavo does not want statements about himself that he has not written. His answers: remove the unverified ones (Skills section, About lede and principles, the Stories intro); keep the hero text and the cards from his mockup; the site's main aspect is showcasing projects. The four cards (Applications, Cloud & DevOps, Security, Systems & Tools) become three cards that separate the kinds of content: **Projects**, **Life stories**, **Competitions** (competitions are life stories with a competition tag). The pages behind the cards come in Feature F5; until then the cards point at the existing sections.

- **TE4-AC6** The home page contains no statement about Gustavo that he has not written: the Skills section (component, navigation link, home composition and its tests) is removed; the About section keeps the portrait and only the name, positioning line, short bio and professional summary from `shared/models/profile.ts`; the About lede and the three principles are removed; the Stories section heading and intro use neutral wording that claims nothing ("Stories", "Stories Gustavo has published."); the default category label "Field notes" stays as the editor default only if Gustavo wants it (open question: ask at review).
- **TE4-AC7** The four category cards are replaced by three: Projects (to the Projects section), Life stories (to the Stories section), Competitions (to the Stories section filtered by the tag `competition`), each with its own icon and image strip; nothing links to a removed section; the browser tests and the axe scan pass.
