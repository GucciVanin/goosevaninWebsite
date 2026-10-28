# Security review - O5

Scope: `markdown.ts` (headingOffset), `markdown.spec.ts`, new CSS (`.gcv-section--page`, `.gcv-reading`, `--gcv-header-clearance`), class usage in story/project/not-found/index pages. Read-only review; no code edited.

Result: PASS. No blocking or medium findings.

## Findings

| # | Severity | Item | Result |
| --- | --- | --- | --- |
| 1 | None | `MarkdownIt` options unchanged: `html: false`, `linkify: true`. The new rule is a core rule that only rewrites `tag` on `heading_open`/`heading_close` tokens after parsing; it adds no HTML, attributes, or inline content. markdown-it's `validateLink` (javascript:, vbscript:, file:, data: except images) is untouched, so link and scheme protections hold. | Pass |
| 2 | None | Crafted heading input cannot influence the tag. The tag is derived from the parser's own token (`h1`-`h6`), not from source text; heading text is still escaped by the renderer. Offset is a code constant (`1`) at both call sites, never user or request input. | Pass |
| 3 | Info | Defensive edge: a non-finite offset (e.g. NaN) would produce tag `hNaN`. Not reachable (callers pass literal 1; `<= 0` returns early; `Math.min` caps at 6). The string contains only `h`, `N`, `a`; no injection. Optional hardening: `Number.isInteger` check. | Pass, optional |
| 4 | None | `state.env` is created per `render` call, so the offset cannot leak between renders (editor preview stays at 0, public pages at 1). Editor preview paths (`story-form-page.ts:174`, `project-form-page.ts:191`) call without options. | Pass |
| 5 | None | Output still goes through `[innerHTML]` so Angular's sanitizer applies; no `bypassSecurityTrust*` was added anywhere in the client. | Pass |
| 6 | None | CSS is static, token-only (`var(--gcv-*)`), no `url()`, no `@import`, no `content:` from data, no attribute-selector exfiltration. New class names (`gcv-reading`, `gcv-section--page`) are fixed literals in templates; none are built from user data. | Pass |
| 7 | None | Test change: `markdown.spec.ts` asserts `h1` absent and `h2`/`h3`/`h6` present, including the h6 cap. No secrets, no network. (Cosmetic: a double blank line was added.) | Pass |

## Notes
- Heading demotion does not weaken the existing XSS tests in `markdown.spec.ts`; they run with the default offset 0 and the rule is inert there.
- Pre-existing, out of scope: the `/stories` body and project overview rely on markdown-it plus Angular sanitization only (no CSP check done in this review).
