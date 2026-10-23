# Worker change record - TE5 O3 (tighten the checks)

## Result
Full run `./scripts/verify.ps1 -SkipCi` (2026-10-22, exit 0): **509 passed, 0 failed, 0 skipped, 17 known**. The 404 existing checks are unchanged. Browser stage: 120 tests on 3 engines (103 PASS, 17 KNOWN, 0 FAIL) plus 2 stage self-checks (PASS): 8 screens x 4 checks + home reduced motion + smoke + home keyboard at 320 px + 5 self-tests = 40 per engine. Three consecutive stage-only runs (`VERIFY_STAGES=40`) were identical: 105 passed / 0 failed / 17 known, "120 of 120 listed tests reported".
No new real findings: the mobile menu is reachable and shows a focus indicator; the 17 known entries are unchanged (15 x `request-401:GET /api/auth/me`, 2 x WebKit date field). `known-issues.json` is unchanged.

## Files
- New: `verify/browser/helpers/keyboard.ts` (list, numbering and baseline styles; Tab walk), `helpers/axe.ts`, `helpers/viewport.ts`, `tests/keyboard-mobile.spec.ts`, `tests/selftest.spec.ts`.
- Changed: `helpers/known.ts` (pure `evaluate`, used by `settle` and the self-test), `helpers/each-screen.ts` (kind argument, tags `@screen:` and `@kind:`), `tests/{load,a11y,reflow,keyboard,motion}.spec.ts`, `verify/stages/40-browser.sh`, `Docs/architecture.md` section 10, `Docs/tests/README.md`.
- No application code, no change to other stages.

## Items
1. Focus indicator: the unfocused outline and box-shadow of every element are recorded before Tab; the focused style must show an outline or shadow AND differ from that record. Tab also starts from a marker at the document start (blur alone left Chromium and Firefox continuing from the clicked menu button).
2. Selector: adds iframe, audio and video with controls, contenteditable (also plaintext-only), summary (first in details), role=button|link|checkbox|tab|menuitem with tabindex, any tabindex. `tabIndex` reads -1 for contenteditable, so natural focusability is handled explicitly. New home test at 320 px with the menu opened: asserts the button and every drawer link are numbered, then Tab reaches them in order with a visible focus.
3. Axe defects are one per rule and element: `axe:<rule>:<target>`.
4. Orphan known issues: `40-browser.sh` reads `playwright test --list --reporter=json` (all engines, unfiltered) and fails an entry whose screen/kind pair, or `browsers` value, matches no test. A PASS line "known-issues entries each match a test" is recorded otherwise.
5. `assertViewportWidth(page, 320)` (innerWidth) before measuring, in the reflow test and the mobile keyboard test.
6. The stage counts its own failures (`stage_fail`) and compares the number of results with the number of listed tests (same name filter and engines): FAIL "browser results complete" naming the first missing tests, else PASS.
8. "Current result" line in `Docs/architecture.md` section 10 updated. Advisory 7 recorded there as a known limit.

## Red observed (honest account)
- First run of the new self-tests against the old logic (helpers extracted without behavior change): 12 of 15 FAIL (5 tests x 3 engines, minus the known-masking test which already passed because the old per-id matching was fine): static outline not flagged; selector missed `div#editable` (and by the list, iframe/media); axe gave one defect per rule instead of per element; the viewport assertion did not reject 400 px. After the implementation they all pass on all three engines. One self-test needed fixing (it counted other axe rules on a bare `setContent` page; now filtered to `image-alt`).
- First run of the mobile keyboard test: FAIL on all engines (Chromium `order:` and Firefox `unreachable:`): a defect of my check, Tab continued from the clicked menu button. Fixed with the start marker (see item 1); then green.
- Negative controls run by hand and removed: menu button given `tabindex=-1` in the mobile test: 3 FAIL "the menu button is among the interactive elements". Known-issues file with three bogus entries (unknown screen, unknown kind, unknown engine): 3 FAIL "orphan known issue". Reporter temporarily dropping one result: FAIL "browser results complete ... expected 3 ... got 2; first missing: [firefox] smoke". All files restored (verified by grep).
- Kept as self-tests: focus indicator change, selector coverage, axe element identity, known-entry masking, viewport width.

## Notes
- Self-test running order matters not; they use `page.setContent` and need no stack, but run inside the same stage.
- The orphan and completeness checks add two PASS lines to the stage (so 105 browser-stage passes = 103 tests + 2).
- No git write command was run. Everything is uncommitted in the working tree.
