# Worker record - orders O1 and O2 (F9-US6)

## O1
- Red observed: 7 of the 10 new tests failed against the old plain-text sender (no views, no logo, name not encoded; stubs for the new constructor parameter and `SiteLink` were added first so they compiled).
- Green: full suite `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` passed, 168 of 168.
- Changed: `Modules/Accounts/Services/SmtpAccountEmailSender.cs` (now takes `PublicLinkBuilder`, sends `text/plain` and `text/html` alternate views, subject unchanged), new `AccountVerificationEmailRenderer.cs`, `Shared/Hosting/PublicLinkBuilder.cs` (new `SiteLink(path)`).
- Tests: new `tests/GooseWebsite.Api.Tests/AccountVerificationEmailTests.cs` (10 tests). `IAccountEmailSender`, `ReaderAccountService`, the contact code and their tests are unchanged.
- Logo: `{PublicBaseUrl}/email/gcv-logo.png`, no override option (kept Accounts independent of Contact).

## O2
- Docs: `Docs/email-templates/README.md`, new `Docs/email-templates/client-check.md`, `Docs/architecture.md` (Accounts table row).
- Preview: `Docs/email-templates/account-verification-preview.png`, rendered in headless Chrome with sample values (logo from a local file URL for the preview only).
