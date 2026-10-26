# Worker change record - F9-US7 O6 (corrections after the Reviewer, round 2)

## Result
Karma: 120 passed (119 before, 1 new). Full `./scripts/verify.ps1 -SkipCi` (2026-10-22, exit 0): **567 passed, 0 failed, 0 skipped, 0 known**. Three identical stage-only runs `VERIFY_STAGES=40`: each 128 passed, 0 failed, 0 known, "126 of 126 listed tests reported".

## Changed
1. `Docs/tests/browser-manual-check.md`: the contact line now asks Gustavo to confirm that a text error appears beside each invalid field after leaving it or pressing Send or Enter, that focus moves to the first invalid field, and that nothing is sent; the "button stays disabled" statement is gone.
2. `Docs/architecture.md` section 10: Current result now 567; the Content checks paragraph describes the O5 field-error checks (text error in the shared alert style, aria-invalid, aria-describedby, correction removes them, Enter shows every error and moves focus, button not disabled by validity, no request to /api/contact).
3. Focus: `contact-page.ts` injects `ElementRef`; `focusFirstInvalid(form)` focuses the first invalid control (by form-control order, found by `[name]`) on a submit attempt with an invalid form. New Karma spec; `contact.spec.ts` gets a focus assertion (defect `focus-not-moved-on-submit`), and the Enter step now starts from the email field so the check is meaningful (starting on the name field would pass trivially).
4. `contact.spec.ts`: the `expect.poll` timeout is 3000 ms (the single occurrence).

## Red observed
- Karma: the new spec failed against the unchanged component (focus stayed on email instead of moving to the reason select). Its first assertion (email is first invalid, already focused) passes by nature; the second assertion is the one that proves the change.
- Browser: stage 40 limited to `contact` against the old running app failed on chromium, firefox and webkit with "new defect focus-not-moved-on-submit: focus is on contact-email, expected contact-name". Green after the full rebuild.

## Notes
- The component files are LF in this tree (not CRLF as the brief said); edits kept LF.
- No git write command run. No valid message submitted. known-issues.json stays empty.
