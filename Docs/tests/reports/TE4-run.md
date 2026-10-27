# Testing report - TE4 (remove sample content)

Role: Testing. No source file was edited and no git write command was run.

## Environment

| Item | Value |
| --- | --- |
| Date | 2026-10-26 (real clock, local, about 15:50 to 16:20) |
| Git | branch `r1-closeout`, HEAD `5d1c9de38636c6ab9c253a72a0da5b011dd1cc64`. The TE4 work is UNCOMMITTED: 19 entries in `git status`, the same count before and after my runs |
| OS | Windows 11 Pro 10.0.26200 |
| .NET SDK | 10.0.300 |
| Node | v22.15.0 (host) |
| Browser for Karma | Chrome 155.0.0.0 headless (`ChromeHeadlessNoSandbox`) |
| Docker | 29.8.2; images after my runs: `goosewebsite-web` f29eaeb08f51, `goosewebsite-verify` e6e4a4f87b01 (rebuilt by `verify.ps1`, layer cache) |
| Playwright / axe | as pinned in `Docs/tests/README.md` (1.64.1 / 4.14.0); engine build numbers not recorded |
| Known issues | `verify/browser/known-issues.json` is `[]` |

## Commands

- A: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (plus a TRX logger for per-test names)
- B: `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`
- C: `./scripts/verify.ps1 -SkipCi` (PowerShell, repo root, three runs one after another from a wrapper that only ran it and copied `verify-results/latest.*`). CI: not applicable (nothing pushed).

## Totals per suite and run

| Suite | Run 1 | Run 2 | Run 3 | Result |
| --- | --- | --- | --- | --- |
| A .NET (xUnit) | 175 pass, 0 fail, 0 skip | 175 pass, 0 fail, 0 skip | 175 pass, 0 fail, 0 skip | pass |
| B Angular (Karma) | 128 of 128 SUCCESS | 128 of 128 SUCCESS | 128 of 128 SUCCESS | pass (see irregular runs: `npm test` did not exit) |
| C Container verify | 574 pass, 0 fail, 0 skip, 0 known, exit 0 | 574 pass, 0 fail, 0 skip, 0 known, exit 0 | 574 pass, 0 fail, 0 skip, 0 known, exit 0 | pass |

C breaks down as 175 unit-dotnet + 128 unit-angular + 143 runtime + 128 browser = 574. The 128 browser results are 126 Playwright results (3 engines x 42) plus the two stage self-checks. Compared with the F9-US7 run (567): Karma +8 (120 to 128, the blog-page specs), runtime -1 (the `route /design serves the app` check was removed), so +7.

## Irregular runs (nothing hidden)

- Suite B: the Karma result was `TOTAL: 128 SUCCESS` in all three runs, but `npm test` did NOT exit by itself in any of them (in the F9-US7 run it exited 0). Leftover `npm`, karma node and Chrome processes stayed alive. I ended the karma/Chrome process trees that my own runs started (by process id, selected by command line) so the next run could start; the recorded exit code of 1 is from that termination, not from a test. Because the wrapper loop advanced only after the kill, runs 2 and 3 started a few seconds after the kill. This is an environment/tooling behaviour to look at (a possible hang of the Karma process at the end of the run), not a failing test. A pre-existing `node scripts/test.mjs` process (started 15:39, not mine) was left alone.
- Suite A and C: no Docker network or DNS failure; all three container runs were counted. They took about 3 minutes each because the image layers and browser layer were cached.
- Karma prints no test names; its per-test results are the 128 `unit-angular` lines of the container log.

## Determinism

- A: sorted per-test name+outcome lists from the three TRX files: identical (175 lines, all Passed).
- C: sorted per-test lists (result, stage, test; durations removed) of the three counted runs: 574 lines each, `cmp` reports no difference, including all 128 browser results on three engines. 574 PASS, 0 FAIL, 0 SKIP, 0 KNOWN in each run.
- B: 128 of 128 in each run; the 128 named unit-angular tests are inside C's identical lists.
- Nondeterministic cases: none.

## Coverage of the acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| TE4-AC1 no hard-coded content; starts empty; empty and failure states in words | Karma `BlogPage`: `shows a loading state and no story before the API answers`, `says in words that nothing is published when the API returns none`, `says in words that loading failed and keeps the section usable` (3/3 runs). Live check on the verification stack after the runs: `GET /api/blog` returned `[]` and the rendered home page's Stories section reads "Stories will appear here as soon as Gustavo publishes them." (read once through the browser pane, not repeated three times). Repository search for sample strings (AC4) finds nothing | pass |
| TE4-AC2 search, category filter, reading view with API data (two stubbed stories); browser tests pass on the empty state with axe clean | Karma: `lists both and builds the categories from the data`, `filters by search text`, `filters by category`, `opens the reading view and goes back`, `renders the Markdown body safely in the reading view`. Browser stage on the home page (the stack has no published stories, so all of these saw the empty state): `home: has no critical or serious WCAG 2.1 A/AA violation`, `loads with no failed request and no console error`, `fits a 320 px viewport`, `keyboard ...`, `keyboard at 320 px with the mobile menu open`, `reduced motion ...`, profile and contact checks, on chromium, firefox and webkit, all PASS in 3/3 runs, 0 KNOWN | pass |
| TE4-AC3 `/design` removed | Diff reviewed: route removed from `app.routes.ts`, entry removed from the shell rule in `app.ts`, `/design` removed from the route list in `verify/runtime/30-pages.sh`; `src/GooseWebsite.Client/src/app/features` has no `design` directory. Runtime: the six remaining routes and `route /a-route-that-does-not-exist serves the app` PASS in 3/3 runs (143 runtime tests, one fewer than before). On the live stack `/design` answers 200 with the app fallback page (same as any unknown route), not a design page | pass |
| TE4-AC4 no sample content in the repository | My own search over every tracked and untracked, non-ignored file (git file list) excluding `Docs/work-items`, `Docs/tests/reports`, `node_modules`, build output, `wwwroot`, lockfiles, for `Pulseboard`, `Atlas Finance`, `Fieldnotes`, `The first signal`, `Threat modeling my morning`, `Shipping a tiny tool` (case-insensitive): 0 matching files. Runtime tests that create content clean up / the verification data volume is reset by `verify.ps1` (the blog API was `[]` after the runs) | pass |
| TE4-AC5 operator note in architecture.md known gaps | READ, not executed: the known-gaps row (about line 636 of `Docs/architecture.md`) says existing local databases may hold demo posts, to back up `Data/goosewebsite.db` first, then delete them in the editor or run `DELETE FROM BlogPosts WHERE Title IN (...)` with `sqlite3` after listing with `SELECT Id, Title FROM BlogPosts;`, and marks TE4 done 2026-10-26. I did not run the SQL | pass (reading only) |

## NOT covered

- The Stories section with real published stories in a real browser: the browser stage runs against a stack with no published stories, so only the empty state is seen in the browser. Search, filter and reading view are covered by Karma with a stubbed API only.
- The Stories failure state ("could not load") in a browser: Karma only.
- The home page copy question is a human decision, not a test.
- The operator note (AC5) was read, not executed: no SQL was run against any database.
- Users or databases that already hold demo posts: not tested.
- Real Edge and Safari and previous browser versions (manual checklist `Docs/tests/browser-manual-check.md` not run by me). CI: not applicable (nothing pushed).
- Karma's failure to exit was not investigated.

## Failures

None in counted runs.

## Verdicts

- A .NET: 175 pass / 0 fail / 0 known, 3/3 runs - pass
- B Angular Karma: 128 pass / 0 fail / 0 known, 3/3 runs - pass (but `npm test` did not exit by itself; processes ended by me)
- C Container verify: 574 pass / 0 fail / 0 known (128 browser results), 3/3 runs, per-test lists identical - pass
- Nondeterministic cases: none
- TE4-AC1 to AC5: pass for what is automated, with the gaps above
