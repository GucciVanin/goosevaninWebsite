# TE8 test run (mail service, transports, templates)

Date: 2026-10-28. Branch `r1-signoff`, HEAD `2d1c62d` plus the uncommitted TE8 work (`Docs/work-items/TE8`). Docker 29.8.2. CI gate skipped (`-SkipCi`, branch not yet committed). Role: Testing; no code edited.

## Totals per run

| Run | .NET (`dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`, client built once first) | `./scripts/verify.ps1 -SkipCi` (exit code) |
| --- | --- | --- |
| 1 | 188 passed, 0 failed, 0 skipped | 773 passed, 0 failed, 0 skipped, 0 known (exit 0) |
| 2 | 188 passed, 0 failed, 0 skipped | 773 passed, 0 failed, 0 skipped, 0 known (exit 0) |
| 3 | 188 passed, 0 failed, 0 skipped | 773 passed, 0 failed, 0 skipped, 0 known (exit 0) |

Verify stage split (identical in all three runs): unit-dotnet 188, unit-angular 195, runtime 148, browser 242 (Chromium, Firefox, WebKit). The verify container re-ran the Angular suite, so the 195 Angular results come from it. The separate Karma run required by the test list was skipped on request (client untouched by TE8).

## Determinism

- .NET: the per-test lists (name and outcome) from the three trx files are identical (188 each, all Passed).
- Verify: `latest.log` of the three runs, with timings stripped and sorted, are byte-identical (805 lines each: 773 PASS and 32 stage headers and summary). No test differs, so none is `nondeterministic`.

## Log check (mail flows)

- Web container logs (`docker logs goosewebsite-web`, one capture per run, the stack is rebuilt each run): zero real email addresses (regex `x@y.z`), zero verification links or tokens (`/verify`, `/confirm`, `token=`, `code=`), zero message bodies, zero lines mentioning MailerSend, SMTP or MailService. The `@` occurrences in the logs are EF Core SQL parameter names (`@p0`, `@normalizedEmail`); parameter values are logged as `?`.
- Only warning-level noise seen: one HttpsRedirection middleware warning.
- Nothing in the mail flows logged an address, link, token or body.
- No email was sent: no valid contact message was submitted (40-contact only exercises rejections: invalid address, unknown or empty token, GET never delivers). The only outbound mail-provider call is runtime test `45-mail`, a read-only `GET /v1/api-quota` that checks the API key (it passed in all three runs); it sends nothing. The xUnit mail tests use fake transports/handlers (`TestMailService`, `MailServiceTests`, `ContactEmailTemplateTests.MailerSendTransport_*` against a stub handler), and they assert that no address, link, body or key is logged.

## Mail-related tests that passed (all runs)

MailServiceTests (template and rendered send, not configured, provider rejects, transport throws, no secrets or addresses logged, `NoModuleTalksToSmtpOrMailerSendDirectly`), ContactEmailTemplateTests, AccountVerificationEmailTests, ContactSubmissionModuleTests, the 45-mail runtime test.

## Not covered

- Real SMTP delivery and real MailerSend delivery (templates rendered by the provider, deliverability, the logo and link in a received message). Only the API key authorization (read-only) is checked.
- Real Edge and Safari (manual checklist `Docs/tests/browser-manual-check.md`, not done in this run).
- Separate Karma run (skipped; Angular results come from the verify container).
- CI (GitHub Actions) not run for these uncommitted changes.

## Cleanup

After the runs `goosewebsite-caddy` and `goosewebsite-web` were stopped by name only; no other container or process was touched.
