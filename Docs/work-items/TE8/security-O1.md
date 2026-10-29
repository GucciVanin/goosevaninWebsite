# Security review - TE8 O1 (one Mail module)

Scope: uncommitted changes under `Shared/Email`, `AccountEmailSender`, `ContactEmailDeliveryService`, `Program.cs`, `appsettings.json`, `docker-compose.yml`, `.env.example`. Static review only; no code edited, no containers touched.

## Verdict: PASS (no blocking findings; 3 low/informational notes)

| # | Area | Severity | Result |
| --- | --- | --- | --- |
| 1 | Secrets in repo | - | PASS |
| 2 | Secrets in logs/exceptions | - | PASS |
| 3 | Logging of recipients, bodies, links, tokens, provider text | - | PASS |
| 4 | Header injection (recipient, subject) | Low (note) | PASS |
| 5 | TLS | Low (note) | PASS |
| 6 | Provider text reaching the client | - | PASS |
| 7 | Privacy rule 6 (no durable contact storage) | - | PASS |
| 8 | Fallback config / HttpClient logging | Info | PASS |

## Details

1. Secrets: `appsettings.json` only holds empty `Smtp` Host/Username/Password and a `MailerSend.FromName`; the empty ApiKey/FromEmail defaults were removed. `docker-compose.yml` passes `${MAILERSEND_API_KEY:-}` etc. from `.env`; `.env.example` has empty values only. No secret values in the diff.
2. Secrets in logs/exceptions: the API key is used only in the `Authorization` header of a per-request message; SMTP credentials only in `NetworkCredential`. Neither is interpolated into any string. Exceptions are never logged or rethrown with message text: `MailService` and both transports record `exception.GetType().Name`, SMTP status code and inner type only (comments explain why: malformed-address exception messages contain the address).
3. Logging: `MailService` is the only mail logger and emits kind, transport name, failure category, HTTP status, SMTP status, exception type, and the MailerSend `templateId` (a configuration id, not sensitive). No address, name, subject, body, link, token or response body. Transports never log. Contact/Accounts loggers log only reason, outcome, clientKey and token length (not the token). Existing `ContactSubmissionService` log lines log `clientKey` (pre-existing, not part of this change).
4. Header injection: SMTP path uses `System.Net.Mail.MailMessage`/`MailAddress`, which validate addresses and encode/reject CR/LF in subject and display names. SMTP subject is a constant (`AccountVerificationEmailRenderer.Subject`). The one dynamic subject (`New contact message from {message.Name}`) goes only through the MailerSend JSON API (serialized, no header splicing). Note (Low): `ContactMessage.Name` has only `Required, StringLength(120)`, so CR/LF is not rejected at the contract; harmless today because of the JSON path, but if a dynamic subject or name is ever sent over SMTP, rely on `MailAddress`/`MailMessage` validation or add a no-control-characters check. A malformed recipient now yields `TransportError` instead of throwing (improvement; Contact still handles `FormatException` for the template path).
5. TLS: SMTP sets `EnableSsl = true` unconditionally (STARTTLS on 587 / implicit per .NET behavior), default certificate validation (no custom callback), credentials only sent over that channel. MailerSend `BaseUrl` defaults to `https://`. Note (Low): `MailerSendOptions.BaseUrl` is overridable by configuration and not checked to be https; it is operator-controlled, so acceptable, but a startup check for `https` would harden it.
6. Client exposure: `MailResult` carries only a `MailFailure` enum, never provider text; the interface never throws. Callers reduce it to a bool (`Succeeded`), and controllers do not reference mail types. Provider text cannot reach the client.
7. Privacy rule 6: pending contact messages remain in the in-memory `_pending` dictionary of `ContactVerificationService` (unchanged); the mail layer stores nothing and `MailService` does not retain mail content. Message body is passed only as a template variable in the outbound request.
8. Legacy fallback: both `MailerSend` (legacy) and `Email:MailerSend` sections bind; the new one wins only for keys it sets. Compose sets `Email__MailerSend__*` explicitly (possibly empty), which overrides a legacy value; this is a configuration-behavior note, not a vulnerability. HttpClient factory default logging records method and URL (`https://api.mailersend.com/v1/email`, no query, no headers), at Information level: no sensitive data.

## Residual notes (non-blocking)
- Add an https check for `MailerSend:BaseUrl` (optional hardening).
- Consider rejecting control characters in `ContactMessage.Name` (defence in depth).
- The `emailed` retry loop in Contact is unchanged and still treats Format/ArgumentException as a failed send, consistent with the new no-throw service.
