# Worker change record - TE5 O1 and O2

## Result
Full run `./scripts/verify.ps1 -SkipCi` (2026-10-22, exit 0): **489 passed, 0 failed, 0 skipped, 17 known**.
The 404 existing checks (unit-dotnet, unit-angular, runtime) all still pass unchanged. The browser stage adds 102 results: 85 PASS, 17 KNOWN, 0 FAIL (3 engines x 34 tests: 8 screens x 4 checks + home reduced motion + smoke = 34). Three consecutive stage-only runs (`VERIFY_STAGES=40`) gave identical totals (85 / 0 / 17).

## Files changed or added
- `verify/browser/` (new): `package.json`, `package-lock.json`, `playwright.config.ts`, `known-issues.json`, `helpers/` (`forward.cjs`, `global-setup.ts`, `global-teardown.ts`, `paths.ts`, `screens.ts`, `each-screen.ts`, `page.ts`, `known.ts`, `jsonl-reporter.ts`), `tests/` (`smoke`, `load`, `a11y`, `reflow`, `keyboard`, `motion` `.spec.ts`).
- `verify/stages/40-browser.sh` (new).
- `Dockerfile.verify`: pinned Playwright install with `--with-deps chromium firefox webkit`, `PLAYWRIGHT_BROWSERS_PATH=/ms-playwright`, cached layer before the sources.
- `verify/lib.sh`: `KNOWN` result and counter. `verify/run.sh`: `known` in the JSON and the summary line, and `VERIFY_STAGES` (prefix regex) to run only some stages.
- `docker-compose.yml` (verify service): passes `VERIFY_STAGES`, `VERIFY_BROWSERS`, `VERIFY_BROWSER_GREP`.
- `scripts/verify.ps1`: result line also prints the known count.
- `.gitignore`: `verify/browser/node_modules/`.
- `Docs/tests/README.md` (browser section: run all/one engine/one screen, read a failure, known issues, add a test) and `Docs/architecture.md` section 10 (stage table, results, "Browser tests (TE5)" paragraph).
- No application code touched.

## Versions pinned
`@playwright/test` 1.64.1 (npm "latest" on 2026-10-22), `@axe-core/playwright` 4.14.0, `axe-core` 4.14.0, exact versions plus lockfile (`npm ci`). Browser builds are the ones Playwright 1.64.1 installs (Chromium, Firefox, WebKit) into `/ms-playwright`. Node 22.15.0 (existing image).

## Design notes
- Certificate and host name: `helpers/forward.cjs` listens on 127.0.0.1:443 and [::1]:443 inside the verification container and pipes to Caddy (`CADDY_IP`), so all three engines use the real `https://localhost`; the Caddy local certificate is accepted with `ignoreHTTPSErrors`. No hosts-file or per-engine DNS tricks.
- Administrator: `global-setup.ts` signs in through the API (CSRF token, then `/api/auth/login`) with `VERIFY_ADMIN_EMAIL/PASSWORD` from `.env.verify` (same source as the runtime tests); the session cookie file is in the container temp dir and deleted by `global-teardown.ts`. Credentials are never printed. Only the status code appears in an error.
- Result format: a custom reporter writes one JSON line per test per engine; `40-browser.sh` replays them (sorted by name) through `record`, so the log line, `latest.jsonl` and `latest.json` are the same as other stages (stage `browser`, name `[engine] screen: check`). Full evidence (every axe violation with elements, defects, interactive elements found and Tab order) goes to `verify-results/browser.jsonl.details.log`; Playwright output to `verify-results/browser.out`.
- Known-issues mechanism (`helpers/known.ts`): every check collects `{id, detail}` defects and ends with `settle(info, screen, kind, defects)`. Defect not in the file: FAIL "new defect ...". Entry not reproduced: FAIL "known issue ... no longer reproduces: remove this known issue ...". All defects known: result KNOWN (counted separately). Entry fields as ordered plus optional `browsers` for engine-specific defects.
- Checks: AC2 failed requests (status >= 400, requestfailed), `pageerror`, console errors (Chromium's generic "Failed to load resource" line is dropped because the same refusal is reported as the request). AC3 axe tags wcag2a, wcag2aa, wcag21a, wcag21aa; only critical or serious block, others are logged. AC4 max(document, body scrollWidth) <= 320 at a 320x640 viewport. AC5 numbers interactive elements in document order (open shadow roots included), presses Tab from a blurred start until all were seen, checks coverage, first-seen order, and outline or box-shadow on the focused element; a multi-stop control (date field) needs an indicator on at least one stop where it holds `:focus`. AC6 `reducedMotion: 'reduce'`, asserts the media query matches, content present (h1, `gcv-scene` with shadow children, body text over 200 chars) and polls up to 3 s for zero running animations/transitions over the document and every open shadow root.
- Waits are on conditions (networkidle, expect, expect.poll); no wall-clock sleeps in tests.
- Smoke test: home loads with HTTP 200, title and h1 on all three engines.

## Observed red (honest account)
1. Stage added before the image had Playwright: `FAIL browser tests ran - npx: command not found` (exit 127, no results). Fixed by the Dockerfile install and calling `node_modules/.bin/playwright`.
2. First full spec run: 19 FAIL: 15 x `request-401:GET /api/auth/me` and 4 x `no-focus-indicator` on the date fields (Chromium and WebKit). Triage below: the 4 were partly a defect in my keyboard check (a date field has several Tab stops, the picker stop reports the input as `activeElement` without `:focus`); fixed the check, which removed the Chromium ones and left the WebKit ones, which are real.
3. After the fix, one full run had 2 FAIL (WebKit date fields) until they were recorded in `known-issues.json`.
4. Negative controls (scratch specs, not kept): axe on injected `<img>` without alt and an empty button reported `image-alt` and `button-name` critical; the home page has 22 running CSS animations without the preference (so the reduced-motion pass is not vacuous); a bogus known-issues entry (`axe:color-contrast` on home) made the test FAIL with "no longer reproduces: remove this known issue".

## Findings recorded in `verify/browser/known-issues.json` (all `fixedBy: "F9-US7"`)
1. `request-401:GET /api/auth/me`, screens home, sign-in, email-confirmation, contact-confirmation, access-denied (kind `load`, all three engines = 15 KNOWN results). Evidence: every page load by a signed-out visitor sends `GET /api/auth/me` and the API answers 401 (header session probe); the browser reports a failed request (red line in Chromium's console). Suggested fix: anonymous probe answering 200, or no probe when there is no session cookie. Not seen on the editor screens (signed in).
2. `no-focus-indicator:input#story-date ""` (story-form) and `...input#project-date ""` (project-form), kind `keyboard`, WebKit only. Evidence (scratch probe): Tab onto the date field gives `:focus` true but `:focus-visible` false, `outline-style: none`, no box-shadow, so the global `input:focus-visible` outline in `components.scss` never applies; Chromium and Firefox apply it. Suggested fix: style `:focus-within`/`:focus` for date inputs too. 2 KNOWN results.

No axe critical or serious violation, no 320 px overflow, no keyboard reachability or order problem, no running animation under reduced motion, and no console error were found on any screen on any engine. axe also reported no non-blocking violations (nothing in the details log).

## Notes for the Captain
- Image build took about 9 minutes the first time (browser downloads); later builds reuse the cached layer.
- Quick loop after the first full run: `$env:VERIFY_STAGES='40'; docker compose --profile verify build verify; docker compose --profile verify run --rm verify`, optionally with `VERIFY_BROWSERS` and `VERIFY_BROWSER_GREP`.
- `Docs/architecture.md` section 10 still shows the 2026-10-14 totals line ("222 checks") as historical; I did not rewrite it. Backlog and decision-log updates for TE5 are left to the Captain.
- No git write command was run. Untracked: `verify/browser/`, `verify/stages/40-browser.sh`.
