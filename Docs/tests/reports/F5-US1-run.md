# F5-US1 test run (orders O1 to O3)

Role: Testing. Branch `f5-public-browsing`, base commit `514fbe1` plus the uncommitted F5-US1 working tree. Date 2026-10-28. Windows 11, Docker stack `goosewebsite` rebuilt by `./scripts/verify.ps1 -SkipCi` (no pull request yet, so the CI gate was not run). Chrome 156 for Karma; Playwright 1.64.1 engines for the browser stage.

## Result: PASS

No test failed in any of the three runs of any layer, and the per-test lists are identical across runs.

## Totals per run

| Layer | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- |
| .NET (`dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`, client built once first) | 175 passed, 0 failed | 175 passed, 0 failed | 175 passed, 0 failed |
| Karma (`ChromeHeadlessNoSandbox`) | 165 of 165 SUCCESS | 165 of 165 SUCCESS | 165 of 165 SUCCESS |
| `./scripts/verify.ps1 -SkipCi` | 649 passed, 0 failed, 0 skipped, 0 known, exit 0 | same | same |

Container run breakdown (each run): unit-dotnet 175, unit-angular 165, runtime 145, browser 164 (chromium, firefox, webkit).

## Determinism

- .NET: test name and outcome lists from the three TRX files are identical.
- Karma: the sorted per-test lists (165 lines) are identical.
- Container: the sorted `latest.log` result lines of the three runs (649 each, durations stripped) are identical. No test is `nondeterministic`.

Note: Karma exits by itself when run directly with output redirected to a file. A first attempt piped through PowerShell variable capture did not return within 10 minutes and was stopped as a background task; this was a harness artefact, not a test hang (the redirected runs each took well under a minute). Lingering Chrome and node processes from that attempt could not be killed by this role (permission denied); they should be checked.

## New F5-US1 tests seen passing in every run

- Runtime: `route /projects serves the app`, `route /projects/a-project-that-does-not-exist serves the app`.
- Angular: projects index, empty state, load-failure message, home preview of at most three projects, Projects card and header link, home-from-/projects navigation with scroll, page titles (index, project, not found), editor project tests.
- Browser (all three engines): screens `projects` (load, axe, 320 px, keyboard) and `projects-empty` (direct load with header and footer, axe, no sideways scroll at 1280 and 375 px).

## Manual look at the running stack (after the last run)

Browser pane, `https://localhost`:
- `/projects` (empty state): title "Projects - Gustavo Couto Vanin"; heading PROJECTS, intro line "What Gustavo has built, and the part he played in each.", and the empty message "Projects will appear here as soon as Gustavo publishes them." Header (Projects item highlighted) and footer present.
- `/projects/nope`: title "Page not found - Gustavo Couto Vanin"; heading PAGE NOT FOUND, message and a "Back to the home page" link, with header and footer. Observation (minor, not a failure): the header still highlights the Projects item on this not-found address.
- 375 px: both pages fit (`scrollWidth` 375, no sideways scroll); the header collapses to icons and a menu button; text wraps cleanly; the empty message and the 404 message are readable.
- The contact form was not touched; no contact message was submitted.

## Not covered

- A real published project in a browser (list with items, project detail page, cover image): no published project exists in the stack, and the browser tests cover only the empty state. The populated list and detail page are covered only by Angular unit tests with a mocked API.
- Real Microsoft Edge and real Safari: not run (Chromium, Firefox and WebKit engines only; the manual checklist `Docs/tests/browser-manual-check.md` was not performed).
- CI on GitHub (no pull request yet).
- Window sizes other than 1280, 375 and 320 px; screen readers; the unauthenticated 404 route for other unknown top-level paths beyond what the runtime route test checks.
