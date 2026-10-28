# Learning log

Gaps the gates missed: the gap type (missing requirements, unchecked work, lost context at hand-off, unrecorded decision), what happened, and what would have caught it. No entries yet.

## 2026-10-22 - F9-US5

- **Gap type: unchecked work.** The story's AC2 asks for 320 px and keyboard behaviour, but the project has no browser-level test layer yet (TE5), so the Testing role could verify it only by one-off manual measurement. What would have caught it: scheduling TE5 before the F9 stories that depend on browser checks, or writing AC2 as a unit-testable statement.

## 2026-10-22 - F9-US6

- **Gap type: unchecked work.** AC1 asks for real mail clients and "follows the approved design", which no automated test can assert, and the logo cannot load from `localhost`. What would have caught it: a rendered-email visual test in the browser layer (TE5) and a public staging address; until then the manual checklist `Docs/email-templates/client-check.md` is the check.

## 2026-10-22 - F9-US7

- **Gap type: missing requirements.** F2-US1 (marked Done) required the invalid field to be identified and errors to sit beside the fields, but the form only disabled the submit button; it went unnoticed until a browser test checked it. What would have caught it: a browser-level layer when F2-US1 was built (TE5 now exists), and acceptance criteria written as observable text rather than "identified".
- **Gap type: unrecorded decision.** The F1-US1 text on the home page was lost when the hero was redesigned (F9-US3) and nobody re-ran F1 until F9-US7. What would have caught it: re-running earlier features acceptance tests when a screen is redesigned, not only at the end of the feature.

## 2026-10-28 - F5-US1

- **Gap type: lost context at hand-off.** The shell rendered the home page for every URL that was not an authentication screen, so a new public route would have shown the whole home page under its own content; the order said "update the shell" without saying how, and the router-test harness renders into its own outlet, so a shell test must navigate with the router itself. What would have caught it: stating the shell's three modes (authentication, home, routed public page) in `architecture.md` before routes were added, which is now there.
