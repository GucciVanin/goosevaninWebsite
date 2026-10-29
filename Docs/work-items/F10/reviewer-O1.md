# Reviewer report - O1 (F10-US1)

**Verdict: accurate.**

I ran `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` myself: 222 passed, 0 failed, matching the Worker's report. I did not run Karma, verify.ps1 or Docker.

## Acceptance tests
| AC | Test | Would it fail if the behavior broke? |
| --- | --- | --- |
| 1 | `Subscribe_StoresAPendingSubscriberAndSendsOneConfirmationEmail` | Yes: one rendered mail and one pending record. |
| 2 | `EveryKindOfSubmissionIsAnsweredIdentically` (status and body of pending, confirmed, invalid, empty and long name, honeypot) and the mail-failure test | Yes. |
| 3 | Confirm-once and reuse, expired link (7 days + 1 minute), malformed tokens, GET never confirms | Yes. Client spec covers the button and the invalid-link wording. |
| 4 | `CleanupDeletesPendingRecordsWhoseLinkExpired...` through `RemoveExpiredPendingAsync`, plus a hosted-service registration check | Yes for the logic. The timer itself never ran (disclosed). |
| 5 | Honeypot stores and sends nothing; per-IP limit gives 429 with no extra record or mail; CSRF refused | Yes. |
| 6 | `pragma_table_info` column list, hashes are 64 hex characters, raw token absent from every value | Yes. |
| 7 | Access rule limits (100 and 254), `DataRejectedException` beneath the service, whole-host log capture holds no name, email or token | Yes. |

## CLAUDE.md rules
- DatabaseService only: yes. The service uses `ReadForUpdate`, `Add`, `Remove` and `SaveChangesAsync`, and has no DbContext or Identity injection.
- Access rule: `SubscriberAccessRule` sits next to the entity.
- CSRF: `[ValidateAntiForgeryToken]` on both POSTs.
- Rate limit: separate per-IP policies for subscribe and confirm.
- Responses: identical. Only 429 differs, which AC5 allows.
- Logs: they carry only the outcome category.
- Stored data: no IP address and no user agent.
- Module registration: one line in `Program.cs`.

## Mismatches
None blocking.

## Advisories
1. **Medium: AC3 wording.** AC3 says the subscriber is confirmed "when it is opened". The page confirms only after a button press. The decisions log records this as accepted by the Captain, but the story text still says "opened". Reword AC3 to "opened and confirmed", or tell Gustavo at review.
2. **Low: `SubscriptionService.cs`, "Timing side channel".** New and pending addresses do a database write and a mail send, while confirmed and invalid ones do not, so response time can leak state. AC2 covers only status and message, so this is not a failure. Sending the mail in the background would fix it. This also narrows what the 2-minute cooldown protects.
3. **Low: read access.** `SubscriberAccessRule` has no `VisibleWhen`, so the gateway lets every caller read rows. `NoEndpointListsSubscribers` and the runtime check mitigate this. The Worker disclosed it and it is reasonable to accept.
4. **Low: link format.** The link uses the fragment `#token=` while the decisions file's first entry says `?token=`. The later accepted-deviations entry and `Docs/architecture.md` agree with the code, so there is no mismatch. Keep the story text consistent.
5. **Low: `CreatedAtUtc`.** Cleanup is measured from link expiry, not creation. It is documented and is the same as creation + 7 days unless the person resubmits.
6. **Low: ASCII-only addresses.** Internationalised addresses are refused by the regex. Already flagged for Gustavo.
7. **Low: `TheCleanupJobIsRegisteredAsAHostedService`.** It checks registration only, and its trailing `await Task.CompletedTask` is noise.
8. **Low: O2 migration.** The O2 migration must drop the unused `UnsubscribeTokenHash` column. The Worker's harness (the code that runs the test) asserts the column exists, so that test must be updated in O2.
9. **Low: untested.** Real mail delivery, the email in real mail clients and the real cleanup timer remain untested, as disclosed.

Docs: `Docs/architecture.md` (section 3.6c, flow 5.4, frontend table, shell modes), the backlog evidence and `Docs/tests/README.md` match the code. The F10-US1 status is correctly "In Progress", not Done.
