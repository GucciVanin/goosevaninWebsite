# Worker report - F3-US1 order O1 (test-only)

No application code changed (each temporary mutation below was restored; `git diff` of `src/GooseWebsite.Api` and the Angular sources is empty apart from the new spec cases). No git write command, no credential printed or written, no valid contact message sent. Only `goosewebsite-*` containers were touched (the two stack containers were stopped by name at the end, as in the evidence run).

## Tests added and proof each can fail

| Gap | Test | Proof it fails |
| --- | --- | --- |
| GAP-1 (AC1) | Playwright `verify/browser/tests/auth.spec.ts`, "after a real sign-in the session cookie is invisible to script and only the marker is readable" (3 engines). Real UI sign-in as the seeded administrator; `document.cookie` names must equal `['goosewebsite.signedin']`; the browser's cookie jar must hold `goosewebsite.auth` as HttpOnly and Secure. | Mutant: `Cookie.HttpOnly = false` for `goosewebsite.auth` in `AccountsModule.cs`, rebuilt stack: failed on chromium, firefox and webkit ("Received + goosewebsite.auth"). Restored. |
| GAP-2 (AC4) | `verify/runtime/80-reader.sh`: two checks. A registration body with `role: "Admin"` and `isAdmin: true` gets 404 (registration is off in the stack); signing in with that email and password gets 401. | Mutant: the sign-in probe pointed at the seeded administrator instead: "expected 401 but got 200". Restored. The registration-on case stays in xUnit (it would send a verification email); recorded in the script comment. |
| GAP-3 (AC5), Karma | `sign-in-page.spec.ts`, 5 new cases: same generic message for an unknown-email failure and a wrong-password failure (the two mock errors carry different server text, so the page must ignore it), and the password is cleared; same message for any other failure (500); labels Email and Password tied to `#email` and `#password`; Sign out button shown when signed in, calls `logout`, then the form returns with "You have signed out."; no Sign out button when signed out. | Mutants in `sign-in-page.ts` (show the server's error text; remove the Sign out button; point the Password label at another id): exactly the 3 matching new cases failed (197 passed, 3 failed). Restored. |
| GAP-3 (AC5), browser | `auth.spec.ts`, "a wrong password and an unknown email show the same message, sign-in works, Sign out ends the session" (3 engines): identical alert text for both, sign-in shows "Signed in as", `/editor` opens, Sign out shows "You have signed out.", then `/editor` redirects to `/sign-in` and `/api/auth/me` is 401. | Mutant: Sign out button removed from the page: failed on all 3 engines at the Sign out step. Restored. |
| GAP-4 (AC3) | `auth.spec.ts`, "the session cookie is gone but the marker stays...": with the saved administrator session, clear only `goosewebsite.auth`, click Projects in the editor, expect `/sign-in?reason=session-expired&returnUrl=/editor` and the text "Your session expired. Sign in again to continue." | Mutant: the interceptor's redirect disabled (`if (false && ...)`): timed out waiting for the redirect on all 3 engines. Restored. |
| Dummy hash | xUnit `SignInTimingEqualisationTests.UnknownEmailWrongPasswordAndLockedAccountEachRunTheSameNumberOfHashVerifications` (see below). | Mutant: the `BurnDummyPasswordWork` call inside `if (user is null)` removed: "Expected: 2, Actual: 1". Restored. |

Test infrastructure added: `verify/browser/helpers/lock.ts` (cross-process mutex) and an optional `configureServices` argument on `AuthApiFactory` (test project only).

## Things worth knowing

1. Signing out rotates the account's security stamp, which ends every session of that account (this is F3-US1's own fix). The sign-out browser flow therefore uses the second seeded administrator (`VERIFY_ADMIN2_EMAIL` with `VERIFY_ADMIN2_NEW_PASSWORD`, the password after the operator reset test) and holds a lock so the three engines do not end each other's session. Using the first administrator would have invalidated the saved session that the editor screens use. The lock helper polls only for another test, never for the page.
2. First browser run failed on chromium only: the page clears the password field after a failed sign-in, and that clear can land after the next `fill`, leaving the button disabled. The test now waits for the field to be empty before typing again. This looks like a harmless race in the page (a person cannot type that fast), not a defect worth an order; flagging it.
3. Lockout: at most one wrong password per engine for the second administrator, followed by a correct sign-in that resets the count, so the limit of five is never approached.

## The dummy hash

It exists and the backlog claim is true. `AuthController` keeps a static dummy hash and `BurnDummyPasswordWork` verifies the supplied password against it through the injected `IPasswordHasher`. Count of hash verifications per failed sign-in, measured by the new test: unknown email 2 (the burn at the top of `Login` plus the burn in the unknown-user branch), wrong password 2 (the top burn plus the real verification in `PasswordSignInAsync`), locked account 2 (the top burn plus the lockout burn, since Identity skips the real check when locked). All three equal, so the claim "unknown-user and locked-account paths perform dummy password-hash verification" holds, and the test fixes it. Wording note only: the claim is about work equalisation by count, not measured time; the backlog wording ("to reduce credential-timing leaks") is accurate and needs no change. Minor observation, not a defect: the top-of-method burn runs for every request, so the second burn in the unknown branch is what makes unknown-user work equal to the known-user path (burn plus real verification).

## Documents updated

`Docs/tests/README.md` (browser table row for `auth.spec.ts`, a table of the other F3-US1 tests), `Docs/product-backlog.md` (acceptance evidence paragraph in F3-US1; status stays In Progress). F3-US1 is not marked Done.

## Final verification

| Check | Result |
| --- | --- |
| `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | 189 passed, 0 failed (188 before, +1) |
| Karma (`cmd /c` to file) | 200 SUCCESS (195 before, +5) |
| `./scripts/verify.ps1 -SkipCi`, run 1 | 790 passed, 0 failed, 0 skipped, 0 known, exit 0 |
| `./scripts/verify.ps1 -SkipCi`, run 2 | 790 passed, 0 failed, 0 skipped, 0 known, exit 0 |

790 = 773 (evidence runs) + 17: 1 xUnit, 5 Karma, 2 runtime, 9 browser (3 tests x 3 engines). Before the final two runs, the earlier runs of the new tests were: 3 consecutive isolated runs of the 3 browser tests x 3 engines passing, after fixing the race in note 2. The CI gate was skipped as in the evidence runs.
