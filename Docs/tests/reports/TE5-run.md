# Testing report - TE5 (browser test layer: Playwright and axe-core)

Role: Testing. No source file was edited and no git write command was run.

## Environment

| Item | Value |
| --- | --- |
| Date | 2026-10-22 (real clock, local PDT, about 14:24 to 14:42) |
| Git | branch `f9-emails-and-verification`, HEAD `cb6a1c810de33a8e5869abc6725180f36476d49e` (F9-US6). The TE5 work is UNCOMMITTED: modified `.gitignore`, `Dockerfile.verify`, `Docs/architecture.md`, `Docs/tests/README.md`, `docker-compose.yml`, `scripts/verify.ps1`, `verify/lib.sh`, `verify/run.sh`; untracked `Docs/work-items/TE5/`, `verify/browser/`, `verify/stages/40-browser.sh` |
| OS | Windows 11 Pro 10.0.26200 |
| .NET SDK | 10.0.300 |
| Node | v22.15.0 (host and verification image) |
| Browser for Karma | Chrome 155.0.0.0 headless (`ChromeHeadlessNoSandbox`) |
| Docker | 29.8.2 (Docker Desktop); `goosewebsite-verify` image id `d65dd0881622`, built 2026-10-22 21:13 UTC by run 1; `goosewebsite-web` id `2f9e3faa63126`, built 20:53 UTC (cached, unchanged code) |
| Playwright / axe | `@playwright/test` 1.64.1, `@axe-core/playwright` 4.14.0, `axe-core` 4.14.0 (pinned in `verify/browser/package.json`). The engine builds are the ones Playwright 1.64.1 installs; I did not extract their version numbers |

## Commands

- A: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (plus a TRX logger to get per-test names)
- B: `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`
- C: `./scripts/verify.ps1 -SkipCi` (PowerShell, repo root, one run at a time). CI is not applicable until the Captain pushes.

## Totals per suite and run

| Suite | Run 1 | Run 2 | Run 3 | Result |
| --- | --- | --- | --- | --- |
| A .NET (xUnit) | 169 pass, 0 fail | 169 pass, 0 fail | 169 pass, 0 fail | pass |
| B Angular (Karma) | 97 SUCCESS, exit 0 | 97 SUCCESS, exit 0 | 97 SUCCESS, exit 0 | pass |
| C Container verify (526 results = 169 dotnet + 97 angular + 138 runtime + 122 browser) | 509 pass, 0 fail, 0 skip, 17 KNOWN | 509 pass, 0 fail, 0 skip, 17 KNOWN | 509 pass, 0 fail, 0 skip, 17 KNOWN | pass (with KNOWN) |

The 122 browser results are the 120 Playwright results (3 engines x 40) plus the two stage self-checks (`known-issues entries each match a test - 7 entries`, `browser results complete - 120 of 120 listed tests reported`). Per engine: 5 selftest, 1 smoke, 5 home, 3 each of sign-in, email-confirmation, contact-confirmation, access-denied, 4 each of editor-workspace, story-form, project-form (webkit: 3 project-form and 3 story-form PASS + 2 KNOWN, same total). The 5 selftests per engine are the negative controls (focus indicator must be a change, axe entry does not mask another element, axe violation identified by rule and element, 320 px check asserts the real viewport, selector covers every control kind).

## Irregular runs (nothing hidden)

- First container attempt in my session: the summary I first read (404 passed, no browser lines) was an OLD `C1.log` in my scratchpad (case-insensitive file name clash with a previous session's file), not this run; I discarded it and used a fresh directory. The real run 1 ended with the 526-result summary. No test result was affected.
- No Docker network or DNS failure occurred; every container run was counted. No leftover node or chrome processes: Karma exited 0 on its own in all three runs.
- Karma prints no test names; its per-test results come from the verify log (stage `unit-angular`, 97 named tests).

## Determinism

- A: sorted per-test name+outcome lists from the three TRX files: identical, 169 each, all Passed.
- C: sorted per-test lists (result, stage, test, duration removed) of the three counted runs, 526 lines each, `diff` empty, covering the 120 browser results and the two stage self-checks. Same 17 KNOWN lines in all three runs, 0 FAIL, 0 SKIP.
- The unit-dotnet (169) and unit-angular (97) stage lines match suites A and B.
- Nondeterministic cases: none.

## Per-AC coverage

All results below are pass (or KNOWN as stated) in 3/3 runs, on chromium, firefox and webkit. Browser lines look like `PASS browser [engine] screen: check`.

| AC | Covering tests | Result |
| --- | --- | --- |
| AC1 one command, 3 engines, one line per test per engine, same log and JSON, deterministic | `./scripts/verify.ps1 -SkipCi` ran all three; 120 browser lines in `latest.log` and `latest.json` (`known` field added to the JSON and summary); `[engine] smoke: the home page loads` x3; stage self-checks `browser results complete (120 of 120)` and `known-issues entries each match a test`; three identical runs | pass |
| AC2 each screen loads with no failed request or console error (8 screens) | `[engine] <screen>: loads with no failed request and no console error` for home, sign-in, email-confirmation, contact-confirmation, access-denied, editor-workspace, story-form, project-form | pass for 3 admin screens; KNOWN for the 5 public screens x 3 engines (15 entries, see below) |
| AC3 axe WCAG 2.1 A and AA, critical/serious fail | `[engine] <screen>: has no critical or serious WCAG 2.1 A/AA violation` (8 screens x 3); selftests `an axe violation is identified by rule and element`, `a known axe entry for one element does not mask another element` | pass (no axe defect and no KNOWN entry) |
| AC4 320 px, scrollWidth <= 320 | `[engine] <screen>: fits a 320 px viewport without sideways scrolling` (8 x 3); selftest `the 320 px check asserts the real viewport width` | pass |
| AC5 keyboard order, reachability, visible focus indicator | `[engine] <screen>: keyboard: every control is reachable in page order with a visible focus` (8 x 3 = 24: 22 PASS, 2 KNOWN); `[engine] home: keyboard at 320 px with the mobile menu open` x3; selftests `a focus indicator must be a change from the unfocused state`, `the reachable-element selector covers every kind of control` | pass, except 2 KNOWN (webkit date inputs) |
| AC6 reduced motion on home | `[engine] home: reduced motion stops every animation and keeps the content` x3 | pass |
| AC7 documented, reproducible build | I verified by reading only: `Docs/architecture.md` (browser tests table, "Browser tests (TE5)" subsection) and `Docs/tests/README.md` (Browser tests section: run, read a failure, known issues, add a test) exist and describe the layer; versions pinned in `verify/browser/package.json`. The image rebuilt successfully during run 1. No test asserts the documentation, and I did not rebuild twice to compare image contents | documentation present; reproducibility not independently proven |

## The 17 KNOWN results (7 entries in `verify/browser/known-issues.json`, all `fixedBy: F9-US7`)

| Entry id | Screen | Kind | Engines | Results |
| --- | --- | --- | --- | --- |
| `request-401:GET /api/auth/me` | home | load | all | 3 |
| `request-401:GET /api/auth/me` | sign-in | load | all | 3 |
| `request-401:GET /api/auth/me` | email-confirmation | load | all | 3 |
| `request-401:GET /api/auth/me` | contact-confirmation | load | all | 3 |
| `request-401:GET /api/auth/me` | access-denied | load | all | 3 |
| `no-focus-indicator:input#story-date ""` | story-form | keyboard | webkit | 1 |
| `no-focus-indicator:input#project-date ""` | project-form | keyboard | webkit | 1 |

Total 15 + 2 = 17. For F9-US7: the anonymous `/api/auth/me` call answers 401 (a failed request on every public screen in every engine), and the date inputs show no focus indicator in WebKit. The fixing commit must remove the entries; a fixed defect that stays listed fails the test.

## Not covered

- Real Edge and Safari (only the Chromium and WebKit engines are run; the manual pass on real Edge and Safari is not done by me).
- Previous browser versions (only the build Playwright 1.64.1 ships).
- Requests that start after `networkidle` (known limit of the load check).
- Iframe content and positive `tabindex` ordering (keyboard check does not walk them).
- Out of scope per the criteria: visual regression, Lighthouse, mobile device emulation beyond the 320 px viewport. Also not exercised: the documentation text itself (AC7) and image reproducibility across two rebuilds.

## Failures

None in counted runs.

## Verdicts

- A .NET: 169 pass / 0 fail / 0 known, 3/3 runs - pass
- B Angular Karma: 97 pass / 0 fail / 0 known, 3/3 runs - pass
- C Container verify: 509 pass / 0 fail / 17 KNOWN (526 results incl. 122 browser), 3/3 runs, per-test lists identical - pass
- Nondeterministic cases: none
- AC1 to AC6: pass, with the 17 KNOWN defects handed to F9-US7; AC7: documentation present, reproducibility not independently verified
- CI check: not applicable (nothing pushed)
