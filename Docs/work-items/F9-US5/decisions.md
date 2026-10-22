# Decisions - F9-US5

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-22 | Process adopted: start-dev with backlog stories as specs; one commit per US, one PR per full feature (Gustavo). | F9 is one PR (#20, branch `f9-shell-and-accounts`) with one commit per story. |
| 2026-10-22 | `/mattpocock-skills:implement` cannot be invoked by the Captain (reserved for the user). | Gustavo chose: Workers implement orders test-first (TDD) instead. |
| 2026-10-22 | Spec check: F9-US5 has three acceptance tests, all testable (AC3 is the human approval); no open questions. | Accepted; moved to `plan`. |

Acceptance criteria (from `Docs/product-backlog.md`, F9-US5):
- **F9-US5-AC1** The editor workspace, lists, story form and project form use the new tokens and components, with clear draft, preview, save and publish states that do not rely on colour alone.
- **F9-US5-AC2** At 320 CSS pixels or by keyboard, editor actions stay reachable and focus is visible.
- **F9-US5-AC3** Gustavo reviews the editor and approval is recorded.

| 2026-10-22 | Verify result: all suites pass three times. AC2 (320 px layout, keyboard operation, visible focus) is covered only by a manual 320 px measurement and by accessible-name specs; no automated test exists. | Recorded as a follow-up for TE5 (browser tests) and F9-US7; Gustavo decides at review whether to accept. |
