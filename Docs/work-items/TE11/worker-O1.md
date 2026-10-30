# Worker record - TE11 O1

Status: complete. Build: 0 errors. `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 292 passed, 0 failed, 0 skipped. Container run and CI not done (Testing role / later).

## Files changed

Code
- `src/GooseWebsite.Api/Shared/Email/MailModels.cs`: `RenderedMail` gains optional `ReplyTo` (`MailAddressee?`, last positional parameter, default null).
- `src/GooseWebsite.Api/Shared/Email/SmtpMailTransport.cs`: `CreateMessage` adds `ReplyToList` when `ReplyTo` is given.
- `src/GooseWebsite.Api/Shared/Email/BrandedEmailLayout.cs` (moved with `git mv` from `Modules/Subscriptions/Services`, namespace `GooseWebsite.Api.Shared.Email`): `unsubscribeUrl` is now nullable and a new optional `footerNote` parameter replaces the unsubscribe line when there is no link. With an unsubscribe link the footer line is built from the same text and styles as before.
- `Modules/Subscriptions/Services/AnnouncementEmailRenderer.cs`, `SubscriptionEmailRenderer.cs`: added `using GooseWebsite.Api.Shared.Email;` only.
- `Modules/Contact/Services/ContactEmailRenderer.cs` (new, public static): `Confirmation`, `OwnerNotice`, `Receipt`, `OwnerNoticeSubject`, `CleanName`. HTML plus plain text, visitor values HTML-encoded, message line breaks as `<br>`, names cleaned of control, Zl, Zp and Cf characters.
- `Modules/Contact/Services/ContactEmailDeliveryService.cs`: renders and sends with `IMailService.SendAsync`; same bool result, `ContactDeliveryOutcome`, `OwnerNotified` retry rule; only `Contact:RecipientEmail` is required now.
- `Modules/Contact/Models/ContactEmailTemplateVariables.cs`: deleted (nothing references it any more; `ContactEmailTemplateTests` used string literals).

Tests
- `ContactRenderedMailTests.cs` (new, 17 tests) and `ContactMailFlowApiTests.cs` (new, 3 tests).
- `AuthApiFactory.cs`: new `realContactDelivery` option (keeps the real `ContactEmailDeliveryService`, sets `Contact:RecipientEmail`; default keeps the stub, so all existing request tests are unchanged).
- `ContactEmailTemplateTests.cs`: removed the delivery-service/template tests (replaced by the rendered-mail tests); the three `MailerSendTransport` tests stay for TE12.

Docs
- `Docs/architecture.md` (diagram, Shared table incl. `BrandedEmailLayout` and `ReplyTo`, Contact delivery row, flow 5.2 diagram, mail rule, config rows, risk row), `Docs/email-templates/README.md`, `Docs/product-backlog.md` (evidence row in the decision log, TE11 not marked Done), this file.

## Tests per acceptance criterion
- AC-1: `VerificationRequest_SendsOneRenderedMail...`, `VerificationRequest_WhenTheMailFails_ReturnsFalse`, `FullFlow_WorksWithoutAnyMailerSendKeyOrTemplateId` (HTTP level).
- AC-2: `Delivery_SendsOwnerNoticeThenSenderReceipt_...`, `MessageKeepsItsLineBreaksInTheHtml`, full flow.
- AC-3: `VisitorValues_AreEscapedInTheHtml_AndUnchangedInThePlainText`, `SenderEmail_IsEncodedInTheNoticeHtml`.
- AC-4: `NamesWithLineBreaks_CannotAddHeadersOrRecipients`, `CreateMessage_WithAHostileDisplayName_DoesNotThrowAndAddsNoHeader`.
- AC-5: `Delivery_WhenTheOwnerNoticeFails_...`, `Delivery_WhenTheReceiptFails_...`, `Retry_AfterAFailedReceipt_SendsOnlyTheReceipt`, `Submit_WhenTheConfirmationMailFails_...`.
- AC-6: `CreateMessage_WritesReplyToWhenGiven_AndNoneWhenAbsent`, `RenderedMail_ReplyToDefaultsToNone_...`; account and subscription tests unchanged and green.
- AC-7: `Delivery_NeedsOnlyTheRecipientAddress_...`, `Delivery_WithoutARecipient_SendsNothing`, full flow (no key, no template ids).
- AC-8: full flow log assertions (no body, addresses, link, token), `ContactDoesNotReferenceSubscriptions_AndTheSharedLayoutLivesInShared`, existing `NoModuleTalksToSmtpOrMailerSendDirectly` and `DataAccessArchitectureTests` pass.

TDD note: tests were written first and failed to compile (no `ReplyTo`); then implemented to green.

## Deviations and doubts
- `verify/runtime/40-contact.sh` does not mention templates, so it was not changed.
- Byte-for-byte equality of the account and subscription mails was argued from the code (the unsubscribe footer line is the same text) and no test pins their full HTML; there is no golden-file test. Testing may want to diff one rendered subscription mail against `main`.
- Rendering uses `<br>` for line breaks instead of the design file's `white-space:pre-wrap` (Outlook ignores pre-wrap). Plain text keeps the original newlines.
- The footer notes of the three contact mails are short code-side texts (no site URL inside them), derived from the design files.
- `ContactOptions.Templates`, `Contact:Templates` in appsettings, `TemplateMail`, `SendTemplateAsync`, `MailerSendTransport` left for TE12 as ordered. `TestMailService` still records templated sends (TE12).
- Nothing was sent, no Docker or container touched, `.env` not read.

## Round 2: review findings closed

- Security F1: the sender address in the owner notice `mailto:` links is percent-encoded (`Uri.EscapeDataString`), then HTML-encoded. Test: `OwnerNotice_PercentEncodesTheSenderAddressInTheMailtoLink` (hostile `a?bcc=x&body=y@attacker.example`; the hrefs carry no `?` or `&`). It failed before the fix.
- `Truncate` no longer cuts between a surrogate pair. Test: `OwnerNotice_TruncatesALongNameWithoutSplittingASurrogatePair` (emoji at the cut).
- Reviewer advisory 1: the unsubscribe footer is pinned by `TheConfirmationMailFooterCarriesTheUnsubscribeLine_Pinned` (SubscriptionsApiTests, exact footer string with a link) and `ContactMails_HaveNoUnsubscribeFooter` (absence without a link).
- Tests: 4 added (3 in `ContactRenderedMailFollowUpTests`, 1 in `SubscriptionsApiTests`). Total 296 passed, 0 failed. Build 0 errors.
