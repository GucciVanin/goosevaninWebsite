# Testing report - F9-US5 (editor workspace and forms, new design)

Role: Testing. No source file was edited and no git write command was run.

## Environment

| Item | Value |
| --- | --- |
| Date | 2026-10-22 (real clock; local time PDT, 11:25 to about 12:00) |
| Git | branch `f9-shell-and-accounts`, HEAD `ed74295e8dc45cdc40d73eab49de0171db5ef2b6` (F9-US4). The F9-US5 work is UNCOMMITTED: 10 modified files (editor components/specs, `components.scss`, `tokens.scss`) and untracked `Docs/learning-log.md`, `Docs/process/`, `Docs/specs/`, `Docs/tests/`, `Docs/work-items/`, `editor-design.spec.ts` |
| OS | Windows 11 Pro 10.0.26200 |
| .NET SDK | 10.0.300 |
| Node | v22.15.0 |
| Browser for Karma | Chrome 155.0.0.0 headless (`ChromeHeadlessNoSandbox`) |
| Docker | 29.8.2 (Docker Desktop); image `goosewebsite-web:latest` id `043870a4dd84`, built 2026-10-22 11:42 PDT (rebuilt by every container run) |
| Pinned data | The container run seeds fixed test administrators and unique-suffixed content; the clock is real. |

## Commands

- A: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (repo root)
- B: `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`
- C: `./scripts/verify.ps1 -SkipCi` (PowerShell, repo root). CI check not applicable until the Captain commits and pushes.

## Totals per suite and run

| Suite | Run 1 | Run 2 | Run 3 | Result |
| --- | --- | --- | --- | --- |
| A .NET (xUnit) | 158 passed, 0 failed | 158 passed, 0 failed | 158 passed, 0 failed | pass |
| B Angular (Karma) | 97 SUCCESS | 97 SUCCESS | 97 SUCCESS | pass |
| C Container verify (393 = 158 dotnet + 97 angular + 138 runtime) | see note | 393 passed, 0 failed, 0 skipped | 393 passed, 0 failed, 0 skipped | pass |

Notes on irregular runs (nothing hidden):
- C run 1 ABORTED before any test: the Docker build could not pull the base image (`dial tcp: lookup westus.data.mcr.microsoft.com: no such host`, a network/DNS failure on this machine); verify.ps1 logged `FAIL stack ... did not become healthy` and exit 2. No test executed, so it carries no per-test result. It was repeated as a replacement run (a fourth invocation) which gave 393 passed, 0 failed, exit 0. Counted as run 1 below. This is an environment failure, not nondeterminism of any test. Run order was: aborted, run 2, run 3, replacement run 1.
- B: the first attempt (PowerShell redirect) completed 97 of 97 SUCCESS but the `npm test` process did not exit afterwards (leftover node/chrome processes, killed by me). One further attempt wrote to a locked output file and did not run. The three counted runs were made afterwards, each exit 0 with `TOTAL: 97 SUCCESS`. The Karma default reporter prints no test names, so per-test pass/fail for Karma comes from the verify.ps1 log (stage `unit-angular`, 97 named tests).
- A is 158 here; the `unit-dotnet` stage in verify lists the same 158 names.

## Determinism

The full per-test lists (stage, name, result, duration removed) of the three good container runs were sorted and diffed: identical, 393 lines each, 0 FAIL. No test differs between runs, so there are no `nondeterministic` cases. Karma and xUnit totals are identical in all three runs.

## Per-AC coverage

| AC | Automated coverage | Evidence (all pass in 3/3 runs) |
| --- | --- | --- |
| AC1 new tokens, states in words | Yes | `editor-design.spec.ts` (10 specs): no legacy alias or colour literal in styles/template of EditorPage, StoryFormPage, ProjectFormPage, ImageControls; rendered markup has no inline style/alias/literal; global `.gcv-editor`/`.gcv-auth` rules carry no colour literal (glows from tokens); workspace rows, story form and project form write Draft and Published in words. `editor-page.spec.ts`: shared page shell, rows as cards with labelled chip, delete confirmation in words with two named buttons in shared alert style. `story-form-page.spec.ts`: Draft badge in words, Published badge, Save solid and Publish separately named, shared field style, preview banner in words, refusal in shared alert style. `project-form-page.spec.ts`: status badge and shared buttons. `image-controls.spec.ts`: inputs inside shared field with labels. Limit: "uses the new tokens" is checked by absence of legacy aliases and literals, not by a positive visual check. |
| AC2 320 px / keyboard / visible focus | Partial | Automated: every button, link, input, textarea and select on the three pages has an accessible name; the Stories/Projects switch exposes `aria-pressed` on both buttons and moves it on choosing (`editor-design.spec.ts`, `editor-page.spec.ts`). Runtime stage also checks `/editor` route and the editor list/preview flows. NO automated test for 320 px layout (no horizontal scroll). The only 320 px evidence is the manual measurement in `Docs/work-items/F9-US5/worker-O3.md` (emulated viewport, stubbed API, scrollWidth 320 on all editor routes, including long-text cases); I did not re-measure. NO automated test for keyboard operation or for visible focus: focus visibility relies on the shared focus style, unchanged and not re-measured (O3 says so). |
| AC3 Gustavo's approval | Not testable by Testing | Human approval, to be recorded by the Captain. |

## Failures

None in counted runs. The only failure output was the aborted container run 1 (network DNS error pulling `mcr.microsoft.com/dotnet/sdk:10.0` layer, see above).

## Verdict list

- A .NET unit/API tests (GooseWebsite.Api.Tests, incl. EditorAccessTests): 158/158 pass, 3/3 runs - pass
- B Angular Karma (including EditorPage, StoryFormPage, ProjectFormPage, ImageControls, editor-design): 97/97 pass, 3/3 runs - pass
- C Container verify, unit-dotnet stage: 158/158 pass; unit-angular stage: 97/97 pass; runtime stage: 138/138 pass, 3/3 counted runs - pass
- Nondeterministic cases: none
- AC1: pass (automated). AC2: automated parts pass; 320 px only by manual record, keyboard/focus visibility has no automated test. AC3: pending human.
- CI check: not applicable (nothing committed or pushed).
