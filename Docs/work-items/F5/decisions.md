# Decisions - F5 (Public Project and Story Browsing)

Specs: the acceptance tests of F5-US1 and F5-US2 in `Docs/product-backlog.md`. Order: F5-US1 first (one commit), then F5-US2 (one commit); one pull request for the feature.

## Facts found in the code (2026-10-28)
- The public APIs exist: `GET /api/projects`, `GET /api/projects/{slug}` (404 when unknown or draft) and `GET /api/blog`, `GET /api/blog/{slug}`. No new endpoints needed.
- The client has no `/projects`, `/stories` or not-found route yet. The Projects section on the home page and the Stories section are in-page only.
- A project has one image (the cover, with alt text), not a gallery. F5-US1 AC2 says "images".

## Drafted decisions (need approval)
| # | Question | Proposed answer |
| --- | --- | --- |
| D1 | Routes | `/projects`, `/projects/<slug>`, `/stories`, `/stories/<slug>` (as the 2026-10-02 decision on readable slugs). The home page keeps its Projects and Stories sections as previews that link to the full pages. |
| D2 | Card targets | Projects card -> `/projects`; Life stories -> `/stories`; Competitions -> `/stories` filtered by the tag `competition` (shareable as `/stories?tag=competition`). |
| D3 | Not-found | One site not-found page for unknown project or story slugs and any unknown URL, with a link home. The page is served with the Angular shell; the API call for the slug returns 404. |
| D4 | Project images | The cover image is the project's image (no gallery; adding one is a new story, not part of F5). |
| D5 | Story reading | Quiet column, metadata (date, tags) secondary, optional cover image; Markdown rendered with the existing safe renderer. Existing in-page reading view on the home page is replaced by links to `/stories/<slug>`. |

## Draft acceptance criteria (the backlog tests, made testable)
- **F5-AC1** `/projects` lists every published project and no draft; entries are compact and unframed.
- **F5-AC2** `/projects/<slug>` shows title, overview, contribution, technologies, cover image, demo/source links in that order, links distinguishable; unknown slug shows the not-found page; works on direct load.
- **F5-AC3** `/stories` lists published stories with title, date and tags, no drafts; `?tag=` filters; `/stories/<slug>` shows title, body, date, tags, optional cover; unknown slug shows not-found.
- **F5-AC4** No horizontal scroll at 375 px and desktop; axe finds no serious violation on the new pages; browser tests added for the pages (with the empty states, since the verification stack publishes nothing).
| 2026-10-28 | Checkpoint 1: Gustavo approved D1-D5 and F5-AC1..AC4 as drafted. | Status plan -> execute. |
