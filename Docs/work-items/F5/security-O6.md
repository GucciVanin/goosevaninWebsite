# Security review - O6

Scope: BlogController page-size cap removal, story summary, gcv-reading on the project overview, GIF upload and serving, emoji handling, plainSummary. Method: read the diff and the code paths (BlogController, BlogPostRepository, MediaModule, ImageService, ImageSniffer, ImageEditorController, markdown.ts, Caddyfile). Nothing was run and no container was touched.

## Verdict: PASS, with one recommended change (S1)

## Findings

### S1 - Unbounded `pageSize` on unauthenticated `GET /api/blog` - Low (Medium if the content grows or Gustavo wants hardening) - recommend a ceiling
- Facts: the Caddyfile proxies everything to the API, so the endpoint is reachable by anyone who reaches the site. "Internal" is not a network boundary here.
- Overflow and crash safety: sound. `page` is at least 1, `pageSize` at least 1, and `(long)(page-1)*pageSize > int.MaxValue` returns an empty list, so the `int` product in `Skip` cannot overflow. `Take(int.MaxValue)` becomes a SQLite LIMIT, which is valid. No exception path found.
- Real exposure: the list returns the full `Content` of every published story (no pagination ceiling, no length limits on title, excerpt or content were found in Blog or Projects contracts) and includes the author. Response size equals the whole published corpus; the server materializes all entities plus the serialized list in memory. Only an administrator can create stories, so the corpus is bounded by Gustavo's own writing, and one request costs about the same as the home or index page the client already makes. The cap never prevented repeated requests, so it was never a rate-limit; the extra exposure is only that one request can no longer be made cheap. Amplification is low today (a personal site, tens of stories) but it grows linearly with content, and the rate limiter is registered only for the accounts flows (`AccountsModule.cs:111`), not for public reads.
- Recommendation: restore a ceiling that still satisfies the index requirement, rather than the 2147483647 sentinel. For example `MaxPageSize = 500` (or 1000), `pageSize = Math.Clamp(pageSize, 1, MaxPageSize)`, and have the client default to that ceiling (or page through until a short page). This keeps "every story" true for any realistic site, removes the sentinel, and keeps the overflow guard as defence in depth. If Gustavo insists on no cap, accept the risk explicitly in `decisions.md` (the worker's own doubt already notes this) and keep the overflow guard.
- Optional follow-up: omit `Content` from the list response (the index needs only excerpt and metadata), which removes the bulk of the size amplification at any cap.

### S2 - GIF upload and serving - Pass
- Type decided by magic bytes (`GIF87a`/`GIF89a`), never by name or declared type. Stored under a generated GUID name with an extension chosen by the server, so no path traversal or extension control.
- Served by `/uploads` static files with `ServeUnknownFileTypes = false` and `X-Content-Type-Options: nosniff`; `.gif` maps to `image/gif`. SVG cannot be uploaded (not in the sniffer, the extension list or the content-type list), so no scripted image can be served from the site origin.
- Upload is admin-only, CSRF-protected, with a 5 MB limit enforced on the request and while reading the stream.
- Residual (Info): animated-GIF frame count or dimensions are not validated. The server never decodes the file (no image library), so there is no server-side decompression bomb; a visitor's browser decodes it, and the 5 MB cap and admin-only upload bound the risk to Gustavo's own uploads. No action needed.
- Residual (Info): the sniffer checks only the header, so a file starting with `GIF89a` followed by other content is stored. It is served as `image/gif` with nosniff, so it is inert.

### S3 - Story summary and gcv-reading on the overview - Pass
- The excerpt is rendered with Angular interpolation (`{{ s.excerpt }}`), which is HTML-escaped; no `innerHTML`. Emoji and markup characters appear as text.
- The project overview and story body go through `renderMarkdown` with `html: false` (markdown-it also blocks `javascript:`, `vbscript:` and `data:` links) and Angular's sanitizer on `[innerHTML]`. Adding the `gcv-reading` class is presentational only and changes no data path.
- Inline GIFs in Markdown render as `<img src>`; an external image URL in the body is allowed by Markdown (unchanged, noted by the worker). That lets a (trusted, admin) author embed third-party images, which would leak visitor IPs to that host; acceptable because only Gustavo writes content.

### S4 - Emoji handling - Pass
- The sanitizer strips only `char.IsControl` characters and trims; this does not remove ZWJ, variation selectors or other format characters (`char.IsControl` covers Cc only). The API tests the worker added back this up. No injection or normalization issue introduced. Lone control or bidi override characters are outside this change.
- `plainSummary` now cuts on a grapheme boundary with `Intl.Segmenter`: output is text only, rendered through interpolation, and the loop is bounded by `end + 2`, so there is no ReDoS or unbounded-work concern. Minor non-security note: `Intl.Segmenter` is unavailable in very old browsers and would throw there; consider a feature check if older support matters.

## Summary table

| ID | Area | Severity | Result |
| --- | --- | --- | --- |
| S1 | Unbounded `pageSize` on public GET | Low | Pass with recommendation: set a ceiling of about 500 and drop the 2147483647 sentinel |
| S2 | GIF upload and serving | Info | Pass |
| S3 | Summary, `gcv-reading`, Markdown | None | Pass |
| S4 | Emoji, `plainSummary` | None | Pass |

No blocking findings. No secrets, logging or authorization changes in this order.
