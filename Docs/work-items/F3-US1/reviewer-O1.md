# Reviewer verdict - F3-US1 order O1

Verdict: ACCEPT (no blocking findings). Two low-severity notes and one optional hardening below.

## Checks

| Check | Result |
| --- | --- |
| Application code unchanged | Yes. `git diff HEAD -- src/GooseWebsite.Api` is empty. `sign-in-page.ts` shows as modified only by line endings (`git diff --ignore-cr-at-eol` is empty). Changed files are only the Karma spec, the `AuthApiFactory` test helper (optional `configureServices`, additive), `verify/`, the new xUnit file and docs. |
| `dotnet test` (reviewer run) | 189 passed, 0 failed. |
| Karma (reviewer run, `cmd /c` to file) | 200 SUCCESS. |
| Tests are not tautologies | Read all of them. Each asserts the criterion's observable outcome (cookie names readable by script, HttpOnly/Secure in the jar, identical alert text for two failure kinds where the mocked server text differs, redirect URL with `reason=session-expired`, 401 from `/api/auth/me` after sign-out, hash-verification counts). Mutation proofs in `worker-O1.md` are credible and cover each. |
| Dummy-hash test | Measures what it claims: it decorates `IPasswordHasher<ApplicationUser>` and counts `VerifyHashedPassword` calls per failed login: unknown email 2, wrong password 2, locked account 2 (the lock is reached after the fifth wrong attempt, one measured plus four in the loop). I confirmed against `AuthController.Login`: the top-of-method burn, the unknown-user burn, the real check in `PasswordSignInAsync`, and the lockout burn. It asserts `unknown > 0`, so removing the dummy work fails it (worker's mutant: 2 became 1). It proves equal work by count, not time; the test comment and README say so. |
| Cross-process lock and second administrator | Sound for the intended use. The first administrator (whose saved session the editor screens use) is never signed out; sign-out rotates only the second administrator's stamp, and the lock stops the three engines ending each other's session. Wrong passwords: at most one per engine, then a correct sign-in resets the count, so lockout (5) is not approached. The lock is a temp directory outside the repo and the stack; it cannot change stack state. A failed test leaves at most a live session on the second administrator, which is harmless. |
| README and backlog match | Yes. Test counts in `worker-O1.md` (790 = 773 + 17) match the new tests (1 xUnit, 5 Karma, 2 runtime, 3 browser x 3 engines). The backlog paragraph and the README rows describe what the tests do; status correctly stays In Progress. |
| Credentials | None are printed or hard-coded for real accounts. Administrator credentials come from `.env.verify` (gitignored), are compared as booleans, and the failure dump prints only lengths and class names. Throwaway values (`Wrong-password-123`, `verify-role-probe@example.test`, `Verify-role-probe-pass-1`) belong to no account and follow the existing `register_body` pattern. |

## Low-severity notes (no order needed)

1. Credential echo on failure only: in the first browser test, `expect(visibleToScript).not.toContain('goosewebsite.auth')` would print the whole `document.cookie` string if it ever failed, which would contain a session cookie value of the seeded test administrator. The names assertion above it fails first, so it is practically unreachable. Suggest dropping that line or asserting on `names` only.
2. Lock robustness: the stale-lock timeout is 60 s from lock creation. If a slow engine holds it longer than 60 s, another engine takes over and the sign-out collision returns. Also two waiters that both judge a crashed lock stale can both proceed. Only matters after a crashed worker or an unusually slow run; both runs passed twice. The sign-out test also depends on the operator reset of the second administrator's password having run earlier in `verify.ps1`; running `auth.spec.ts` alone on a fresh stack fails by design (the error names the missing variables only, not that dependency). A comment already explains the password source.
3. The 404 plus "cannot sign in" runtime check for `role`/`isAdmin` is weak on its own (a 401 would also follow if the endpoint ignored the body), but it is honest about it and the real role-ignored behaviour is covered in xUnit with registration on. Acceptable.

## The "late password clear race"

Reproduced by reading `sign-in-page.ts`. `signIn()` sets `isBusy`, and the inputs are not disabled while busy (only the button is). The error handler later runs `this.password = ''`; the `ngModel` write of that empty value into the input happens on the next change detection. Two consequences:

- In the test, `fill()` right after the alert appears can be followed by the delayed clear, so the new text vanishes. The Worker's wait for the field to be empty is the right fix.
- For a user: only if they type a new password between pressing Sign in and the response arriving (a request that includes a deliberately slow password hash, so a few hundred ms to seconds on a slow link). That text is wiped and the button stays disabled until they retype. They would normally wait for the result before retyping, so this is a minor annoyance, not a security or data problem and not a defect worth blocking sign-off.

Optional hardening for the backlog (not required for F3-US1): disable the inputs while `isBusy()` or clear the password only if it still equals the submitted value. Either would remove the race rather than the test working around it.

## Conclusion

The four evidence gaps and the dummy-hash claim are closed with real, failure-capable tests. Nothing here is a code defect. F3-US1 may proceed to the next step (Security, then Captain sign-off).
