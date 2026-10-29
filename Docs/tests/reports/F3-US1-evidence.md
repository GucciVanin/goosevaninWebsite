# F3-US1 evidence (administrator authentication)

Date: 2026-10-28. Branch `r1-signoff`, HEAD `ceee6f7`, working tree clean. Role: Testing; no code edited. CI gate skipped (`-SkipCi`; CI is not part of this evidence). Mail: no contact message submitted, no email sent (reader registration is switched off in the stack, so runtime registration only sees 404/429).

## Runs

| Run | `./scripts/verify.ps1 -SkipCi` | Exit |
| --- | --- | --- |
| 1 | 773 passed, 0 failed, 0 skipped, 0 known | 0 |
| 2 | 773 passed, 0 failed, 0 skipped, 0 known | 0 |
| 3 | 773 passed, 0 failed, 0 skipped, 0 known | 0 |

Determinism: `latest.log` of the three runs, timings stripped and sorted, are byte-identical (same tests, same outcomes). No flaky test. The Angular results (Karma) come from the verify container. Separate host run `dotnet test --filter "AuthApiIntegrationTests|EditorAccessTests|SignOutSessionTests|SessionMarkerCookieTests"`: 39 passed, 0 failed.

"3/3" below means the test passed in all three verify runs.

## AC to tests to verdict

Backlog text: Docs/product-backlog.md, F3-US1, acceptance tests 1 to 5. xUnit files are in `tests/GooseWebsite.Api.Tests/`; runtime in `verify/runtime/`; Karma in `src/GooseWebsite.Client/src/app/`; Playwright in `verify/browser/tests/`.

| AC | Automated tests (name, file) | Result | Verdict |
| --- | --- | --- | --- |
| AC1 valid sign-in gives a session not exposed to script | xUnit `AuthApiIntegrationTests.AdminLoginRequiresCsrfAndSetsSecureHttpOnlySessionCookie` (valid login 200; `goosewebsite.auth` has httponly, secure, samesite=lax, no expires; `/me` says admin, no email); `SessionMarkerCookieTests.SignIn_KeepsTheSessionCookieHttpOnly`, `SignIn_SetsAMarkerCookieWithTheAgreedFlagsAndNoIdentity`, `TheMarkerAloneGrantsNothing` (the one non-HttpOnly cookie is a constant and grants nothing). Runtime `60-auth.sh`: "the seeded administrator can sign in", "the session cookie is Secure / HttpOnly / SameSite=Lax / a session cookie (no fixed expiry)", the sign-in marker checks, "the signed-in profile says administrator", "never includes the email", "sign-in without a CSRF token is rejected". | 3/3 pass | **Proven** at the protocol level on the real stack (Set-Cookie flags). No browser-level check that `document.cookie` lacks the session cookie exists (GAP-1); HttpOnly in the header is the standard guarantee, so this is a cheap confirmation, not a hole. |
| AC2 invalid credentials or unauthenticated request rejected, no privileged action | xUnit `AuthApiIntegrationTests.InvalidAndUnknownCredentialsHaveSamePublicResponse` (unknown, wrong, locked: 401, no auth cookie issued); `EditorAccessTests.Anonymous_CannotWrite_AndTheStoredContentIsUnchanged` (5 verbs, 401, stored content re-read and unchanged); `AReader_CannotWrite_AndTheStoredContentIsUnchanged` (5 verbs, 403); `AnAdministratorWithoutTheCsrfToken_CannotWrite` and `..._CannotChangeWhatIsPublic`; `AuthApiIntegrationTests.AdminMutationsRequireCsrfAndLogoutInvalidatesSession` (anonymous write 401); `ImageUploadApiTests.AnonymousVisitorsAndAdministratorsWithoutTheCsrfTokenCannotUpload`; `DataSafeguardsTests.Write_IsRefusedForEveryoneWhoIsNotAnAdministrator_AndNothingChanges`. Runtime: `60-auth.sh` "sign-in with an unknown email / a wrong password is refused" (401), "the protected profile endpoint refuses an anonymous caller"; `70-blog.sh`, `75-projects.sh`, `78-images.sh` anonymous create/update/delete/publish/list/upload all 401, administrator without CSRF refused. | 3/3 pass | **Proven.** The "no privileged action" half is asserted by re-reading stored data in xUnit; runtime checks assert the status code only. |
| AC3 after sign-out or expiry the same session is rejected | xUnit `SignOutSessionTests.SignOut_EndsTheSessionEvenForACookieCapturedBeforehand` (cookie captured before sign-out replayed after it: 401); `SignOut_DoesNotStopTheAccountFromSigningInAgain`; `AuthApiIntegrationTests.AdminMutationsRequireCsrfAndLogoutInvalidatesSession`; `ExpiredAdminSessionCannotAccessProtectedEndpoints` (150 ms cookie; `/me` and a write 401); `EditorAccessTests.AnExpiredSession_CannotWrite_AndTheStoredContentIsUnchanged` (5 verbs, content unchanged); `SessionMarkerCookieTests.SignOut_ClearsTheMarkerWithTheSameScopeSoTheBrowserMatchesIt`. Runtime `60-auth.sh`: "sign-out answers 204", "the profile endpoint refuses the signed-out client", "a signed-out session cookie replayed later is rejected". Karma: `session.interceptor.spec` "sends the person to sign in, remembering where they were, when a write finds the session gone"; `auth-api.spec` "clears the marker after signing out", "clears the marker when the API answers 401". | 3/3 pass | **Proven** for sign-out (replay of the captured cookie, on the real stack and in xUnit) and for expiry in xUnit (short lifetime). Expiry is not exercised on the running stack (GAP-4, low value: the lifetime is configuration). |
| AC4 registration cannot create or grant an administrator | xUnit `AuthApiIntegrationTests.PublicRegistrationIgnoresRequestedAdminRoleAndCreatesReaderAccount` (body carries `role: "Admin"`; account is Reader, not Admin, unconfirmed); `ReaderRegistrationIsDisabledUnlessExplicitlyEnabled`; `ReaderRegistrationRequiresEmailConfirmationBeforeSignIn`; `ReaderAccountServiceTests.Register_*`; `AdminProvisioningTests.*` (operator-only path, existing users never promoted); `DataSafeguardsTests.Identity_AnonymousVisitorsCanStillRegisterAndSignIn`. Runtime `80-reader.sh`: "registration is refused with 404 while it is switched off", "registration requests are rate limited after five tries". | 3/3 pass | **Proven** in xUnit (the exact scenario: role value in the body). **Partly** on the running stack: registration is off there, so runtime only proves the endpoint is closed, not role-ignored with registration on (GAP-2). |
| AC5 sign-in form and sign-out clearly available; non-enumerating validation and session-expiry feedback | Same response: xUnit `InvalidAndUnknownCredentialsHaveSamePublicResponse`, `RegistrationAndVerificationResendResponsesDoNotRevealAccountExistence`, `ReaderAccountServiceTests.Register_AnswersTheSameWhetherOrNotTheAddressIsAlreadyRegistered`; runtime `60-auth.sh` "a wrong password and an unknown email get the same answer" (body equality). Karma: `sign-in-page.spec` "explains an expired session and keeps that message over the generic one", "makes no request to /api/auth/me without the marker and says the visitor is not signed in" (form present), "asks the API with the marker and shows who is signed in"; `site-header.spec` "shows a shared sign-in link on the right side that targets the sign-in route"; `app.spec` "keeps the sign-in screen without the site header and footer"; `session.interceptor.spec` "does not treat a wrong password as an expired session". Runtime `30-pages.sh` "route /sign-in serves the app". Playwright (chromium, firefox, webkit), sign-in screen: loads with no failed request or console error, no critical/serious WCAG A/AA violation, keyboard reachable with visible focus, fits 320 px. | 3/3 pass | **Partly.** The non-enumerating server response and the expiry notice are proven. Not tested: the UI text after a failed sign-in, the Sign out button shown when signed in, and a real end-to-end sign-in/sign-out through the page (GAP-3). Labels are only checked indirectly (axe). |

## Gaps (proposals only, nothing written)

- **GAP-1 (AC1, low).** No browser check that the session cookie is invisible to script. Proposal: new `verify/browser/tests/auth.spec.ts`: sign in as the seeded test administrator through the sign-in page; `page.evaluate(() => document.cookie)` must not contain `goosewebsite.auth` (only the `goosewebsite.signedin=1` marker), while `context.cookies()` lists `goosewebsite.auth` with `httpOnly: true`. Seeded verify administrator only; no email.
- **GAP-2 (AC4, medium).** Role-ignored behaviour is proven only in xUnit; on the running stack registration is off. Proposal: in `80-reader.sh`, with registration off, post a body with `"role":"Admin"` and `"isAdmin":true` and assert 404, then assert `/api/auth/login` for that email stays 401. A registration-on runtime test would send a verification email, so it must stay in xUnit; record that decision.
- **GAP-3 (AC5, medium).** Karma never asserts (a) the failed-sign-in message `Unable to sign in with those credentials.` (same for unknown and wrong password), (b) that the `Sign out` button shows when `user()` is set, calls `signOut()` and then shows `You have signed out.`, (c) the `Email` and `Password` labels tied to their inputs. Proposal: three Karma cases in `sign-in-page.spec.ts`, plus one Playwright flow in `auth.spec.ts`: wrong password and unknown email show identical visible text; sign in then Sign out returns to the signed-out state and `/editor` redirects to `/sign-in`.
- **GAP-4 (AC3, low).** Expiry is tested only in xUnit with a 150 to 400 ms lifetime. Proposal: Playwright test that clears the `goosewebsite.auth` cookie but keeps the marker, performs an editor write, and expects the redirect to `/sign-in?reason=session-expired` with the text `Your session expired. Sign in again to continue.`
- **Note (AC2).** Runtime tests assert status codes only; "no privileged action" is proven by the xUnit re-read of stored data. A runtime variant: after the anonymous attempts in `70-blog.sh`, assert the public story list is unchanged.
- **Note.** The timing-equalisation claim (dummy hash for unknown users, backlog implementation evidence) has no test; only response equality is tested.

## Summary

AC1 proven, AC2 proven, AC3 proven, AC4 proven in xUnit and partly at runtime, AC5 partly. No failure was found. The four gaps are test-only orders; whether GAP-3 must close before sign-off is Gustavo's call.

## Cleanup

After the runs `goosewebsite-caddy` and `goosewebsite-web` were stopped by name only. No other container or process was touched.

## Final run after orders O1 and O2 (2026-10-28, branch r1-signoff, uncommitted test additions)

Commands as fixed in `Docs/tests/README.md`; the client was built once first. Each layer ran three times.

| Layer | Run 1 | Run 2 | Run 3 | Per-test list across runs |
| --- | --- | --- | --- | --- |
| `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | 189 passed, 0 failed | 189 passed, 0 failed | 189 passed, 0 failed | Identical (names and outcomes compared from the three .trx files) |
| Karma (ChromeHeadlessNoSandbox) | 195 of 195 success | 195 of 195 success | 195 of 195 success | Totals identical; the Karma progress reporter does not list test names, so names were compared through the `unit-angular` lines of `verify-results/latest.log` below |
| `./scripts/verify.ps1 -SkipCi` (reset, rebuild, .NET, Angular, runtime, browser x3 engines) | 790 passed, 0 failed, 0 skipped, 0 known; exit 0 | 790 / 0 / 0 / 0; exit 0 | 790 / 0 / 0 / 0; exit 0 | 822 log lines per run; per-test lines identical after removing timings |

Determinism: no test changed outcome between runs, so none is `nondeterministic`. CI was skipped (`-SkipCi`) as for the earlier reports; the branch pull request check is not part of this run.

Secrets check: the values of `.env.verify` (5 values of 6 characters or more) were searched for, without printing them, in `verify-results/` (excluding `backup-*` directories, which hold copies of the test database) and in `Docs/`: no match. No `goosewebsite.auth=` or `.AspNetCore.*=` cookie value pattern in `verify-results/` or `Docs/tests/reports/`. The backup databases contain the seeded test accounts by design and are not logs.

New tests seen passing in all three runs: `SignInTimingEqualisationTests.UnknownEmailWrongPasswordAndLockedAccountEachRunTheSameNumberOfHashVerifications`; Karma `sign-in-page.spec` (labels tied to inputs, Sign out button when signed in and its effect, none when signed out); runtime `80-reader.sh` ("a registration body carrying role and isAdmin is answered as before", "no account that can sign in results from a registration body carrying role and isAdmin"); Playwright `auth.spec.ts` on chromium, firefox and webkit (cookie invisible to script and HttpOnly; same message for wrong password and unknown email, sign-in, Sign out; cleared session cookie with marker kept redirects with `reason=session-expired`).

### Verdict per acceptance criterion after the new tests

| AC | Verdict | Basis |
| --- | --- | --- |
| AC1 session not exposed to script | **Proven** | Protocol-level flags (xUnit, runtime) plus the browser check that `document.cookie` holds only the marker while the browser holds `goosewebsite.auth` as HttpOnly, on three engines (closes GAP-1). |
| AC2 invalid or unauthenticated rejected, no privileged action | **Proven** | Unchanged: xUnit re-reads stored data; runtime asserts status codes; the browser flow shows the identical failure message. |
| AC3 sign-out or expiry rejects the session | **Proven** | Sign-out replay (xUnit, runtime) and expiry in xUnit; now also an expired-session look in the browser (cookie gone, marker kept: redirect with the expiry notice). Natural expiry on the running stack is still not exercised (GAP-4 stays low value: the lifetime is configuration). |
| AC4 registration cannot create or grant an administrator | **Proven in xUnit, partly on the running stack** | Role ignored with registration on: xUnit only. On the stack registration is off, so the runtime test proves the endpoint stays closed to a body carrying `role` and `isAdmin` and that no account able to sign in results. A registration-on runtime test would send a verification email, so it stays out of the runtime layer by decision. |
| AC5 sign-in form and sign-out available, non-enumerating validation, expiry feedback | **Proven** | Same response for unknown and wrong password (xUnit, runtime body equality, Karma message, browser visible text); labels tied to inputs (Karma); Sign out button shown and effective when signed in, absent when signed out (Karma); real end-to-end sign-in and Sign out through the page on three engines; expiry notice (Karma, browser). Closes GAP-3. |

Timing-equalisation claim (dummy hash for unknown email and locked account): now tested by counting hash verifications in xUnit; this is a count of work done, not a wall-clock measurement.

Summary: no failure, no flaky test. AC1, AC2, AC3 and AC5 proven; AC4 proven in xUnit and partly at runtime for the stated reason. Real Edge and Safari remain the manual pass (`Docs/tests/browser-manual-check.md`).

Cleanup: `goosewebsite-caddy` and `goosewebsite-web` stopped by name at the end of this run; nothing else was touched.
