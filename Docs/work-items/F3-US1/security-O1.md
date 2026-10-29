# Security review - F3-US1 order O1 (test additions)

Reviewer: Security. Read-only review of the uncommitted changes in `worker-O1.md`. No code edited, no git write, no container touched.

Overall: **PASS**, with two Low findings and one Informational finding. No finding blocks commit.

## Scope read

`verify/browser/tests/auth.spec.ts`, `verify/browser/helpers/lock.ts`, `global-setup.ts`, `jsonl-reporter.ts`, `playwright.config.ts`, `verify/stages/40-browser.sh`, `verify/runtime/80-reader.sh`, `.env.verify.example`, `.gitignore`, `docker-compose.yml` (verify service), `tests/GooseWebsite.Api.Tests/AuthApiFactory.cs`, `SignInTimingEqualisationTests.cs` (existence), `sign-in-page.ts` and its spec diff, `Docs/tests/reports/F3-US1-evidence.md`, `worker-O1.md`.

## Findings

| # | Severity | Area | Result |
| --- | --- | --- | --- |
| S1 | Low | Credentials could reach logs on a failing test | Pass with note |
| S2 | Low | Cross-process lock directory in the OS temp folder | Pass with note |
| S3 | Info | Late password clear race | No security effect |
| S4 | Pass | Credential sourcing and committed artifacts | Pass |
| S5 | Pass | Playwright traces, screenshots, video, reports | Pass |
| S6 | Pass | `AuthApiFactory.configureServices` hook | Pass |
| S7 | Pass | Test-only code reachable from the production build | Pass |
| S8 | Pass | Second administrator usage and state left behind | Pass |

### S4 - Credentials: Pass
- `auth.spec.ts` reads `VERIFY_ADMIN_EMAIL`, `VERIFY_ADMIN_PASSWORD`, `VERIFY_ADMIN2_EMAIL`, `VERIFY_ADMIN2_NEW_PASSWORD` from `process.env` only. The container gets them through `env_file: .env.verify` (docker-compose.yml), and `.env.verify` is gitignored (`git check-ignore` confirms; it is not tracked). `.env.verify.example` holds documented test-only values with a "do not reuse a real password" warning.
- The only literal credentials in the new files are `Wrong-password-123`, `Verify-role-probe-pass-1` (the role probe in `80-reader.sh`), which are throwaway values for accounts that do not exist.
- The spec compares field contents as booleans (`=== email`, `=== password`) so a failed assertion prints `true/false`, not the value. The "Sign in stayed disabled" diagnostic prints class names and lengths only.
- I searched the three `.env.verify` password values across `verify-results/`, `Docs/`, `verify/runtime`, `verify/stages`, `verify/browser/tests`, `verify/browser/helpers`, `tests/`, `scripts/`: zero hits. The F3-US1 evidence report and `worker-O1.md` contain no credential values. `verify-results/` is gitignored and untracked.
- `80-reader.sh` builds the JSON with `jq --arg`, so the password is not interpolated into a shell string and is not echoed; `check` prints only status codes.

### S5 - Playwright artifacts: Pass
`playwright.config.ts` sets `trace: 'off'`, `screenshot: 'off'`, `video: 'off'`, `retries: 0`; reporter is the custom JSONL reporter plus `line` (no HTML report, no blob report). So there are no traces (which would hold request bodies and cookies), no screenshots of the typed form, no videos. `outputDir` is `/tmp/verify-browser-output` inside the verification container, not the repo. Cookie values are never read by the new spec: it reads cookie names, `httpOnly`, `secure` only (`context.cookies()` objects are not printed). The saved admin storage state (`adminStatePath`) holds a session cookie but is written by the existing `global-setup.ts` outside the new changes and is test-stack-only.

### S1 - Low: possible echo of a typed password on a failing assertion
`await expect(page.getByLabel('Password')).toHaveValue('')` is used after each failed sign-in. If the clear never happens (the exact race below, or a regression where the page stops clearing the password), Playwright's failure message prints `Expected: ""` and `Received: "<typed value>"`. In this test the typed value at that point is the throwaway `Wrong-password-123`, so nothing real leaks, but the pattern is wrong to copy for the real password: the same assertion after typing the real second-admin password would put it into `browser.jsonl` (first 600 chars of the error) and then `latest.log` / `console.log` in `verify-results/` (gitignored, local). Recommendation (optional, not blocking): assert `(await field.inputValue()) === ''` as a boolean, as `attemptSignIn` already does. Same caution applies to the `toHaveText` on the alert, which holds no secret.

### S2 - Low: cross-process lock
`lock.ts` uses an empty directory `goosewebsite-verify-locks/second-administrator` under `os.tmpdir()` inside the verification container. It contains no data, no secrets, no credentials or cookies; the name is a fixed literal supplied by the test, not user input, so there is no path traversal. It is removed in `finally`. A crashed worker leaves an empty directory that is taken over after 60 s (stale check), and the container filesystem is discarded with the container. Residual: the stale check and `rmSync` are not atomic (two waiters could both take over a stale lock), and a test body longer than 60 s would lose the lock to another waiter. That is a test-reliability matter (could briefly let two engines sign the same account out), not a security issue. In a shared host `/tmp` another local user could pre-create the directory to stall the run (denial of the test only); not applicable to a container.

### S3 - Late password clear race: no security effect
`signIn()` in `sign-in-page.ts` does `this.password = ''` in the error callback (and on success). Because `ngModel` is two-way bound, the clear flows to the input asynchronously (the `NgModel` writes the model value on a later change-detection tick), so a `fill` that lands between the callback and the view update can be wiped. Effects:
- Confidentiality: the clearing is in the secure direction. The race can only lose typed text, never retain a password longer than intended. The password never leaves the in-memory field; it is not put into the URL, storage, or the message signal.
- Integrity/auth: the server decides authentication; the race cannot cause a different or privileged request, a submission with someone else's credentials, or a bypass of the generic failure text. The alert text is a constant, so there is no user-enumeration effect.
- Availability: only the button staying disabled (`!signInForm.valid`) for one attempt; no lockout effect, and a human cannot type that fast. The Worker's reading (harmless UI race) is correct. One mild note: after a failed attempt `this.password = ''` clears the password but the email stays, which is fine and matches the generic-failure design.
Reader note (not a finding): `register()` and `signIn()` share the `password` field; a failed registration also clears it, which is consistent.

### S6 - `AuthApiFactory` hook: Pass
`AuthApiFactory` is in `tests/GooseWebsite.Api.Tests`, an `internal sealed` test-project class deriving `WebApplicationFactory<Program>`. The new `Action<IServiceCollection>? configureServices` is applied inside `ConfigureWebHost` of that factory only, after the SQLite swap, with a null default. Nothing in `src/` references the factory (searched). It cannot be invoked by a deployed host. The test that uses it (`SignInTimingEqualisationTests`) swaps `IPasswordHasher` for a counting wrapper in the in-memory test host only; it does not alter any production registration or the dummy-hash code (`git diff` of `src/GooseWebsite.Api` is empty).

### S7 - Test-only reachability from production: Pass
Changed files under `src/` are only `sign-in-page.ts` (the Karma-spec-related change set shows `sign-in-page.spec.ts`; `sign-in-page.ts` is listed as modified in git status, and the diff of the page I read matches the committed behaviour: Sign out button, generic message). The `/api/auth/register` role probe and the other new checks live in `verify/` (a separate verification container image), `Docs/`, and `tests/`. No new endpoint, no config switch, no environment-name branch, no test credentials or backdoor was added to the production build. The verification stack's seeded administrators are created by the operator command `admin provision` in a data volume reset on every run, as documented.

### S8 - Second administrator and state left behind: Pass
`VERIFY_ADMIN2_*` is a test-only account created by the verify run in the testing stack; its password is the post-reset value from `.env.verify`. The sign-out test performs at most one wrong password per engine for it (three attempts total plus the unknown-email one), followed by a correct sign-in that resets the lockout count, so the limit of five is never approached and the account is not left locked. Sign-out rotates the security stamp, which is the feature under test and is why the first administrator is not used. At the end the tests leave only a signed-out state; the data volume is reset per run (`verify-results/backup-*` are local data backups, gitignored). No account created by the role probe exists (the test asserts 401 for it). The 404 registration probe creates nothing.

## Other observations
- The new Karma and runtime assertions strengthen the non-enumeration property (same generic message for unknown and wrong password, server error text ignored) without weakening any control.
- The `SignInTimingEqualisationTests` claim (equal hash-verification count across unknown, wrong, locked paths) is by count, not time; the Worker stated this correctly. Count equality is a reasonable but not complete defence against timing differences (database lookup time on a real user still differs); the backlog wording "to reduce" is accurate.

## Verdict
PASS. Recommended (non-blocking): change the two `toHaveValue('')` password assertions in `verify/browser/tests/auth.spec.ts` to a boolean comparison so a failure can never print a typed password (S1).
