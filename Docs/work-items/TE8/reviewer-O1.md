# Reviewer report - TE8 O1 (one Mail module)

## Verdict: ACCURATE, with two small documentation defects and no code defects

worker-O1.md is accurate on every claim I could check. `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 188 passed, 0 failed (I ran it; no containers touched, `verify.ps1` not re-run, so its 773 figure is taken from the Worker). No git write, no real mail.

## Checks

| Item | Result |
| --- | --- |
| AC1 one interface, no module touches SMTP/MailerSend | Pass. Grep of `src` outside `Shared/Email` finds no `System.Net.Mail`, `SmtpClient`, `SmtpOptions`, transports or `mailersend.com`; only comments/docstrings in Contact name MailerSend. `MailServiceTests.NoModuleTalksToSmtpOrMailerSendDirectly` pins it. Modules depend on `Shared` only (rule 3.1 holds); Shared does not reference modules. |
| AC2 failure model, never throws | Pass. `MailService` wraps both transport calls in `catch (Exception)` and returns `MailResult`; the transports also return categories. Note: this also swallows `OperationCanceledException` (turns caller cancellation into `TransportError`); acceptable, see advisory 4. |
| AC2 logging rule | Pass. Log lines carry kind, transport, failure, HTTP status, SMTP status, exception type names, and the template id (an identifier, not a secret). Exception messages are never logged (the SMTP transport comment notes a malformed-address message is the address). Transports have no logger. Log-capture tests cover success, provider rejection (address and body in the provider response), not-configured (both kinds), SMTP connection failure, a throwing transport whose message embeds address and link; all assert none of address, token, link, body, key, name appear. |
| AC3 behaviour unchanged | Pass. Compared with `HEAD`: MailerSend payload code is byte-for-byte the same (from, to, reply_to omitted when null, subject, template_id, personalization, Bearer header, X-Requested-With); SMTP settings (SSL, creds, 30 s timeout, plain-then-HTML alternate views) same; subject/HTML/plain text come from the unchanged `AccountVerificationEmailRenderer`; Contact delivery logic (owner notice skipped on retry, reply-to wiring, subjects) same, only `.Succeeded` replaces the bool. Test assertions are unchanged except double swaps. The old "SMTP not configured -> false, nothing sent" behaviour is kept (`WhenMailIsNotConfigured...`). Deliberate small differences: cancellation token now passed to `SendMailAsync`; malformed address now returns failure instead of throwing (Worker doubt 3, an improvement); log wording changed. |
| Config fallback | Pass. `AddEmail` binds legacy `MailerSend` first, then `Email:MailerSend` (new wins); a test covers both. `appsettings.json` no longer ships empty `ApiKey`/`FromEmail` defaults, which is what stops the legacy values being shadowed. Existing user-secrets under `MailerSend:*` keep working in development. |
| docker-compose / .env | Pass for the supported path. `.env` variable names are unchanged (`MAILERSEND_API_KEY`, `MAILERSEND_FROM_EMAIL`, `SMTP_*`); compose maps them to `Email__MailerSend__*` with the same defaulting (`FROM_EMAIL` falls back to `SMTP_FROM_EMAIL`), so an old `.env` works end to end. The empty-override issue the Worker flagged is real but narrow: compose now always sets `Email__MailerSend__ApiKey` (empty when `.env` lacks it), and a configuration binder will take that empty string over a legacy `MailerSend__ApiKey` set on the container some other way (for example a hand-written override file or `docker run -e`). Nobody in the repo does that, so it is not a defect, but it is why the fallback is not fully transparent (advisory 2). |
| Removed failure-log test | Covered. `ContactEmailTemplateTests.MailerSendClient_OnFailureReturnsFalseAndLogsNoSecretsOrAddresses` asserted a 422 is logged with no key/address/body; `MailServiceTests.TemplateSend_WhenProviderRejects_ReturnsFailureAndLogsOnlyTheStatus` asserts the same and more, through the real transport plus service. |
| AC4 one double | Mostly. `TestMailService` replaces `TestAccountEmailSender`, `RecordingMailerSendClient` and `CapturingSmtpMailSender`. `StubContactEmailDeliveryService` remains but doubles the Contact-level service, not a mail seam; I judge AC4 met (advisory 3). |
| AC5 docs | Mostly. Architecture diagram, module table, delivery/verification rows, sequence participant, section 7 settings, troubleshooting, conventions, `CLAUDE.md` stack and secrets lines, `.env.example` all match the code. TE8 is correctly "implemented, not Done". Two stale spots below. |

## Defects (documentation only)

1. `Docs/architecture.md` line 608 (section 10, "Test doubles"): still says `TestAccountEmailSender` captures verification links; that file is deleted. Should name `TestMailService` (and its `AccountMessages`). Section 10 line 602 says "a test mail sink", which is fine.
2. `Docs/email-templates/README.md` line 31 still says the account email "is sent over SMTP ... by `SmtpAccountEmailSender`"; it is now `AccountEmailSender`. (Backlog F9-US6 evidence and old work-item files also name the old class; those are dated history and should stay.)

## Advisories, ranked

1. Fix the two doc defects above before the Captain closes (CLAUDE.md rule 8).
2. Document in `.env.example` or architecture section 7 that, in Docker, only the `.env` variable names are supported and a raw `MailerSend__*` container variable is shadowed by the empty `Email__MailerSend__*` values compose always sets. Alternatively make compose omit the key when unset; not worth the churn.
3. `StubContactEmailDeliveryService` (about 25 call sites) stays; record in the decision log that AC4 was read as "mail seam doubles", so the choice is explicit.
4. `MailService` catches `OperationCanceledException` as a `TransportError`; an aborted request now logs an error-level "delivery failed". Consider treating caller cancellation separately. Cosmetic: a successful SMTP send logs `...transport=SMTP; .` (empty diagnostics leaves a stray `; .`).
5. The two later tests (no-module guard, config binding) were never seen red (Worker disclosed it). Both are plausible pins; no action needed.
6. Not verified by me: `verify.ps1 -SkipCi` (773 tests) and any real SMTP/MailerSend delivery; the SMTP provider path remains the known gap in CLAUDE.md.
