# Testing report - F9-US6 (branded account verification email)

Role: Testing. No source file was edited and no git write command was run.

## Environment

| Item | Value |
| --- | --- |
| Date | 2026-10-22 (real clock, local PDT, 12:48 to about 13:00) |
| Git | branch `f9-emails-and-verification`, HEAD `bf5217e798840444ed94b407430a56c74ef3dfca` (merge of PR #20). The F9-US6 work is UNCOMMITTED: modified `Docs/architecture.md`, `Docs/email-templates/README.md`, `SmtpAccountEmailSender.cs`, `PublicLinkBuilder.cs`; untracked `AccountVerificationEmailRenderer.cs`, `AccountVerificationEmailTests.cs`, `Docs/email-templates/account-verification-preview.png`, `Docs/email-templates/client-check.md`, `Docs/work-items/F9-US6/` |
| OS | Windows 11 Pro 10.0.26200 |
| .NET SDK | 10.0.300 |
| Node | v22.15.0 |
| Browser for Karma | Chrome 155.0.0.0 headless (`ChromeHeadlessNoSandbox`) |
| Docker | 29.8.2 (Docker Desktop); image `goosewebsite-web` id `b701ab57da2d`, built 2026-10-22 12:50 PDT by the last container run |

## Commands

- A: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (repo root; also run with a TRX logger to get per-test names)
- B: `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`
- C: `./scripts/verify.ps1 -SkipCi` (PowerShell, repo root). CI is not applicable until the Captain pushes.

## Totals per suite and run

| Suite | Run 1 | Run 2 | Run 3 | Result |
| --- | --- | --- | --- | --- |
| A .NET (xUnit) | 169 passed, 0 failed | 169 passed, 0 failed | 169 passed, 0 failed | pass |
| B Angular (Karma) | 97 SUCCESS | 97 SUCCESS | 97 SUCCESS | pass |
| C Container verify (404 = 169 dotnet + 97 angular + 138 runtime) | 404 passed, 0 failed, 0 skipped | 404 passed, 0 failed, 0 skipped | 404 passed, 0 failed, 0 skipped | pass |

## Irregular runs (nothing hidden)

- C run 1, first attempt ABORTED before any test: the Docker build could not resolve `registry-1.docker.io` (`lookup registry-1.docker.io: getaddrinfow: The requested name is valid, but no data of the requested type was found`), verify.ps1 exited 2 ("The stack did not become healthy"). A DNS/network failure on this machine, an environment failure, no test executed. Repeated at once; the repeat is counted as run 1 (exit 0). Order: aborted, run 1, run 2, run 3.
- B: all three runs exited 0 on their own; no leftover processes needed to be killed.
- Karma prints no test names; its per-test results come from the verify.ps1 log (stage `unit-angular`, 97 named tests).

## Determinism

xUnit: sorted per-test name+outcome lists of the three A runs (from TRX): identical, 169 each. Container: sorted per-test lists (stage, name, result, duration removed) of the three counted C runs: identical, 404 lines each, 0 FAIL, 0 SKIP (stages: unit-dotnet 169, unit-angular 97, runtime 138). The unit-dotnet list equals the A list. No `nondeterministic` cases.

## Per-AC coverage

| AC | Automated coverage | Evidence (pass in 3/3 runs of A and of C) |
| --- | --- | --- |
| AC1 logo, design, plain-text alternative | Partial | `AccountVerificationEmailTests` (11 tests in the file): `HasBothAPlainTextAndAnHtmlView`; `HtmlShowsTheLogoWithSizeAndAltTextFromTheSiteItself` (logo img with width/height and alt, site's own address); `LogoSizeKeepsTheRealImageAspectRatio` (attributes keep the real PNG's ratio within one pixel); `LogoAddressIsBuiltFromThePublicBaseUrl`; `PlainTextKeepsTheLinkTheExpiryAndTheIgnoreSentence` (readable text view). Non-test artefacts present on disk, which I did not judge visually: `Docs/email-templates/account-verification-preview.png` and the manual checklist `Docs/email-templates/client-check.md`. REAL MAIL CLIENTS (Gmail, Outlook, Apple Mail, phones, dark mode) ARE NOT COVERED BY ANY AUTOMATED TEST; the logo also cannot load from localhost. "Follows the approved design" is not asserted by any test. The contact acknowledgement's plain-text alternative lives in the MailerSend dashboard and is not testable from code (recorded limit). Verification of AC1 in real clients is pending Gustavo's manual checklist. |
| AC2 no tracking or third-party resources; links, expiry, privacy unchanged | Yes (structure and text) | `HtmlUsesNoThirdPartyOrActiveContent` (no third-party host, script, link, tracking image); `HtmlKeepsTheLinkTheExpiryAndTheIgnoreSentence` and `PlainTextKeepsTheLinkTheExpiryAndTheIgnoreSentence`; `DisplayNameIsHtmlEncodedInTheHtmlViewOnly`; `SendsOneMessageToTheRecipientWithTheUnchangedSubject`; `WhenSmtpIsNotConfiguredNothingIsSentAndItReportsFailure`; `PublicLinkBuilder_BuildsASiteAddressForAPath`. Not tested: that no email address, token or link is logged (no test visible for log content in the new code; contact tests cover logs for MailerSend only). |
| AC3 mail-sink tests still pass | Yes | All pass in 3/3 runs. Visible names: `ContactEmailTemplateTests` (10: Delivery_SendsOwnerNoticeThenSenderReceipt_EachWithEveryVariable, Delivery_WhenOwnerNoticeFails_DoesNotSendReceipt, Delivery_WhenReceiptFails_ReportsItAndTheRetryOnlySendsTheReceipt, Delivery_WithoutTemplateIdsOrRecipient_SendsNothing, LogoUrl_IsTheSiteAddressUnlessOverridden, MailerSendClient_* x4, VerificationRequest_UsesConfirmationTemplate_WithItsVariablesAndNeverTheMessage); `ContactVerificationApiTests` (8: Submit_EmailsLinkToSender_AndNeverReturnsTokenOrUrl, Submit_LimitsVerificationEmailsPerRecipientAddress_IgnoringCase, Submit_WhenVerificationEmailFails_ReturnsBadGatewayAndSendsNothing, Verify_ConcurrentConfirmationsDeliverExactlyOnce, Verify_DeliversOnceOnPost_AndRejectsReplayAndGet, Verify_RejectsUnknownToken, Verify_WhenDeliveryFails_KeepsTheMessageSoTheSameLinkCanBeRetried, Verify_WhenOnlyTheReceiptFails_TheRetryDoesNotNotifyGustavoAgain); `AuthApiIntegrationTests` incl. ReaderRegistrationRequiresEmailConfirmationBeforeSignIn, PublicRegistrationIgnoresRequestedAdminRoleAndCreatesReaderAccount, FailedVerificationEmailCanBeRecoveredByResending, ExpiredReaderVerificationTokenDoesNotConfirmAccount, RegistrationAndVerificationResendResponsesDoNotRevealAccountExistence; `ReaderAccountServiceTests` (Register_*, VerifyEmail_ConfirmsTheAddressOnceAndRefusesAnInvalidToken). Runtime stage also passes its verification-token tests (GET never delivers, empty token rejected, unknown token 409). |

## Failures

None in counted runs. The only failure output was the aborted container attempt (DNS error, environment).

## Verdict list

- A .NET: 169/169 pass, 3/3 runs - pass
- B Angular Karma: 97/97 pass, 3/3 runs - pass
- C Container verify: 404/404 pass (169 + 97 + 138), 3/3 counted runs - pass
- Nondeterministic cases: none
- AC1: automated parts pass (structure, text, logo attributes); real mail clients NOT covered, manual checklist pending. AC2: pass (automated). AC3: pass.
- CI check: not applicable (nothing pushed).
