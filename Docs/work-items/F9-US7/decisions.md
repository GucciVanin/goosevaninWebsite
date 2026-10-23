# Decisions - F9-US7

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-22 | Follows TE5 (accepted), in the same pull request (#21). One commit for the story. | The seven known defects in `verify/browser/known-issues.json` (all `fixedBy: F9-US7`) are fixed here and removed in the same commit. |
| 2026-10-22 | Conflict found when planning AC6: F1-US1 expects the name, the positioning line, the short bio and the professional summary on the home page; the new hero shows none of them. Gustavo chose: show them in the About section; the hero stays as in the mockup. | F1-US1 AC4 and F1-US2 AC4 are amended in the backlog (the F9 design replaces them). About gets: name caption, positioning line, short bio, professional summary, from `shared/models/profile.ts`. |
| 2026-10-22 | The signed-out 401 on `GET /api/auth/me`. Gustavo chose: the client skips the probe when there is no session indicator. | A non-secret marker cookie (no identity in it) is set when sign-in succeeds and cleared at sign-out or when the API reports 401; the header, sign-in page and guard ask the API only when it is present. The API contract of `/api/auth/me` does not change. |
| 2026-10-22 | AC5 (latest two versions of Chrome, Edge, Firefox, Safari): only engines can be run (Chromium, Firefox, WebKit). Gustavo accepted engines plus a manual pass. | A manual checklist for real Edge and Safari is written; AC5 is reported as automated on engines and manual for the rest. |

Acceptance criteria (from `Docs/product-backlog.md`, F9-US7), with how each is checked:
- **F9-US7-AC1** Contrast meets WCAG AA and state is not conveyed by colour alone on every public, account and editor screen. Checked by the axe scan (the colour-contrast rule is part of WCAG AA) on every screen, plus the state-in-words specs; the "not colour alone" part on screens without such specs goes in the manual checklist.
- **F9-US7-AC2** Every screen at 320 CSS pixels has no horizontal scroll or clipped content. Browser tests (TE5).
- **F9-US7-AC3** Keyboard-only use reaches every control in a logical order with visible focus. Browser tests (TE5), including the WebKit date-field fix.
- **F9-US7-AC4** With reduced motion, motion is removed or minimal and no content depends on it. Browser test on the home page; the other screens have no animation (also asserted).
- **F9-US7-AC5** Latest two versions of Chrome, Edge, Firefox and Safari: automated on Chromium, Firefox and WebKit; real Edge and Safari and previous versions by manual checklist.
- **F9-US7-AC6** The F1 and F2-US1 acceptance tests re-run against the new design and pass, as browser tests where they can be automated (F1-US1 tests 1 to 3, F1-US2 tests 1 to 3 and 5, F2-US1 tests 1 to 4 as far as the stack allows).
- **F9-US7-AC7** The automated accessibility scan has zero critical or serious violations (TE5 axe scan; the known entries are removed).
