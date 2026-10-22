# Test list and reports

The Testing role runs the list below as the last check, three times per test, and writes a report per run here.

## Commands (fixed, so every run is the same)

| Layer | Command | Notes |
| --- | --- | --- |
| .NET unit and API tests | `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | Build the client once first. |
| Angular unit tests | `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox` | |
| Container runtime tests | `./scripts/verify.ps1 -SkipCi` (PowerShell) | Resets and rebuilds the stack; writes `verify-results/`. Use the CI gate (without `-SkipCi`) once the branch has a pull request. |
| CI | `gh pr checks <number> --watch` | First check; must be green before the container run is reported. |

## Determinism

The container run uses seeded test administrators and a unique suffix per run for created content; the clock is real, so tests compare relative values only. Record the environment (commit, Docker image, date) in each report. A test whose result differs between its three runs is `fail` with reason `nondeterministic`.

## Dynamic list

The runtime tests are the files in `verify/runtime/` plus the xUnit and Karma suites; each story's acceptance tests map to tests by name (see the evidence paragraph of the story in `Docs/product-backlog.md`). A new acceptance criterion adds a test; a fixed security finding adds a regression test; a removed test is recorded in the work item's `decisions.md`.

## Browser tests (TE5)

Playwright and axe-core run as stage `40-browser` of `./scripts/verify.ps1`, inside the verification container, against the live stack, on Chromium, Firefox and WebKit (the engines behind Chrome and Edge, Firefox and Safari; real Edge and Safari are a manual pass, recorded in each report). Pinned: `@playwright/test` 1.64.1, `@axe-core/playwright` and `axe-core` 4.14.0 (`verify/browser/package.json` and lockfile); the engine builds are the ones that Playwright version installs, so rebuilding the image gives the same browsers.

| Check | File in `verify/browser/tests/` | Criterion | Runs on |
| --- | --- | --- | --- |
| Loads with no failed request and no console error | `load.spec.ts` | TE5-AC2 | every screen |
| axe-core WCAG 2.1 A and AA, fails on critical or serious | `a11y.spec.ts` | TE5-AC3 | every screen |
| 320 px: `scrollWidth` at most 320 | `reflow.spec.ts` | TE5-AC4 | every screen |
| Keyboard: every control reached in page order, focus indicator that differs from the unfocused look | `keyboard.spec.ts` | TE5-AC5 | every screen |
| Keyboard at 320 px with the mobile menu open (menu button and drawer links) | `keyboard-mobile.spec.ts` | TE5-AC5 | home |
| Self-tests of the checks (negative controls: static outline, selector coverage, axe element identity, known-issue masking, viewport width) | `selftest.spec.ts` | TE5-AC3 to AC5 | no application needed |
| Reduced motion: nothing animates, content present | `motion.spec.ts` | TE5-AC6 | home |
| Smoke: the layer works end to end | `smoke.spec.ts` | TE5-AC1 | home |

Screens (`verify/browser/helpers/screens.ts`): home, sign-in, email confirmation and contact confirmation (no token), access denied, and with the seeded test administrator the editor workspace, story form and project form.

### Run

| Goal | Command (PowerShell, repo root) |
| --- | --- |
| Everything, as the one command | `./scripts/verify.ps1 -SkipCi` |
| Only the browser stage, stack already up (after a first full run) | `$env:VERIFY_STAGES='40'; docker compose --profile verify build verify; docker compose --profile verify run --rm verify` |
| One engine | add `$env:VERIFY_BROWSERS='firefox'` (comma list of `chromium`, `firefox`, `webkit`) |
| One screen or check | add `$env:VERIFY_BROWSER_GREP='story-form'` (Playwright `--grep` on the test name) |

Clear the variables afterwards (`Remove-Item Env:VERIFY_STAGES, Env:VERIFY_BROWSERS, Env:VERIFY_BROWSER_GREP`). `docker compose build` is needed after any change under `verify/` (the tests are copied into the image; the browsers are a cached layer).

### Read a failure

- `verify-results/latest.log` has one line per test per browser: `PASS`, `FAIL` or `KNOWN`, then `browser  [engine] screen: check (ms) - detail`. The summary counts `known` apart from `passed` and `failed`.
- `verify-results/browser.jsonl.details.log` has the full evidence of every test that logged anything: each axe violation with its elements, each console error or failed request, the interactive elements found and the order Tab visited them. `verify-results/browser.out` is Playwright's own output.
- `browser results complete` fails when the number of results differs from the number of tests Playwright lists (screens x checks x engines), so a crash that drops tests cannot pass; the line names the first missing tests. The stage counts its own failures, not the global counter.
- A `new defect <id>: ...` failure is a real finding: fix the application, or (when it is fixed by a later story) add a known-issues entry. `known issue <id> no longer reproduces: remove this known issue` means the defect was fixed: delete its entry.

### Known issues

`verify/browser/known-issues.json` lists defects the tests found that a named story will fix: `{id, screen, kind, description, fixedBy, browsers?}`. `id` is the defect id printed in the failure (`axe:<rule>:<element selector>`, `request-401:GET /api/auth/me`, `overflow-x`, `no-focus-indicator:<element>`, `unreachable:<element>`, `order:<element>`, `running-animation:<name> on <element>`), `screen` the screen id, `kind` one of `load`, `a11y`, `reflow`, `keyboard`, `keyboard-mobile`, `motion`; `browsers` (optional) limits an entry to engines where the defect is engine-specific. A matching defect is reported `KNOWN` and never `PASS`; a new defect fails; an entry that no longer reproduces fails; an entry whose `screen`, `kind` or engine matches no test fails the stage ("orphan known issue"). An axe entry names the element as well as the rule, so it cannot hide the same rule on another element. The fixing story removes the entry in the same commit as the fix.

### Add a test

Add a screen: one line in `helpers/screens.ts`; every check then covers it. Add a check: a new `tests/<name>.spec.ts` that calls `eachScreen('<kind>', 'title', async ({ page, screen, info }) => {...})`, collects `Defect[]` (`{id, detail}`) and ends with `settle(info, screen.id, '<kind>', defects)`. Wait on conditions (`expect(...).toBeVisible()`, `expect.poll`), never on a clock. Never log tokens, passwords or message bodies; the administrator's credentials come from `.env.verify` and the session file is deleted after the run.
