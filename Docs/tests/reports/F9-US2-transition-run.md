# F9-US2 page transition - Testing run (orders O1 and O2)

Date 2026-10-28. Branch `r1-signoff`, HEAD 3e6c26c, with the uncommitted page-transition work (tree unchanged from the start of the run, see "Mutation proofs"). Docker stack `goosewebsite` only (`goosewebsite-web`, `goosewebsite-caddy`); no other container or process was touched.

## Results per run (three runs each, same commands as `Docs/tests/README.md`)

| Layer | Run 1 | Run 2 | Run 3 | Per-test lists |
| --- | --- | --- | --- | --- |
| `dotnet clean` then `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | 191 passed, 0 failed | 191 passed, 0 failed | 191 passed, 0 failed | 191 names + outcomes (from the .trx), identical in all three |
| Karma (`cmd /c "npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox > file 2>&1"`, Angular cache removed before each run) | 207 of 207 SUCCESS | 207 of 207 SUCCESS | 207 of 207 SUCCESS | spec-reporter lines (suite > test), identical in all three |
| `./scripts/verify.ps1 -SkipCi` (exit 0 each) | 824 passed, 0 failed, 0 skipped, 0 known | same | same | 824 PASS lines (timings stripped), identical in all three |

Determinism: no test differs between runs in any layer. Run 3 of verify was followed by the mutation runs and a fourth full verify after restoring (824 passed, 0 failed, 0 skipped, 0 known), so the restored code is also green.

Page-transition tests in `verify-results/latest.log`, present and PASS on chromium, firefox and webkit in each of the three runs (12 of 12 lines per run):
- a route change plays the transition and the header stays still (this test also holds the footer assertions: no footer old/new animation, footer box settled after 400 ms)
- a direct load shows the page with no transition
- home section scrolling is unchanged: no transition
- a route change navigates instantly and nothing animates (the reduced-motion test)

Note: the footer check is not a separate test; it is inside the first test above. Unit side: Karma `Page transition` (5 tests: first load, same page, reduced motion, same route different slug, no motion preference) and `App routing configuration > asks the browser for a view transition on a route change` also run and pass in every run (and appear in the verify log as `unit-angular`).

## Mutation proofs (assertions can fail)

Method: edit, rebuild the web image (`docker compose up --build -d --wait web caddy`), then browser stage only (`VERIFY_STAGES=40`, `VERIFY_BROWSER_GREP='route change|direct load shows the page|section scrolling'`; 12 tests). Original files were saved first and restored byte for byte (SHA-256 of `page-transition.ts` 7011AC6A..., `site.scss` 3EB4ACFB... matched after each restore; `git diff` and `git status` output equals the start snapshot).

| Mutation | Result | Failing tests |
| --- | --- | --- |
| (a) `page-transition.ts`: `if (reducedMotion() \|\| isSamePage(...))` changed to `if (isSamePage(...))` (reduced-motion skip disabled) | 11 passed, 3 failed | `a route change navigates instantly and nothing animates` on chromium, firefox and webkit. The assertion `animations toEqual([])` caught 47 recorded animations (`gcv-page-in`/`gcv-page-out` at 0.01 ms, the root group at 250 ms, and so on). The next assertion (`skipped === started`) was not reached; it was not proved separately. |
| (b) `site.scss`: removed `animation: none` from `::view-transition-old/new(gcv-footer)` | 11 passed, 3 failed | `a route change plays the transition and the header stays still` on chromium, firefox and webkit: the "no footer old/new animation" assertion caught 22 UA animations (`-ua-view-transition-fade-in/out`, `-ua-mix-blend-mode-plus-lighter`) on the footer pseudo-elements. |

Caveat on (b): the animations caught have `duration` 0 (the group's 0s duration applies in the engines), so the rule removes an animation object that is visually near-invisible; the assertion guards the rule, not a visible flicker. The footer box-settled assertion was not shown to fail.

## Observations (built-in browser, stack up, Chromium)

- Transition on `/projects` -> `/stories` and `/stories` -> `/` -> `/projects`, real clicks on the header links: a view transition started per navigation and `ready`/`finished` resolved. Recorded animations: `::view-transition-old(root)` `gcv-page-out` and `::view-transition-new(root)` `gcv-page-in`, 240 ms each; root group 250 ms (UA default); header and footer groups 0 ms; no old/new animation on header or footer images. Effect: a quick cross-fade with a small upward settle, header and footer stay still. A screenshot taken right after the click already showed the settled page (too slow to catch mid-fade), so the look is from the animation data and the keyframes in `site.scss`, not from a captured frame.
- Reduced motion on/off in the pane: not possible. The pane tool can emulate colour scheme and size but not `prefers-reduced-motion`; the Playwright reduced-motion test (engine setting `reducedMotion: 'reduce'`) is the only evidence for it. Pane showed `matchMedia('(prefers-reduced-motion: reduce)')` false.
- Tooling note: while the pane was hidden, navigations started by script (`element.click()` from JS) did not complete (path stayed `/projects`, `ready` never fired, a later skip recorded). Real pane clicks worked. One header click issued immediately after another (home then projects in the same batch) did not navigate; repeated alone it worked. Not reproduced under Playwright and not investigated; possibly an artefact of the hidden pane or of clicking while a transition is in flight, so a person should try rapid double clicks in a visible browser.
- Logo request abort (Worker's finding): `/verify-email` and `/contact/verify` loaded in the pane: all `/brand/gcv-logo-*.webp` requests returned 200, none aborted, the console had no errors, resource timing lists both sizes with status 200. The browser `load.spec.ts` (no failed request) passes for both screens on all three engines in every run. The `sizes`-before-`srcset` fix holds. The reason only these two pages exposed the race is still not explained (the Worker did not investigate either).
- No valid contact message was submitted.

## Not covered

- Real Edge and Safari, `Docs/tests/browser-manual-check.md` (manual); the three engines are Playwright builds.
- Reduced motion in a real browser pane, and any mid-transition visual (no frame captured).
- Mutation of the `skipped === started` assertion and of the footer box-settled assertion.
- Rapid successive navigation while a transition is running; mobile widths; slow devices (WebKit's route-change test took 6.6 to 10 s once, but passed within its limits every run).
- CI: `-SkipCi` was used (no pull request check run for this uncommitted work).
- Karma per-test lists used the project's `karma.conf.js` spec reporter (one line per test); the .NET lists come from the .trx files (`--logger trx`, an addition to the fixed command that does not change what runs).
