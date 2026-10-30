# Reviewer record - TE11 O1

Verdict: **accurate**

Reviewed commit 5dbd455 against `orders.md` and `decisions.md`, and read the Worker record. `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` run once by the Reviewer: 292 passed, 0 failed (matches the Worker). No Docker, no `verify.ps1`, no `.env`.

## Acceptance criteria

| AC | Proof | Judgment |
| --- | --- | --- |
| AC-1 | `VerificationRequest_SendsOneRenderedMail...` (subject, link in HTML and plain text, `Single`, no templated sends) and the full-flow test (`Assert.Single` after submit, so nothing for Gustavo; message body absent from HTML and plain text) | Proven |
| AC-2 | `Delivery_SendsOwnerNoticeThenSenderReceipt...` (order, recipients, both Reply-To, HTML and plain text, receipt copy, notice name/address/reason/message) and full flow over HTTP | Proven |
| AC-3 | `VisitorValues_AreEscaped...` runs hostile name, reason and message through all three mails | Proven |
| AC-4 | `NamesWithLineBreaks_...` goes through the real `SmtpMailTransport.CreateMessage` and checks To, Bcc, CC and headers; `CreateMessage_WithAHostileDisplayName...` covers the transport alone | Proven |
| AC-5 | Service-level tests for both failures and the retry, plus an HTTP-level 502 then retry test | Proven |
| AC-6 | `CreateMessage_WritesReplyToWhenGiven...`; account and subscription tests unchanged and green | Proven for Reply-To; "unchanged" rests on the existing tests, see (a) |
| AC-7 | `Delivery_NeedsOnlyTheRecipientAddress...`, `Delivery_WithoutARecipient...`, full flow with only `Contact:RecipientEmail` set | Proven |
| AC-8 | Full-flow log capture (no body, both addresses, token, link path); architecture tests pass; new Contact-not-referencing-Subscriptions test | Proven, see advisory 2 |

## The Worker's doubts

(a) **Account and subscription HTML unchanged: confirmed from the diff.** The old footer line was `<div style="padding-top:10px; font-family:{Sans}; font-size:12px; line-height:18px; color:#6c7199;">{UnsubscribeSentence} <a href="{encoded url}" style="color:#19e6ff; text-decoration:underline;">Unsubscribe</a></div>`. When `unsubscribeUrl` is non-null the new `footerLine` is the same string with the same encoding, in the same position. Only the namespace, the nullable parameter and the optional `footerNote` changed. Indentation is identical (one-line value on a line that keeps the same leading spaces). Only the two Subscriptions renderers use the layout (grep); Accounts has its own renderer and is untouched. No golden test pins the HTML, so this is argued rather than tested (advisory 1).

(b) **`<br>` instead of `white-space:pre-wrap`: acceptable.** It keeps line breaks in Outlook, normalises CRLF and CR, the text is encoded first so no markup can enter, and the plain-text part keeps the original newlines. Only side effect: runs of spaces collapse in the HTML part. Not worth changing.

(c) **Deleting `ContactEmailTemplateVariables`: acceptable.** The order allowed removing what TE11 makes unreachable and only asked to keep it if `ContactEmailTemplateTests` needed it. The solution builds, so nothing references it. It is a Contact-module file and is not a TE12 item (TE12 items: `Contact:Templates`, `TemplateMail`, `SendTemplateAsync`, `MailerSendTransport`, `ContactEmailTemplateTests` MailerSend tests). Those are all still present.

## Constraints

Contract and status codes unchanged (controller untouched; HTTP tests pass). Rate limits and honeypot untouched. `OwnerNotified` and the Delivered/Failed/OwnerNotifiedReceiptFailed outcomes unchanged in logic. The delivery service logs only the `delivered=` flag and the config warning, no values. No secrets in the diff, no real send (all tests use `TestMailService`). Docs (`architecture.md` rows 154/156/610/669/686, `email-templates/README.md`, backlog row) match the code; TE11 is not marked Done.

## Mismatches

None.

## Advisories

1. No golden-file test pins the subscription/account HTML. Testing could diff one rendered subscription mail against `main`, or a cheap test could assert the footer line of `SubscriptionEmailRenderer` output.
2. The AC-8 log test runs against `TestMailService`, so it proves the Contact code and controller log nothing sensitive but does not exercise `MailService`'s own logging (already covered by `MailServiceTests`).
3. `Truncate(name)` in `ContactEmailRenderer` cuts at 60 UTF-16 units and can split a surrogate pair in the button label; cosmetic.
4. The three contact footers are short code texts without the site URL that the design files carry; the layout footer already shows the site link, so nothing is lost.
5. The new backlog row sits directly after a prose paragraph, like the TE11/TE12/TE13 intake row above it, so it may not render inside a table. Pre-existing pattern.
6. Gap 3 (no real email sent) stays open, as the Worker stated.
