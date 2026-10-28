# Worker report - TE8 O1 (one Mail module)

## What changed
Shared/Email (all in `src/GooseWebsite.Api/Shared/Email`):
- New: `MailModels.cs` (`MailAddressee`, `RenderedMail`, `TemplateMail`, `MailFailure`, `MailResult`), `IMailService.cs`, `MailService.cs` (the one place mail is logged, never throws), `IMailTransport.cs` (`IRenderedMailTransport`, `ITemplateMailTransport`, `MailTransportOutcome`), `SmtpMailTransport.cs`, `MailerSendTransport.cs` (transports never log).
- Rewritten: `EmailServiceCollectionExtensions.cs` (single `AddEmail`), `MailerSendOptions.cs` (section `Email:MailerSend`, old `MailerSend` read as fallback).
- Deleted: `ISmtpMailSender`, `SmtpMailSender`, `IMailerSendClient`, `MailerSendClient`, `Modules/Accounts/Services/SmtpAccountEmailSender.cs` (replaced by `AccountEmailSender.cs`, same content/subject/HTML/plain text).
- Modules: `AccountsModule.cs`, `ContactEmailDeliveryService.cs` now use `IMailService` only. `Program.cs` calls `AddEmail`.
- Config: `appsettings.json` (MailerSend moved under `Email`, empty ApiKey/FromEmail defaults removed so the legacy fallback is not shadowed), `docker-compose.yml` (`Email__MailerSend__*`; `.env` variable names unchanged), `.env.example` comment.
- Docs: `Docs/architecture.md` (diagram node, 3.x table, verification email and delivery rows, sequence participant, section 7 settings, troubleshooting, conventions), `CLAUDE.md` (tech stack line, secrets rule), `Docs/project.md` (stack row, shared row, TE8 row marked implemented, awaiting evidence, NOT Done), `Docs/work-items/TE8/decisions.md` (log line).
- Tests: new `MailServiceTests.cs` (9 tests), new `TestMailService.cs` (the one double); `AuthApiFactory` swaps `IMailService` for it; `TestAccountEmailSender.cs` deleted; `AccountVerificationEmailTests` and `ContactEmailTemplateTests` switched from per-module doubles to `TestMailService` / `MailerSendTransport` (assertions unchanged); sed-style rename `factory.AccountEmailSender.SentMessages` -> `factory.Mail.AccountMessages` (and `.ShouldFail`) in AuthApiIntegrationTests, ReaderAccountServiceTests, ContactVerificationApiTests.

## Red-first
`MailServiceTests` were run against a skeleton `MailService` (returned Success, logged nothing, real transports): 7 of 7 behaviour tests failed (wrong results / empty log) before the real `MailService` was written. Then all passed. The two later tests (no module references SMTP/MailerSend; legacy-vs-new config binding) were added after and passed on first run (they pin structure that already held), so they were not seen red.

## Totals
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 188 passed, 0 failed.
- `scripts/verify.ps1 -SkipCi`: 773 passed, 0 failed, 0 skipped, 0 known (log `verify-results/latest.log`). The script reset and rebuilt only the goosewebsite stack.
- Karma not run (client untouched). No real mail sent, no contact message submitted.

## AC mapping
AC1 interface + guard test; AC2 `MailService` + log-capture tests (success, failure, unconfigured, throwing, SMTP connection failure); AC3 existing Contact/F9-US6 assertions unchanged; AC4 `TestMailService`; AC5 architecture section 7, `.env.example`, CLAUDE.md. Not marked Done.

## Doubts
1. `StubContactEmailDeliveryService` still exists: it doubles the Contact-level `IContactEmailDeliveryService` (many flags used by ~25 call sites), not a mail seam. Replacing it with the real service over `TestMailService` would be a large test rewrite. Judge whether AC4 requires it.
2. `IAccountEmailSender` kept as the Accounts-level seam (renders the email); only its transport changed.
3. Small behaviour improvement: a malformed recipient address now returns failure from the SMTP transport instead of throwing out of the account sender.
4. A previously-set `MailerSend__*` env var is only a fallback; compose now sets `Email__MailerSend__*` (empty values there override the fallback), which is why the compose names changed.
5. Log message wording changed (e.g. `Mail delivery failed; kind=...; transport=...`); the old `MailerSend`/`SMTP delivery` strings are gone from docs where referenced. No verify script greps them (verify passed).
6. `git rm --cached` was attempted once by mistake and denied by the permission layer; no git write ran. Deleted files were removed from the working tree only.
