# Decisions - F3-US1 close-out (administrator authentication)

Intent: F3-US1 has been "In Progress" since 2026-09-30 with every implementation task checked. Close it by running its acceptance tests against the real stack and recording the evidence (CLAUDE.md rule 7), not by reading code.

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-26 | No new code is planned. If a check fails, the failure becomes a new order. The acceptance criteria are the five acceptance tests of F3-US1 in `Docs/product-backlog.md` (numbered F3-US1-AC1 to AC5), already testable. | Pending Gustavo's approval of the plan below. |

Evidence plan (what the Testing role maps and runs; each AC must name its automated tests, and any AC with no automated test is stated plainly):
- **F3-US1-AC1** valid sign-in gives an HttpOnly session not readable by script: API integration tests and the runtime cookie-flag checks (`verify/runtime/60-auth.sh`, `20-https.sh`) plus a browser check that `document.cookie` does not contain the session cookie after sign-in.
- **F3-US1-AC2** invalid credentials or an unauthenticated request to a protected operation is rejected with no privileged action: `EditorAccessTests`, `AuthApiIntegrationTests`, the runtime anonymous-401 checks.
- **F3-US1-AC3** after sign-out or expiry the old session is rejected: `SignOutSessionTests`, `AnExpiredSession_CannotWrite...`, the runtime sign-out check.
- **F3-US1-AC4** public registration cannot create an administrator: the registration API tests and a runtime check that a registration body carrying a role is ignored.
- **F3-US1-AC5** sign-in form and sign-out action clearly available with non-enumerating feedback: Karma sign-in specs, the browser stage on the sign-in screen, and a runtime check that unknown email and wrong password return the same response.
Gaps found by the mapping (for example a browser check for the HttpOnly claim or the same-response check) become orders, test first.

| 2026-10-26 | Gustavo approved the evidence plan as drafted; also approved TE8 approach B (see TE8). | Runs after TE4 and TE8. |
