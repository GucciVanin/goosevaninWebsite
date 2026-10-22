# Learning log

Gaps the gates missed: the gap type (missing requirements, unchecked work, lost context at hand-off, unrecorded decision), what happened, and what would have caught it. No entries yet.

## 2026-10-22 - F9-US5

- **Gap type: unchecked work.** The story's AC2 asks for 320 px and keyboard behaviour, but the project has no browser-level test layer yet (TE5), so the Testing role could verify it only by one-off manual measurement. What would have caught it: scheduling TE5 before the F9 stories that depend on browser checks, or writing AC2 as a unit-testable statement.

## 2026-10-22 - F9-US6

- **Gap type: unchecked work.** AC1 asks for real mail clients and "follows the approved design", which no automated test can assert, and the logo cannot load from `localhost`. What would have caught it: a rendered-email visual test in the browser layer (TE5) and a public staging address; until then the manual checklist `Docs/email-templates/client-check.md` is the check.
