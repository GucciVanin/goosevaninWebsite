# Development process (start-dev)

Adopted 2026-10-22. The human talks only to the **Captain**, who never edits code. Everything an agent needs lives in `Docs/`; a decision not written here did not happen.

## Local adaptation (decided by Gustavo, 2026-10-22)

- **No separate spec sheets.** The features and user stories already in `Docs/product-backlog.md` are the specs. Their acceptance tests are the acceptance criteria (AC); the AC IDs are written `F9-US5-AC1`, `F9-US5-AC2`, and so on, numbered in the order the backlog lists them.
- **One commit per user story** (or enabler), and **one pull request per full feature** (all the stories of that feature, one commit each). This replaces the earlier habit of one pull request per story or per group of stories.
- The environment rules in `CLAUDE.md` still apply: GitHub Actions CI first (`gh pr checks`), then verification in the container with `scripts/verify.ps1`.

## Roles

| Role | Touches code | Responsibility |
| --- | --- | --- |
| Human | No | Intent, approval of the work item, final review (merge is acceptance). |
| Captain | No | Turns the story into orders, relays reports, records one action per report. |
| Worker | Yes | Executes one order; asks the Reviewer before reporting back. |
| Reviewer | No | Compares order to execution: `accurate` or `not accurate`. Never fixes. |
| Security | No | Checks each accepted change set: `pass` or `fail` with findings. |
| Testing | No | Runs the runtime test list as the final check; owns the dynamic test list. |

## Work item

`Docs/work-items/<id>/` holds `intent.md`, `status` (`intake`, `spec`, `plan`, `execute`, `security`, `verify`, `review`, `closed`), `decisions.md`, `captain-log.md` and `orders.md`. Here the id is the story id, for example `F9-US5`.

## Stages

1. **Intake:** one-paragraph intent and a name.
2. **Spec:** the backlog story. Every AC must be testable and there must be no open questions; otherwise escalate to the human. The Captain does not fill gaps.
3. **Plan:** `orders.md`. Each order has an ID, target, technical action, constraints (what must not change) and the AC IDs it serves. Every AC is served by at least one order and every order serves at least one AC.
4. **Execute:** Worker executes, asks a Reviewer, at most three rounds; then `blocked`. A Worker never deviates silently.
5. **Security:** each accurate change set goes to Security. High or critical findings block; lower ones become follow-up items.
6. **Verify:** Testing runs the list in `Docs/tests/`, every test three times, with pinned data and a recorded environment. Any difference between runs is `fail` with reason `nondeterministic`. A new AC adds a test; a fixed security finding adds a regression test.
7. **Review:** the human gets the work product, the story, the Reviewer verdicts, the Security report and the Testing report, and decides `accept`, `accept with follow-ups` or `return`.
8. **Close and learn:** follow-ups become new items at `intake`; gaps the gates missed go in `Docs/learning-log.md` with a gap type (missing requirements, unchecked work, lost context at hand-off, unrecorded decision).

## Captain actions

Every report ends with exactly one recorded action in `captain-log.md`: `route-security`, `route-verify`, `return-worker`, `next-order`, `escalate-human`, `close-item`. A report without an action is incomplete.

## Defaults

Two human checkpoints (story approval, final review). Review loop limit three rounds. High and critical security findings block. The weekly review reads `learning-log.md`; three occurrences of one gap type prompt a change to a checklist or test list.
