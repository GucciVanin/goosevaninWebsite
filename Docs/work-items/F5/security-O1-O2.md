# Security review - F5 orders O1 and O2

Scope: uncommitted changes for the public projects index/detail pages, the not-found page, shell routing and the home links. Reviewed the client files and the API they consume (`Modules/Projects`). No code was changed.

**Verdict: PASS.** No high or critical findings. Highest severity: low (two hardening follow-ups).

## Checks

| Area | Severity | Result |
| --- | --- | --- |
| XSS via overview Markdown | none | `project-page.ts` renders `overview` with `renderMarkdown` (markdown-it, `html: false`, which refuses `javascript:`/`vbscript:`/`data:` links), then binds with `[innerHTML]`, so Angular's sanitizer runs a second time. No `bypassSecurityTrust*` anywhere in the client. Title, contribution, technologies and image alt use text interpolation (escaped). The index page uses `plainSummary` as plain text, not HTML. |
| Demo/source link schemes | none | `[href]` goes through Angular's URL sanitizer, which rewrites `javascript:` to `unsafe:`. The API also accepts only absolute `http`/`https` URLs with a host on save (`IsAcceptableLink`). Both links use `target="_blank" rel="noopener noreferrer"`. |
| Cover image source | none | The API accepts only references to images uploaded in the editor (`UploadedImageReference.IsValid`), and `[src]` is sanitized. Not attacker-supplied for visitors. |
| Draft exposure | none | `GET /api/projects` and `GET /api/projects/{slug}` use `GetPublishedAsync` and `GetPublishedBySlugAsync` (`IsPublished` filter), and `ProjectAccessRule` also restricts visibility. A draft slug gets 404, which the page maps to the not-found view, so a draft cannot be told apart from an unknown slug. |
| Slug handling | none | The client uses `encodeURIComponent(slug)`, so a slug cannot add path segments or a query string. The server does an exact EF Core parameterized lookup. The slug is never put in HTML. |
| Information leaks in public responses | info | The response carries no `AuthorId`, user name or email. It does include `id`, `isPublished` (always true), `createdAtUtc` and `updatedAtUtc`, which the client does not use. Harmless, but not needed publicly. |
| Open redirects | none | `navigateByUrl` is only called with the fixed `/projects` card route and `/`. The `scrollTo` target is an internal section id from fixed arrays, passed to `getElementById`. No URL taken from the query string or the user. |
| Secrets and logging | none | No secrets, tokens or credentials in the diff. No `console.*` or logging added. Error handlers discard the error object and show only a fixed message. |
| Shell and routing | none | `authRoute` still hides the shell for `/sign-in`, `/verify-email`, `/contact/verify` and the editor. The editor guards on the routes are unchanged. The wildcard route is last, so it cannot shadow `access-denied` or the editor. |

## Follow-ups (not blocking)

1. **low - Markdown images can load third-party URLs.** markdown-it allows `![](https://other-host/x.png)` in an overview. The author is the trusted administrator, so the risk is only a visitor IP and referrer leak to a third-party host, and the same holds for stories. No Content-Security-Policy header was found in the repository (`img-src`, `script-src`). Add a CSP at Caddy or the API in a hardening item.
2. **low - External-link tabnabbing in Markdown links.** Links produced from Markdown have no `target`, so there is no opener risk now. If `target="_blank"` is ever added to the renderer, add `rel="noopener noreferrer"` with it.
3. **info - Trim the public project DTO.** Use a dedicated public response without `id`, `isPublished`, `createdAtUtc` and `updatedAtUtc`. This would be a public contract change, so the orders ("no change to the public API contracts") rightly excluded it from O1.
4. **info - The runtime check in `30-pages.sh`** only checks that the shell is served with 200 (the SPA fallback). It does not verify that a draft slug returns 404 from the API. Consider a runtime test: create a draft, `GET /api/projects/{slug}` returns 404, and add it as a regression test.
