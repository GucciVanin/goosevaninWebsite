# Decisions - TE5

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-22 | TE5 had only one line in the backlog and no acceptance criteria; F9-US7 depends on it. The Captain does not fill spec gaps. | Gustavo chose: TE5 first as its own enabler commit, then F9-US7, in the same pull request (#21). Acceptance criteria drafted below for Gustavo's approval before any build. |
| 2026-10-22 | Browser matrix: only engines can run here (Chromium for Chrome and Edge, Firefox, WebKit for Safari). | Gustavo accepted engines plus a manual pass on real Edge and Safari; what was not run is recorded in each report. |
| 2026-10-22 | Proposed tool (Captain's recommendation, open to change): Playwright with axe-core, in the existing verification container (`Dockerfile.verify`), as a new stage `40-browser` of `scripts/verify.ps1`, against the live stack. | Pending Gustavo's approval of this file. |

Draft acceptance criteria (the Captain's reading of `Docs/project.md` TE5 and the F9-US7 tasks):
- **TE5-AC1** One command: `scripts/verify.ps1` runs the browser tests in the verification container against the live stack, on Chromium, Firefox and WebKit, with one log line per test per browser, in the same log and JSON result as the other stages, and each test is deterministic (three identical runs).
- **TE5-AC2** Coverage of screens: the home page, sign-in, email confirmation (a link without a token), contact confirmation (a link without a token), access denied, and the editor workspace, story form and project form with a signed-in administrator (seeded test administrator) each load with no failed request and no uncaught console error.
- **TE5-AC3** Accessibility scan: axe-core with the WCAG 2.1 A and AA rule sets runs on every screen in TE5-AC2; the test fails on any critical or serious violation and lists every violation (rule, impact, element) in the log.
- **TE5-AC4** 320 px: every screen in TE5-AC2 at a 320 CSS-pixel viewport has `scrollWidth` of at most 320.
- **TE5-AC5** Keyboard: on every screen, tabbing from the top reaches every interactive element, in an order that follows the page, and each focused element shows a visible focus indicator (a non-empty outline or box-shadow).
- **TE5-AC6** Reduced motion: with `prefers-reduced-motion: reduce`, no CSS animation or transition runs on the home page (including the animated scene), and the page content is complete.
- **TE5-AC7** The layer is documented in `Docs/architecture.md` and `Docs/tests/README.md` (how to add a test, how to run one browser, how to read a failure), and the container image build stays reproducible (pinned versions).

Out of scope: visual-regression snapshots, Lighthouse in CI, real Edge and Safari, previous browser versions (listed in `Docs/project.md` as later work).

| 2026-10-22 | Checkpoint 1: Gustavo approved the acceptance criteria and the tool (Playwright with axe-core in the verification container) as drafted. | Moved to `plan`. |
| 2026-10-22 | Design decision for defects the new tests find: a defect found by a TE5 test is not fixed inside TE5 (F9-US7 fixes them) and must not hide. Known defects are listed in `verify/browser/known-issues.json` with a rule/screen id and the story that fixes it; the test reports them as `KNOWN` (not `pass`), and a known entry that no longer reproduces makes the test fail until the entry is removed. | Keeps the verification honest and the intermediate commit green. |
