# Testing report - F9-US7 (accessibility, responsiveness and browser support of the new design)

Role: Testing. No source file was edited and no git write command was run.

## Environment

| Item | Value |
| --- | --- |
| Date | 2026-10-22 (real clock, local PDT, about 18:35 to 18:46) |
| Git | branch `f9-emails-and-verification`, HEAD `360cefb7f568e9d4249455a7310b00570acf7642` (TE5). The F9-US7 work is UNCOMMITTED: 28 entries in `git status` (19 modified, 9 untracked: `Docs/tests/browser-manual-check.md`, `Docs/work-items/F9-US7/`, `auth-api.spec.ts`, `session-marker.ts`, `contact-page.spec.ts`, `approach.spec.ts`, `SessionMarkerCookieTests.cs`, `verify/browser/tests/contact.spec.ts`, `profile.spec.ts`). The status was identical before and after my runs |
| OS | Windows 11 Pro 10.0.26200 |
| .NET SDK | 10.0.300 |
| Node | v22.15.0 (host; the verification image uses node 22.15.0-bookworm-slim) |
| Browser for Karma | Chrome 155.0.0.0 headless (`ChromeHeadlessNoSandbox`) |
| Docker | 29.8.2 (Docker Desktop); `goosewebsite-web` image 8574608592cc and `goosewebsite-verify` image a17ee4e58c15 (both built 2026-10-22 18:25 to 18:26 PDT, before my runs; each run rebuilt using the layer cache) |
| Playwright / axe | `@playwright/test` 1.64.1, `@axe-core/playwright` 4.14.0, `axe-core` 4.14.0 as pinned in `Docs/tests/README.md` and `verify/browser/package.json`. I did not extract the engine build numbers |
| Known issues | `verify/browser/known-issues.json` is `[]` |

## Commands

- A: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (plus a TRX logger for per-test names)
- B: `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`
- C: `./scripts/verify.ps1 -SkipCi` (PowerShell, repo root, one run at a time, started from a wrapper script that only ran it three times and copied `verify-results/latest.*`). CI is not applicable until the Captain pushes.

## Totals per suite and run

| Suite | Run 1 | Run 2 | Run 3 | Result |
| --- | --- | --- | --- | --- |
| A .NET (xUnit) | 175 pass, 0 fail, 0 skip | 175 pass, 0 fail, 0 skip | 175 pass, 0 fail, 0 skip | pass |
| B Angular (Karma) | 120 of 120 SUCCESS, exit 0 | 120 of 120 SUCCESS, exit 0 | 120 of 120 SUCCESS, exit 0 | pass |
| C Container verify | 567 pass, 0 fail, 0 skip, 0 known, exit 0 | 567 pass, 0 fail, 0 skip, 0 known, exit 0 | 567 pass, 0 fail, 0 skip, 0 known, exit 0 | pass |

C breaks down as 175 unit-dotnet + 120 unit-angular + 144 runtime + 128 browser = 567. The 128 browser results are 126 Playwright results (3 engines x 42, listed below) plus the two stage self-checks (`known-issues entries each match a test`, `browser results complete - 126 of 126 listed tests reported`). Per engine: 42 results each on chromium, firefox and webkit, all PASS.

Per-engine check list (42): for each of 8 screens (home, sign-in, email-confirmation, contact-confirmation, access-denied, editor-workspace, story-form, project-form) the four checks `loads with no failed request and no console error`, `has no critical or serious WCAG 2.1 A/AA violation`, `fits a 320 px viewport without sideways scrolling`, `keyboard: every control is reachable in page order with a visible focus` (32); plus on home `keyboard at 320 px with the mobile menu open`, `reduced motion stops every animation and keeps the content`, `shows the name, positioning, bio and summary, the portrait and the profile links` (profile), `the contact form has its fields, shows a text error beside each invalid field without sending a request, and keeps labels with the fields` (contact) (4); `smoke: the home page loads` (1); 5 selftests (5).

## Irregular runs (nothing hidden)

- Before suite B I found leftover processes from an earlier Karma run (started 11:38 AM the same day: `npm test`, its node child and 8 Chrome processes with a `karma-` profile). I killed them. They did not take part in any counted run.
- During suite B run 1 two node processes were still alive right after exit 0 (the npm shell wrapper winding down); `npm test` exited by itself with exit 0 in all three runs and I stopped the leftovers. Runs 2 and 3 left none.
- No Docker network or DNS failure occurred; all three container runs were counted. The container runs were fast because the images and browser layer were cached.
- Karma prints no test names; its per-test results come from the `unit-angular` stage of the verify log (120 named tests).

## Determinism

- A: sorted per-test name+outcome lists from the three TRX files: identical (175 lines each, all Passed; `fc` reports no differences).
- C: sorted per-test lists (result, stage, test, duration removed) of the three counted runs: 567 lines each, `fc` reports no differences, including all 128 browser results on the three engines. 0 FAIL, 0 SKIP, 0 KNOWN in every run.
- Unit-dotnet (175) and unit-angular (120) in C match suites A and B.
- Nondeterministic cases: none.

## Coverage of the acceptance criteria (all results below: pass in 3/3 runs)

| AC | Covering tests | Result |
| --- | --- | --- |
| F9-US7-AC1 contrast WCAG AA, state not by colour alone | `[chromium/firefox/webkit] <screen>: has no critical or serious WCAG 2.1 A/AA violation` on 8 screens (24 results; axe includes colour-contrast); selftests `an axe violation is identified by rule and element` and `a known axe entry for one element does not mask another element`; text-error specs: Karma `leaving an empty contact-name/email/reason/message shows "..." next to it, tied with aria-describedby` and the browser contact check (error text beside each invalid field) | pass for contrast on every screen and engine and for state-in-words on the contact form. The "not colour alone" part on other screens is NOT checked automatically (see below) |
| F9-US7-AC2 320 px | `[engine] <screen>: fits a 320 px viewport without sideways scrolling` (24 results); selftest `the 320 px check asserts the real viewport width`. This checks `scrollWidth` only; clipped content is not separately detected | pass |
| F9-US7-AC3 keyboard, order, visible focus (incl. WebKit date fields) | `[engine] <screen>: keyboard: every control is reachable in page order with a visible focus` (24 results; story-form and project-form on webkit now PASS with no known entry); `[engine] home: keyboard at 320 px with the mobile menu open` x3; selftests `a focus indicator must be a change from the unfocused state`, `the reachable-element selector covers every kind of control`; Karma `a submit attempt on an invalid form moves keyboard focus to the first invalid field` | pass |
| F9-US7-AC4 reduced motion | `[engine] home: reduced motion stops every animation and keeps the content` x3. There is no separate per-screen motion test for the other screens | pass on home; other screens not separately asserted |
| F9-US7-AC5 latest two versions of Chrome, Edge, Firefox, Safari | The whole browser stage on chromium, firefox and webkit: 42 results per engine, all PASS in all runs | pass on the three engines only. Real Edge, real Safari and previous versions NOT tested (below) |
| F9-US7-AC6 F1 and F2-US1 re-run on the new design | Browser: `home: shows the name, positioning, bio and summary, the portrait and the profile links` x3 (F1-US1 tests 1 and 2; profile links, portrait alt and load, no age/address); `home: the contact form has its fields, shows a text error beside each invalid field without sending a request, and keeps labels with the fields` x3 (F2-US1 tests 1, 2, 4; no request to `/api/contact`). Karma: `shows the positioning line, the short bio and the professional summary in visible text`, `shows each published project as a panel with its stack and a plain-text summary`, `cuts a long summary at a word boundary`, the four `leaving an empty contact-... shows ...` specs, and the first-invalid-field focus spec. xUnit: `ContactVerificationApiTests.*`, `ContactSubmissionModuleTests.*` (honeypot, rate limit, no logging of address or body, verification keeps the pending message), `ContactFeatureTests.*`, `ContactEmailTemplateTests.*` | pass for what can be automated; F2-US1 test 3 (valid submit) is covered only by xUnit/Karma |
| F9-US7-AC7 zero critical or serious axe violations, known entries removed | same 24 axe results as AC1; `verify/browser/known-issues.json` is `[]`; 0 KNOWN in all three container runs; the previously known `request-401:GET /api/auth/me` (5 screens) and WebKit date-focus entries no longer appear. Supporting: xUnit `SessionMarkerCookieTests.*` (marker cookie flags, no identity, not set on failed sign-in, cleared at sign-out, session cookie still HttpOnly) and Karma specs `makes no request to /api/auth/me without the marker ...`, `clears the marker when the API answers 401`, `clears the marker after signing out`, `sends a visitor with no sign-in marker to sign in without asking the API` | pass |

## NOT covered

- Real Microsoft Edge and real Safari, and previous browser versions: only the Chromium, Firefox and WebKit engines of Playwright 1.64.1 run. The manual checklist `Docs/tests/browser-manual-check.md` has not been run by anyone; AC5 is therefore automated-on-engines only.
- "State not conveyed by colour alone" outside axe and the contact-form text-error specs (no automated check; it is on the manual checklist, also not run).
- F2-US1 test 3 (valid submit) in the browser: deliberately not submitted (the stack may have real mail); covered only by xUnit/Karma.
- Users who were already signed in before the deploy (they have a session cookie but no marker cookie, so the client will not probe `/api/auth/me` and will show them as signed out until they sign in again): not tested.
- Per-screen reduced-motion assertion outside home; detection of clipped (not overflowing) content at 320 px; requests that start after `networkidle`; iframe content and positive `tabindex`; visual regression and Lighthouse (out of scope per TE5). Engine build numbers were not recorded. CI: not applicable (nothing pushed).

## Failures

None in counted runs.

## Verdicts

- A .NET: 175 pass / 0 fail / 0 known, 3/3 runs - pass
- B Angular Karma: 120 pass / 0 fail / 0 known, 3/3 runs - pass
- C Container verify: 567 pass / 0 fail / 0 known (includes 128 browser results: 3 engines x 42 plus 2 stage self-checks), 3/3 runs, per-test lists identical - pass
- Nondeterministic cases: none
- AC1 to AC4, AC6, AC7: pass for what is automated, with the gaps listed above; AC5: pass on the three engines, real Edge and Safari and previous versions not verified (manual checklist not run)
