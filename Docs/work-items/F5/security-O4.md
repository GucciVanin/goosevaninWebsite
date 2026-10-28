# Security review: order O4 (public stories pages)

Reviewer: Security. Scope: uncommitted changes on f5-public-browsing (stories index, story page, ?tag= handling, editor reminder, home cards). Static review only; no code edited.

## Verdict: PASS (no blocking findings; 3 low/info notes)

## Checks

| Area | Result |
| --- | --- |
| Markdown / innerHTML | Story page binds `[innerHTML]` to `renderMarkdown(story.content)`. markdown-it has `html: false` and refuses javascript:/vbscript:/data: links; Angular's DomSanitizer sanitizes `[innerHTML]` again. Same pattern as the existing project page and editor preview. Pass. |
| Tag / query reflection | `?tag=` is read from `queryParamMap`, trimmed, shown only through `{{ }}` interpolation (`#{{ t }}`, and in the "No stories tagged" text). Auto-escaped. Used only for a case-insensitive equality filter in memory; never sent to the API, never placed in a URL, innerHTML or attribute. Pass. |
| Cover image | `[src]="s.coverImage"` is passed through Angular URL sanitization. The API also rejects any cover image that is not an uploaded-image reference (`UploadedImageReference.IsValid` in `BlogAuthoringService.ValidateSave`). Alt is bound as a property/interpolation. Pass. |
| Draft exposure | The client only calls `GET /api/blog` and `GET /api/blog/{slug}`, which use `GetPublished*`; drafts get 404, which the page maps to the not-found page. The client does not request or cache any editor/draft endpoint. Pass. |
| Slug handling | Route param is passed through `encodeURIComponent` into the API path; it is also used in `routerLink` as an array segment (encoded by the router). The title is set from `story.title` via `Title.setTitle` (text only). Pass. |
| Open redirects | Home cards navigate to fixed internal routes with a fixed query object; no user-supplied URL is navigated to. Pass. |
| R1-G2 (AuthorId in /api/blog) | The new client `Story` interface does not declare `authorId`, and no new page displays or uses it. The API still sends it (`BlogPostResponse.AuthorId`); remains open under R1-G2 and is not worsened. |
| Secrets / logging | No secrets, no console logging, no storage use in the changed files. Pass. |
| Editor reminder | Static text, shown only for unpublished stories; no behavior change to save/publish. Pass. |

## Notes (non-blocking)

1. Low: the story page `[innerHTML]` relies on two layers (markdown-it html off, Angular sanitizer). Keep `renderMarkdown` as the single entry point; never use `bypassSecurityTrustHtml`.
2. Low: `renderMarkdown` enables `linkify`; external links in a story have no `rel="noopener"` enforcement from the renderer. Modern browsers imply noopener for target=_blank, and markdown-it emits no target, so no action needed.
3. Info: the search box and tag filter run client-side over at most 50 stories (pageSize cap), so older stories are not discoverable once more than 50 are published. A functional limit, not a security issue.
