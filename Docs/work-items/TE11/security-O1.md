# TE11 order O1 - Security review (commit 5dbd455)

Verdict: **pass** (no high or critical findings; one low finding recommended before TE11 closes).

Scope: ContactEmailRenderer, ContactEmailDeliveryService, RenderedMail.ReplyTo, SmtpMailTransport, BrandedEmailLayout (moved to Shared/Email). Contact tests run once: 42 passed, 0 failed. No Docker, no .env.

## Checks

| Area | Result |
| --- | --- |
| HTML encoding of name, email, reason, message | Pass. Every interpolation goes through `Encode` / `EncodeWithBreaks` (HtmlEncode first, then line breaks to `<br>`). Attribute contexts (`href`, `src`, `mailto:`) use HtmlEncode, which escapes quotes. |
| Verification URL, logo URL, site URL | Pass. Verification URL is server-built (`FragmentLink`), encoded in href and text. Logo and site URLs come from configuration, encoded in `BrandedEmailLayout`. No visitor control. |
| Reason | Pass. Allow-listed to two fixed values before it reaches a template. |
| Header / CRLF injection | Pass. Confirmation and receipt subjects are constants. Owner subject uses `CleanName` (strips control, line/paragraph separator and format chars, which also removes bidi overrides). `MailMessage` refuses CR/LF in headers, and `MailAddress` quotes/encodes hostile display names and throws `FormatException` on an invalid address (caught in `ContactSubmissionService`, pending message discarded). Name max 120 chars. |
| Plain-text spoofing | Pass (accepted). Name is cleaned before it enters the plain part; message and reason are as typed but sit under a "Message:" label, so they cannot add a header line outside it. |
| Mail relay | Pass. Visitor-chosen recipients receive only the confirmation (constant subject, fixed text, name in greeting) and only after the `TryAllowRecipient` per-address limit and the client limit, both still ahead of the send. The owner notice goes only to `Contact:RecipientEmail`. The receipt goes to the verified address only. Unchanged relay surface versus before. |
| Reply-To | Pass. Owner notice Reply-To is the sender (needed); receipt Reply-To is the owner. Confirmation has none. |
| Message leakage | Pass. `SendVerificationRequestAsync` has no message parameter; the confirmation cannot hold the body. The body appears only in the owner notice and the receipt to the verified sender. |
| Logging | Pass. Delivery logs reason and outcome only. `SmtpMailTransport` logs nothing; `MailService` logs kind, transport, failure category and exception type names. No bodies, addresses, links or tokens added. |
| Secrets | Pass. None added in code, docs or tests. |
| Unsubscribe line in BrandedEmailLayout | Pass. Subscription mail still passes a non-null `unsubscribeUrl` and renders the same line; contact mail passes null and shows only a static footer note. Change only adds an optional branch; no new personal data in subscription mail. |

## Findings

### F1 - Low: `mailto:` reply button can be steered by a crafted sender address
- Location: `ContactEmailRenderer.OwnerNotice` (`emailCell` link and `Button($"mailto:{Encode(senderEmail)}", ...)`).
- Risk: the address is only checked by `EmailAddressAttribute` and `MailAddress`, which accept characters such as `?`, `&` and `=` in the local part (for example `a?bcc=x&body=y@attacker.example`). The attacker must control that inbox to pass verification. In the owner's mail client the `mailto:` link then parses as recipient `a` with query fields (`bcc`, `cc`, `body`, `subject`), so Gustavo's one-click reply could go to the wrong address or carry hidden Bcc. HTML encoding does not prevent this because the problem is URL syntax, not HTML. The Reply-To header is not affected.
- Recommended fix: build the link with `Uri.EscapeDataString(senderEmail)` (percent-encode `?`, `&`, `=`), or drop the `mailto:` and rely on Reply-To plus the visible address. Optionally reject addresses with characters outside a conservative set in `ContactSubmissionService`. Add a renderer test with such an address.

### F2 - Informational
- `Truncate(name)` cuts at 60 chars and can split a surrogate pair; the lone surrogate is only encoded into the button label (cosmetic).
- Name in the confirmation greeting goes to an arbitrary third party (up to 120 chars of attacker text, no links as HTML). Same exposure as the MailerSend template had; the per-address limit bounds it. Accepted.
- Sender address appears unmodified in the plain-text owner notice; it cannot contain a line break because `MailAddress` rejects it earlier in the confirmation step.
