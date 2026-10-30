# Orders - TE11

## O1 - Contact emails as rendered mail over SMTP

**Target:** `src/GooseWebsite.Api` (Shared/Email, Modules/Contact, move of `BrandedEmailLayout`), tests in `tests/GooseWebsite.Api.Tests`, runtime script `verify/runtime/40-contact.sh` only if it depends on templates, docs `Docs/architecture.md`, `Docs/email-templates/README.md`, `Docs/product-backlog.md` evidence note under F2-US2.

**Action (implement with TDD):**
1. Add an optional `ReplyTo` (`MailAddressee?`) to `RenderedMail` and write it as the message Reply-To in `SmtpMailTransport.CreateMessage`.
2. Move `BrandedEmailLayout` from `Modules/Subscriptions/Services` to `Shared/Email` (namespace and usings; Subscriptions and Accounts keep working). Add an architecture test or extend the existing one so Contact never references Subscriptions.
3. Add `Modules/Contact/Services/ContactEmailRenderer` rendering three mails (HTML plus plain text) from the designs in `Docs/email-templates/` (confirmation: name, verification link; owner notice: sender name and address, reason, message; receipt: name, reason, copy of the message). Every visitor value (name, email, reason, message) is HTML-encoded in the HTML part; the message keeps its line breaks. Subjects as in `decisions.md`; the subject uses the name with control and line-break characters removed.
4. Change `ContactEmailDeliveryService` to render and send with `IMailService.SendAsync`. Keep every behaviour of `SendVerificationRequestAsync` and `SendAsync` (boolean result, `ContactDeliveryOutcome`, `OwnerNotified` retry rule, the missing-configuration path now only needs `Contact:RecipientEmail`).
5. Do not remove `Contact:Templates`, `TemplateMail`, `SendTemplateAsync`, `MailerSendTransport` or `ContactEmailTemplateTests` (TE12). Remove only what TE11 makes unreachable inside the Contact module; leave `ContactEmailTemplateVariables` if `ContactEmailTemplateTests` still needs it.
6. Update the contact tests that asserted template ids to assert the rendered mails; keep all request-level contact tests passing.
7. Update `Docs/architecture.md` (contact delivery section, mail rules, flow 5.x if it names templates) and `Docs/email-templates/README.md` (the HTML files are now the design source for the code renderer, not MailerSend templates). Add the evidence note for TE11 under F2-US2 or the TE11 row; do not mark TE11 Done.

**Constraints (must not change):** the contact API contract and status codes; rate limits; honeypot; expiry, single use, restore and `OwnerNotified` semantics; no durable message storage; logs never contain bodies, addresses, links or tokens (`MailService` remains the only mail logger); the account and subscription emails render byte-for-byte as before apart from the namespace move; no secrets in the repo; no real email is sent by any test.

**Acceptance criteria (each needs a test):**
- AC-1: submitting a valid request sends exactly one SMTP-style rendered mail to the submitted address with a subject `Confirm your email to send your message to Gustavo`, the verification link in HTML and plain text, and no message body; nothing goes to Gustavo.
- AC-2: confirming sends exactly two rendered mails: the owner notice to `Contact:RecipientEmail` with Reply-To = the sender, then the receipt to the sender with Reply-To = the owner, each with HTML and plain text; the receipt holds a copy of the message and the notice holds sender name, address, reason and message.
- AC-3: visitor values containing `<script>`, `&`, quotes and `<b>x</b>` appear escaped in the HTML part and unchanged in the plain-text part.
- AC-4: names or subjects containing CR/LF cannot add headers or recipients (the subject and Reply-To carry no line breaks; `CreateMessage` of a hostile name does not throw an unhandled error and sends no extra header).
- AC-5: retry behaviour is unchanged: a failed owner notice restores the message with nothing sent to the sender; a failed receipt restores it with `OwnerNotified` so a retry sends only the receipt.
- AC-6: `SmtpMailTransport.CreateMessage` writes Reply-To when given and none when absent; the account and subscription mails are unchanged.
- AC-7: with no MailerSend API key and no template ids configured, the whole contact flow works end to end against the test mail service (this is the point of TE11).
- AC-8: logs from a full flow contain no body, address, link or token; the architecture tests (module boundaries, Contact not referencing Subscriptions, `NoModuleTalksToSmtpOrMailerSendDirectly`) pass.

**Verification the Worker runs:** `dotnet build GooseWebsite.slnx -p:SkipClientBuild=true`, `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (the client must have been built once). The container run (`./scripts/verify.ps1 -SkipCi`) is for the Testing role, not the Worker.
