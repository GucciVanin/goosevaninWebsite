# Security review O1 (F10-US1: subscribe and confirm)

Reviewer: Security. Scope: commit 9ad124a (Subscriptions module, migration, client form and confirm page). Read-only static review; nothing was run.

**Verdict: pass** (no critical or high findings; one medium and four low findings, none blocking).

## What was checked and held up

- Tokens: 32 bytes from `RandomNumberGenerator`, Base64Url; only the SHA-256 hash is stored (unique index); lookup is by hash in the database, so no secret comparison timing issue. Confirmation clears the hash and expiry (single use); expired or unknown or already-used tokens all give the same 400. Token length capped at 200.
- Link: built from the configured `PublicBaseUrl`, never from the Host header or a request value (no open redirect, no host-header poisoning). Token travels in the URL fragment, so it is not in access logs or Referer. The confirm page reads it from the fragment, removes it from the address bar before use, and confirms only on a button press via POST (mail scanners cannot confirm).
- CSRF: both POST endpoints use `[ValidateAntiForgeryToken]`; the client interceptor supplies the token. Rate limits per client address (5 subscribe, 20 confirm per 10 minutes), plus a per-address 2 minute resend cooldown.
- Response uniformity: the subscribe endpoint returns the same 200 and message for new, pending, confirmed, invalid and honeypot input. Honeypot stores and sends nothing.
- Injection: names lose control characters; emails are validated by a strict ASCII regex with no whitespace, comma, semicolon or angle brackets; both length-limited and sanitised again by `SubscriberAccessRule`. In the HTML email, name, link, logo and site URL are all `WebUtility.HtmlEncode`d; the link is a fixed https base plus an escaped token. The display name goes through `System.Net.Mail.MailAddress`, which encodes it. The Angular templates interpolate (auto-escaped); no `innerHTML`.
- Data exposure: no endpoint reads Subscribers; the controller returns only fixed messages. Rule is `WriteAccess.Anyone` for writes only. Logs carry only the outcome enum, counts and exception type names; `MailService` logs categories only.
- Cleanup job: hourly, deletes only Pending rows past expiry, own scope, failure logged by exception type only.
- Migration: creates the table with the three unique indexes and NOCASE email collation; no seed data.

## Findings

### S1 - Medium - Timing reveals whether an address is already confirmed
Location: `SubscriptionService.HandleSubscribeAsync`, `SendConfirmationAsync`.
Risk: the mail is sent inline (awaited) for Created and ResentConfirmation, while AlreadyConfirmed, ThrottledResend and Invalid return immediately. The uniform message is defeated by response time (an SMTP round trip versus milliseconds), so an attacker can probe, within the rate limit, which addresses are confirmed subscribers (and a pending address inside its cooldown looks "fast" too).
Fix: send the mail off the request path (a queued background send), or pad every outcome to a common minimum duration. Accept as a documented residual risk if the owner decides the list is not sensitive; it is limited to 5 probes per 10 minutes per client address.

### S2 - Low - Rate limit key depends on correct proxy configuration
Location: `SubscriptionsModule.PartitionByClient` (uses `RemoteIpAddress`).
Risk: `UseForwardedHeaders` honours only configured `ForwardedHeaders:KnownProxies`. If Caddy's address is not listed, every visitor shares the proxy IP and 5 requests per 10 minutes would lock out all visitors; if headers are trusted too broadly, the limit can be evaded by spoofing.
Fix: confirm in the container run that two different client addresses get separate limiter buckets (and that a spoofed `X-Forwarded-For` from outside is ignored).

### S3 - Low - Email can be used to send site-branded mail with attacker-chosen text to a third party
Location: greeting `Hi {name}` in `SubscriptionEmailRenderer`.
Risk: anyone can cause one confirmation email per address per 2 minutes (5 per 10 minutes per client) to any address, with a 100 character name of their choosing. This is inherent to double opt-in; the body is fixed and links only to the site.
Fix: none required. Optionally lower the cooldown abuse by adding a per-address daily cap.

### S4 - Low - Unicode format and line-separator characters are allowed in the name
Location: `SubscriptionService.CleanName` (strips `char.IsControl` only).
Risk: U+2028/U+2029 and bidi overrides (U+202E) are not "control" characters and pass through. No header injection is expected (`MailAddress` encodes the display name), but they can visually spoof the greeting or break plain-text layout.
Fix: also drop characters of category Zl, Zp and Cf in `CleanName`.

### S5 - Low - A later submission invalidates an earlier pending link
Location: resend branch (`existing.ConfirmationTokenHash = ...`).
Risk: a third party submitting a victim's pending address after the cooldown replaces the token, so the victim's first email stops working (nuisance only; the new email arrives).
Fix: acceptable; optionally keep the old token valid until it expires or is replaced after delivery.

## Notes
`PublicBaseUrl` falls back to `https://localhost` when unset; make sure the Docker `.env` sets it so emailed links never point at localhost (not a vulnerability).
