# Testing report 2 - TE4 (remove sample content), orders O1 to O6

Role: Testing. No source file was edited and no git write command was run. This report is for the whole item after the 2026-10-27 amendment (AC6, AC7); the earlier report `TE4-run.md` covered O1 to O3.

## Environment

| Item | Value |
| --- | --- |
| Date | 2026-10-27 (real clock, local, about 22:55 to 23:07) |
| Git | branch `r1-closeout`, HEAD `3844f48ed1687649c763ba28b14f89ef3ee3e9e7` ("Remove sample content (TE4)"). The O4 to O6 work is UNCOMMITTED: 38 entries in `git status --porcelain`, the same count before and after my runs |
| OS | Windows 11 Pro 10.0.26200 |
| .NET SDK / Node | 10.0.300 / v22.15.0 |
| Browser for Karma | Chrome Headless 156.0.0.0 (`ChromeHeadlessNoSandbox`) |
| Docker | 29.8.2; images after the runs: `goosewebsite-web` 25e28bd169ed, `goosewebsite-verify` 0d9bef36a188 |
| Playwright / axe | as pinned in `Docs/tests/README.md` (1.64.1 / 4.14.0): chromium, firefox, webkit |
| Known issues | `verify/browser/known-issues.json` is `[]` |
| CI | not applicable (nothing pushed) |

## Commands (as in `Docs/tests/README.md`)

- A: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (with a TRX logger for per-test names), three runs one after another.
- B: `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`, three runs one after another, each started by a wrapper that would end the process tree it started if `npm test` was still alive 90 s after `TOTAL`.
- C: `./scripts/verify.ps1 -SkipCi` in PowerShell from the repo root, three runs one after another (never two at once); after each the wrapper copied `verify-results/latest.log` and `latest.json`.

## Totals per suite and run

| Suite | Run 1 | Run 2 | Run 3 | Result |
| --- | --- | --- | --- | --- |
| A .NET (xUnit) | 175 pass, 0 fail, 0 skip | 175 pass, 0 fail, 0 skip | 175 pass, 0 fail, 0 skip | pass |
| B Angular (Karma) | 139 of 139 SUCCESS | 139 of 139 SUCCESS | 139 of 139 SUCCESS | pass |
| C Container verify | 585 pass, 0 fail, 0 skip, 0 known, exit 0 | 585 pass, 0 fail, 0 skip, 0 known, exit 0 | 585 pass, 0 fail, 0 skip, 0 known, exit 0 | pass |

C breaks down as 175 unit-dotnet + 139 unit-angular + 143 runtime + 128 browser = 585. The 128 browser results are 126 Playwright results (3 engines x 42) plus the two stage self-checks. Compared with the first TE4 report (574): Karma +11 (128 to 139: tag filter, three cards, About, header link), .NET and runtime and browser unchanged.

## Irregular runs (nothing hidden)

- Suite B: in all three runs `npm test` exited by itself with exit code 0 after printing `TOTAL: 139 SUCCESS`. No process had to be stopped. (In the earlier TE4 report it did not exit; this time it did, so the hang is intermittent.)
- Suite C: no Docker network or DNS failure; all three runs were counted. Each took about 3 minutes (image layers and browser layer cached).
- A pre-existing `node scripts/test.mjs` process (PID 34440, not started by me) was running throughout and was left alone. Other Docker stacks (csanalyzer, deploy) were also running on the machine and were not touched.
- The stack is left running after the last container run, with its data as the run left it (`GET /api/blog` returns `[]`).

## Determinism

- A: sorted per-test name+outcome lists from the three TRX files: 175 lines each, all Passed, identical.
- B: sorted per-test lists (names and outcomes, durations removed) from the three Karma outputs: 140 lines each (139 tests plus the Karma start line), identical.
- C: sorted per-test lists (result, stage, test; durations removed) of the three counted runs: identical (`cmp` reports no difference), including all 128 browser results on three engines. 585 PASS, 0 FAIL, 0 SKIP, 0 KNOWN in each run.
- Nondeterministic cases: none.

## Coverage of the acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| TE4-AC1 no hard-coded content; starts empty; empty and failure states in words | Karma `BlogPage`: `shows a loading state and no story before the API answers`, `says in words that nothing is published when the API returns none`, `says in words that loading failed and keeps the section usable` (3/3 runs). `GET /api/blog` on the stack returned `[]` after the runs | pass |
| TE4-AC2 search, category filter, reading view with API data; browser tests pass on the empty state with axe clean | Karma `BlogPage with two published stories`: `lists both and builds the categories from the data`, `filters by search text`, `filters by category`, `opens the reading view and goes back`; also `renders the Markdown body safely in the reading view`. Browser stage on the home page (empty Stories state, all three engines, 3/3 runs, 0 KNOWN): `has no critical or serious WCAG 2.1 A/AA violation`, `loads with no failed request and no console error`, `fits a 320 px viewport without sideways scrolling`, `keyboard: every control is reachable in page order with a visible focus`, `keyboard at 320 px with the mobile menu open`, `reduced motion stops every animation and keeps the content` | pass |
| TE4-AC3 `/design` removed | Runtime (`30-pages.sh`): the six remaining route checks (`/a-route-that-does-not-exist`, `/access-denied`, `/contact/verify`, `/editor`, `/sign-in`, `/verify-email`) pass 3/3; `grep design verify/runtime/30-pages.sh` finds nothing; `src/GooseWebsite.Client/src/app/features` holds only `auth, blog, contact, editor, home`. On the stack `/design` answers 200 with the app fallback page like any unknown route (status code only; I did not render it) | pass |
| TE4-AC4 no sample content | My own search over every tracked and untracked non-ignored file (352 files after excluding `Docs/work-items`, `Docs/tests/reports`, `node_modules`, `wwwroot`, build output, lockfiles, images), case-insensitive fixed strings: `Pulseboard`, `Atlas Finance`, `Fieldnotes`, `The first signal`, `Threat modeling my morning`, `Shipping a tiny tool`, `Clarity first`, `Human by default`, `Ship with intent`, `I care about the details`, `Signals from the field`: 0 matching files. `Azure`, `Observability`, `Event-driven` under `src/GooseWebsite.Client/src/app/features/home` and `shared`: 0 matches. `toolkit` or `skills` in client source: only the word "skills" inside Gustavo's professional summary in `shared/models/profile.ts` (his own text, kept by AC6). The runtime tests that create content clean up, and the verification data volume is reset on every run | pass |
| TE4-AC5 operator note | READ, not executed: `Docs/architecture.md` known-gaps row (line 635) says existing local databases may hold demo posts, to back up `Data/goosewebsite.db` first, then delete them in the editor or run `DELETE FROM BlogPosts WHERE Title IN (...)` with `sqlite3` after listing with `SELECT Id, Title FROM BlogPosts;`, and marks TE4 done. The SQL was not run | pass (reading only) |
| TE4-AC6 no unwritten statements; Skills removed; About keeps portrait, name, positioning line, bio, summary; neutral Stories heading and intro | Karma `About (Approach)`: `shows the name as a caption beside the portrait, visibly`, `shows the positioning line, the short bio and the professional summary in visible text`, `shows at most the broad location and no age or street address`, `has the plain heading About and none of the removed statements`, `keeps a descriptive alt text on the portrait`; `BlogPage`: `uses a neutral heading and intro that claim nothing`; `Landing page`: `never links to a removed section`. Browser `profile.spec` (`shows the name, positioning, bio and summary, the portrait and the profile links`, chromium, firefox, webkit, 3/3 runs) fails on a removed statement, the `#skills` section or `a[href="#skills"]`. The toolkit component directory is gone (repository search above) | pass. The open question on the "Field notes" editor default label is a human decision, not tested |
| TE4-AC7 three cards, their targets, the tag filter, nothing links to a removed section | Karma `Landing page`: `shows the three category cards, each with an image strip and a link to its section`, `never links to a removed section`, `pre-applies the competition tag only from the Competitions card, and clears it from the others`. Karma `BlogPage tag filter from the Competitions card` (O5 and O6): `shows every story while no tag is set`, `shows only the stories tagged competition (case-insensitive) once the tag is set, and says so`, `still says "No story found" for a search with no match while no tag is set`, `clears the tag from the clear button`, `says no stories carry the tag when none matches, and keeps the clear button`, `does not show the tag status line in the reading view, and shows it again on the list`. Karma `SiteHeader mobile menu`: `clears the story tag from the Stories link in the header and in the menu, but not from other links`. Browser `profile.spec` checks the card titles are exactly `Projects|Life stories|Competitions` on three engines (3/3 runs); axe, keyboard, 320 px, keyboard at 320 px with the mobile menu, and reduced motion on the home page pass on chromium, firefox and webkit (3/3 runs). The old four card images are deleted from the working tree (they show as deleted in `git status`) and the three new ones are untracked | pass |

## NOT covered

- The Stories section with real published stories in a real browser: the browser stage runs against a stack with no published stories, so only the empty state is seen in a browser. Search, category filter, reading view, the Competitions tag filter (matching, the "No stories tagged #competition yet." message, the clear button, the header link clearing the tag) are covered by Karma with a stubbed API only, not end to end in a browser. The click-through from the Competitions card to the filtered Stories section is not run in a browser either.
- The Stories failure state ("could not load") in a browser: Karma only.
- Card images: the tests check that each card has an image strip and alt text, not that the pictures look right; no visual check was done by me.
- The operator note (AC5) was read, not executed; no SQL was run against any database; databases that already hold demo posts were not tested.
- The "Field notes" default label question in AC6 is a human decision.
- Real Edge and Safari and previous browser versions: the manual checklist `Docs/tests/browser-manual-check.md` was not run by me. Chromium, Firefox and WebKit engines ran automatically.
- CI: not applicable until the Captain pushes.

## Failures

None in counted runs.

## Verdicts

- A .NET: 175 pass / 0 fail / 0 known, 3/3 runs - pass
- B Angular Karma: 139 pass / 0 fail / 0 known, 3/3 runs, exited by itself with code 0 in all three - pass
- C Container verify: 585 pass / 0 fail / 0 known (128 browser results), 3/3 runs, per-test lists identical - pass
- Nondeterministic cases: none
- TE4-AC1 to AC7: pass for what is automated, with the gaps above
