# Testing report - TE11 (contact emails as rendered mail over SMTP), three runs, round 2

Role: Testing. Date: 2026-10-29. Branch `te11-contact-over-smtp`; code under test includes c5c1b93 (deterministic hosted-loop test). HEAD was 07b123d when the run started (3c13617 when it ended: a later commit by someone else, no code touched by this role). No code edited, nothing committed.

**Final verdict: PASS.** Three identical runs of every layer, no test differed, plus 10 extra runs of the previously flaky test under build load, all passed.

## Earlier failure (previous Testing run, HEAD 96460a4)

`AnnouncementsApiTests.TheHostedLoopDeliversInTheBackgroundWithoutTheEditorRequestWaiting` (F10-US3) failed once in `verify.ps1` run 1 (14000 ms) and passed in the other runs, so that round was `nondeterministic`. Cause: a 10 s polling budget for a hosted loop with a 1 s poll interval, exceeded under load right after a fresh image build. The failure message was never captured because `unit-dotnet.out` was overwritten. Fixed by c5c1b93 (makes the test deterministic). This round keeps each run's output under a per-run name.

## Environment

- Windows 11 Pro, Docker 29.8.2, .NET 10 SDK, Angular 20 Karma on Chrome Headless 156 (Windows 10), Playwright (Chromium, Firefox, WebKit) in the verification container. Only environment: the local container stack. CI not part of this run (`-SkipCi`).
- Per-run outputs kept (not in the repo): `scratchpad\r\` holds `dotnet{1,2,3}.trx/.out`, `karma{1,2,3}.out`, `verify{1,2,3}.log`, `verify{1,2,3}-unit-dotnet.out`, `verify{1,2,3}-unit-angular.out`, `flaky{1..10}.out`, `build.out`.
- No real email sent. No valid contact message or subscription submitted. `.env` not read or printed.

## Per-layer results

| Layer | Run 1 | Run 2 | Run 3 | Per-test comparison |
| --- | --- | --- | --- | --- |
| `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | 296 passed, 0 failed, 0 skipped | 296/0/0 | 296/0/0 | 296 names plus outcome from the TRX files, sorted: identical (cmp) |
| Karma (README command, `ChromeHeadlessNoSandbox`, `cmd /c`) | 234 of 234 SUCCESS | 234 of 234 | 234 of 234 | the direct reporter prints no per-spec lines; the 234 named unit-angular lines in `verify` are identical in all runs, and the totals match |
| `./scripts/verify.ps1 -SkipCi` | 1038 passed, 0 failed, 1 skipped, 0 known (exit 0) | same (exit 0) | same (exit 0) | `latest.log` of the three runs, timings removed and sorted: identical (1073 lines each) |

`verify.ps1` per stage: unit-dotnet 296 PASS, unit-angular 234 PASS, runtime 187 lines (186 PASS and 1 SKIP, including `40-contact.sh`), browser 326 lines. The in-container `dotnet test` reports 296 passed in each of the three kept outputs.

## Load test of the previously flaky test

`TheHostedLoopDeliversInTheBackgroundWithoutTheEditorRequestWaiting` was run 10 times in a row (`dotnet test --no-build --filter`) while `docker compose build --no-cache web` and the stack start were running in parallel (the build had not finished during any of the 10 runs): 10 of 10 passed, about 2 s each.

## Skipped test (same in all three runs)

`runtime: mail provider authorizes the container's API key` is SKIPPED because `MAILERSEND_API_KEY` is not configured in `.env`. Unrelated to TE11 (MailerSend is TE12 scope), but the key-authorization check did not run.

## AC map (xUnit names; all Passed in all three direct runs and in all three verify runs)

| AC | Tests |
| --- | --- |
| AC-1 one rendered mail to the sender, link in HTML and text, no message body, nothing to Gustavo | `ContactRenderedMailTests.VerificationRequest_SendsOneRenderedMailToTheSender_WithTheLinkAndNeverTheMessage`; `VerificationRequest_WhenTheMailFails_ReturnsFalse`; `ContactMailFlowApiTests.FullFlow_WorksWithoutAnyMailerSendKeyOrTemplateId`; `Submit_WhenTheConfirmationMailFails_AnswersBadGatewayAndSendsNothingToGustavo`; `ContactVerificationApiTests.Submit_EmailsLinkToSender_AndNeverReturnsTokenOrUrl` |
| AC-2 two mails, notice to owner with Reply-To sender, receipt to sender with Reply-To owner, HTML and text | `ContactRenderedMailTests.Delivery_SendsOwnerNoticeThenSenderReceipt_EachWithHtmlPlainTextAndReplyTo`; `MessageKeepsItsLineBreaksInTheHtml`; `ContactMailFlowApiTests.FullFlow_...`; `ContactVerificationApiTests.Verify_DeliversOnceOnPost_AndRejectsReplayAndGet` |
| AC-3 escaped in HTML, unchanged in plain text | `ContactRenderedMailTests.VisitorValues_AreEscapedInTheHtml_AndUnchangedInThePlainText`; `SenderEmail_IsEncodedInTheNoticeHtml`; `ContactRenderedMailFollowUpTests.OwnerNotice_PercentEncodesTheSenderAddressInTheMailtoLink`; `OwnerNotice_TruncatesALongNameWithoutSplittingASurrogatePair` |
| AC-4 CR/LF cannot add headers or recipients | `ContactRenderedMailTests.NamesWithLineBreaks_CannotAddHeadersOrRecipients`; `CreateMessage_WithAHostileDisplayName_DoesNotThrowAndAddsNoHeader` |
| AC-5 retry behaviour unchanged | `ContactRenderedMailTests.Delivery_WhenTheOwnerNoticeFails_RestoresWithNothingSentToTheSender`; `Delivery_WhenTheReceiptFails_ReportsItAndTheRetryOnlySendsTheReceipt`; `ContactMailFlowApiTests.Retry_AfterAFailedReceipt_SendsOnlyTheReceipt`; `ContactVerificationApiTests.Verify_WhenDeliveryFails_KeepsTheMessageSoTheSameLinkCanBeRetried`; `Verify_WhenOnlyTheReceiptFails_TheRetryDoesNotNotifyGustavoAgain`; `ContactSubmissionModuleTests.Verify_WhenDeliveryFails_KeepsThePendingMessageForTheSameLink`; `Verify_WhenOnlyTheReceiptFails_TheRetryRemembersGustavoWasNotified` |
| AC-6 Reply-To written when given, none when absent; account and subscription mails unchanged | `ContactRenderedMailTests.CreateMessage_WritesReplyToWhenGiven_AndNoneWhenAbsent`; `RenderedMail_ReplyToDefaultsToNone_SoExistingMailIsUnchanged`; `ContactRenderedMailFollowUpTests.ContactMails_HaveNoUnsubscribeFooter`; `SubscriptionsApiTests.TheConfirmationMailFooterCarriesTheUnsubscribeLine_Pinned`; existing account and subscription mail tests green (no golden-file test of the full HTML) |
| AC-7 works with no MailerSend key and no template ids | `ContactRenderedMailTests.Delivery_NeedsOnlyTheRecipientAddress_NoTemplateIdsNoApiKey`; `Delivery_WithoutARecipient_SendsNothing`; `ContactMailFlowApiTests.FullFlow_WorksWithoutAnyMailerSendKeyOrTemplateId` |
| AC-8 no body, address, link or token in logs; architecture tests | `ContactMailFlowApiTests.FullFlow_...` (log assertions); `ContactSubmissionModuleTests.Submit_DoesNotLogSenderEmailOrMessageBody`; `ContactRenderedMailTests.ContactDoesNotReferenceSubscriptions_AndTheSharedLayoutLivesInShared`; `MailServiceTests.NoModuleTalksToSmtpOrMailerSendDirectly`; `DataAccessArchitectureTests.*` (4 tests) |

(Test names were carried over from the earlier round's map; this round confirmed all 296 tests Passed in every run, and the TRX lists contain no failed or skipped test.)

## Gaps (not covered)

- Real SMTP or provider delivery: not covered, no real email sent by design. Contact mail not seen in a real mail client (CLAUDE.md gaps 2 and 3 remain open).
- No golden-file test of the whole account and subscription mail HTML against `main`; only the unsubscribe footer is pinned.
- MailerSend key authorization runtime check skipped (key absent in `.env`).
- The direct Karma run has no per-spec output; per-spec identity is shown through the `verify` logs.
- Real Edge and Safari checked by a person: still open. Pull request CI: separate.

## Cleanup

`goosewebsite-caddy` and `goosewebsite-web` were stopped by name after the runs. No other container or process was touched (the host also runs unrelated containers; they were not inspected or changed). `verify.ps1` backed up and reset the stack data volume as designed.
