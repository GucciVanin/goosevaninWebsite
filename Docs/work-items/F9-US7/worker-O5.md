# Worker change record - F9-US7 O5 (contact form field errors)

## Result
Full `./scripts/verify.ps1 -SkipCi` (2026-10-22, exit 0): **566 passed, 0 failed, 0 skipped, 0 known** (before O5: 558). Three identical stage-only runs `VERIFY_STAGES=40`: each 128 passed, 0 failed, 0 known, "126 of 126 listed tests reported". Karma: 119 passed (111 before, 8 new). No backend change, `dotnet test` not run separately (the full run executes it).

## Changed
- `features/contact/contact-page.html`: each field now sits in a `gcv-field` with an error paragraph directly after the control (`#contact-name-error`, `-email-`, `-reason-`, `-message-`; texts "Enter your name", "Enter a valid email address", "Choose a reason", "Write a message"), class `gcv-status gcv-status--alert`. The control gets `aria-invalid="true"` and `aria-describedby` only while the error shows. Labels untouched. The submit button is no longer disabled by form validity (only while sending), so Enter or a click on an invalid form gives the errors. Success `role="status"` block, honeypot and API call unchanged. The server error line also uses `gcv-status--alert` (still `role="alert"`).
- `contact-page.ts`: `shows(control)` (invalid and (touched or submit attempted)); `submit(form)` sets `attempted` and stops when invalid, otherwise posts the same body as before.
- `contact-page.scss`: `.form-error` reduced to `margin: 0` (border and colour come from the shared style).
- New `contact-page.spec.ts` (8 Karma specs). `verify/browser/tests/contact.spec.ts` rewritten for the new behaviour (no disabled-button assertions; error text, aria-invalid, aria-describedby, adjacency, alert style, correction removes them, Enter on a fresh empty form shows all four, zero requests to `/api/contact`, valid input is not submitted).

## Red observed
- Karma against the old component: 6 of 8 failed (four blur cases, malformed email, empty submit); the "no error before touching" and "valid submit posts and shows status" specs passed by nature (existing behaviour).
- Browser test on the old running app (chromium): FAIL with `submit-disabled`, `error-text:blank:*` for all four fields, `error-text:malformed:*`, and so on. My first version timed out at 45 s because of 5 s polls piling up on the old app; I lowered the poll to 1.5 s and re-ran red for the right reason (named defects).
- Green after the change on all three engines in the full run.

## Notes
- Axe scan on the home page still passes (full run). No valid message submitted.
- Decision: not disabling the button is the simplest accessible approach; the errors appear on blur or on submit attempt.
- Files `contact-page.html/.ts` kept CRLF; the new spec and the browser test are LF. Docs (`architecture.md`, `Docs/tests/README.md` row text) not edited; the README contact row still says incomplete input is "blocked and identified", which remains true. No git write command run.
