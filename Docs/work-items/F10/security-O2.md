# Security review O2 (F10-US2: unsubscribe with one click)

Reviewer: Security. Scope: commit 868a4f7 (unsubscribe endpoints, `SubscriptionLinks`, `RenderedMail.Headers`, SMTP header writing, client unsubscribe page, migration dropping `UnsubscribeTokenHash`, name stripping). Read-only static review; nothing was run.

**Verdict: pass** (no critical or high findings; two medium-low items worth fixing and several low or informational ones, none blocking).

## What was checked and held up

- Token forgery and purpose isolation: the token is the subscriber id sealed by an `IDataProtector` with its own purpose string (`GooseWebsite.Subscriptions.Unsubscribe.v1`). Authenticated encryption means a token cannot be forged, altered or produced from another purpose (cookies, antiforgery). Tampered or foreign input throws `CryptographicException`, which is caught and treated as invalid. The plaintext is parsed with `NumberStyles.None` and must be greater than 0.
- Enumeration of ids: ids are only ever visible inside ciphertext. Without the key ring an attacker cannot build a token for id N+1. The Sqlite table uses `Autoincrement` (verified in the AddSubscribers migration), so ids are never reused and a token for a deleted subscriber finds nothing and cannot touch a later subscriber.
- Replay: a second use returns the same 400 as an invalid token. The operation is idempotent in effect (the row is gone). Holding a token only lets someone remove that one subscriber, which is the person the mail was sent to.
- Neutral responses: an invalid, tampered, oversize (limit 1000 characters) or already-used token all give the same 400 and fixed message. Logs say only "Subscriber removed by an unsubscribe request" (no id, name, address or token).
- One-click without CSRF: `[IgnoreAntiforgeryToken]` is correct for RFC 8058 (mail providers post with no session). The endpoint needs both the unforgeable token and the exact `List-Unsubscribe=One-Click` form field, so a third party with no token can do nothing, and a cross-site form cannot unsubscribe anyone it has no link for. No cookie authentication is used on it, so there is no ambient authority to abuse.
- GET: no GET action is mapped on `unsubscribe` or `unsubscribe/one-click`, so a scanner that fetches the link changes nothing. The page URL carries the token in the fragment, and deletion needs a button press plus a CSRF-protected POST, so even a script-running scanner cannot unsubscribe anyone (it would have to click).
- Header injection: `List-Unsubscribe` values are built from the Base64Url Data Protection output (escaped again with `Uri.EscapeDataString`) and the configured `PublicBaseUrl`; no visitor value reaches a header name or value. `MailMessage.Headers.Add` also rejects line breaks, and `CreateMessage` runs inside the `try`, so a bad header becomes a `TransportError` category rather than an unhandled exception. The display name goes through `MailAddress`.
- Client page: token read from the fragment (never sent to the server on load, absent from access logs and `Referer`), address bar cleaned with `replaceUrl` before the request, token field cleared in `finish`. The template uses interpolation only.
- Migration: drops the index first, then the column; `Down` restores both. No data is read or written. Dropping the stored hash is a privacy improvement (nothing in a database copy can unsubscribe anyone).
- Name stripping: removing control, line and paragraph separator and format (Cf) runes closes bidi and zero-width spoofing in the display name and in the mail header path; invalid UTF-16 is replaced, not passed through.

## Findings

### S1 - Medium - Losing the Data Protection key ring silently breaks every unsubscribe link already sent
Location: `SubscriptionLinks` (design), `AccountsModule.AddDataProtection` (`PersistKeysToFileSystem`).
Risk: unsubscribe tokens are stateless, so they exist only as long as the keys that sealed them. If the key directory is lost (volume wiped, container rebuilt without the volume, keys revoked, a changed `DataProtection:KeysPath`), every unsubscribe link in mail already delivered returns "no longer valid" and subscribers have no way to opt out. That is a legal and trust problem (opt-out must keep working), and the failure is silent because the response is deliberately neutral. Normal rotation is safe (old keys stay readable); revocation or loss is not.
Fix: document that the key ring is as important as the database and must be backed up with it; do not call `RevokeAllKeys` or similar; add a startup or health log line when the key ring is freshly created while Subscribers rows exist. Optionally offer a fallback (for example a manual "unsubscribe me" path through the contact form or an operator command that removes an address).

### S2 - Low - One-click endpoint has no request size limit and parses the form for any caller
Location: `SubscriptionsController.UnsubscribeOneClick`.
Risk: the endpoint is anonymous and binds `[FromForm]`, so the framework buffers and parses a form body up to the server default (30 MB for Kestrel, form value count 1024) before the action runs. The rate limiter runs before binding (20 requests per 10 minutes per client) which bounds the abuse, but each request can still cost up to 30 MB of buffering.
Fix: add `[RequestSizeLimit(1024)]` (and optionally `[RequestFormLimits(ValueCountLimit = 2, KeyLengthLimit = 64, ValueLengthLimit = 64)]`) on the one-click action. The token in the query string is already capped at 1000 characters by the service.

### S3 - Low - One-click and confirm share one rate limit bucket per address, and large mail providers share addresses
Location: `SubscriptionsModule.AddRateLimiting` (`ConfirmRateLimitPolicy` reused by `Unsubscribe` and `UnsubscribeOneClick`).
Risk: RFC 8058 posts come from the mail provider's servers, not the subscriber. Many subscribers on one large provider arrive from a small set of addresses, so 20 requests per 10 minutes may reject legitimate unsubscribes with 429 (providers may not retry). Conversely the same bucket is shared with the confirm button for a given address. Also, as noted in O1 S2, the key is only meaningful if `ForwardedHeaders:KnownProxies` lists the Caddy address.
Fix: give one-click its own policy with a higher limit (or partition by token hash as well), and verify the proxy configuration in the container run.

### S4 - Low - Token in the query string of the one-click URL can reach logs
Location: `SubscriptionLinks.OneClickUnsubscribeUrl`, Caddyfile, hosting logs.
Risk: the RFC requires a URL the provider can post to, so the token is in the query. Kestrel's default logging records the path only and the current Caddyfile has no `log` directive, so nothing logs it today. If access logging, HTTP logging middleware or an external proxy is added later, the token (which never expires) would be stored in logs. Impact is limited to unsubscribing that one person.
Fix: record in `Docs/architecture.md` that query strings of `/api/subscriptions/unsubscribe/one-click` must not be logged (or use a path segment and redact it); keep it out of any future access log format.

### S5 - Low - Tokens never expire
Location: `SubscriptionLinks` (`IDataProtector`, not `ITimeLimitedDataProtector`).
Risk: a forwarded or leaked email lets the holder unsubscribe that person at any time. This is the accepted behaviour for unsubscribe links (they must work for old mail) and the harm is only opt-out. No change needed; noted so it is a decision, not an oversight.
Fix: none required; keep the decision recorded.

### S6 - Informational - Client shows "no longer valid" for transient errors
Location: `unsubscribe-page.ts` error handler.
Risk: a 429, 500 or network failure gets the generic "no longer valid" message and the token is cleared and removed from the address bar, so the person cannot retry from that page (must reopen the email). Not a vulnerability; reduces the chance the opt-out completes.
Fix: on non-400 errors keep the token and show "Something went wrong, try again".

### S7 - Informational - Name stripping also removes joiners used by some scripts and emoji
Location: `SubscriptionService.CleanName` (Cf category includes ZWJ U+200D and ZWNJ U+200C).
Risk: none to security; names in Persian and some emoji sequences lose the joiner. Acceptable trade-off for a display name that appears in mail headers.
Fix: optional, allow U+200C and U+200D only between letters.

### S8 - Informational - Header names and the reserved header set are not constrained inside `CreateMessage`
Location: `SmtpMailTransport.CreateMessage`.
Risk: callers could pass `To`, `From` or `Content-Type` as extra headers. Names come only from code today (`SubscriptionLinks`), so there is no exposure.
Fix: if more senders start using `Headers`, add an allow-list (`List-Unsubscribe`, `List-Unsubscribe-Post`, `List-Id`, `Precedence`).

## Blocking summary

No high or critical findings. S1 should be handled (backup note and a guard) before real subscribers receive mail; S2 and S3 are cheap fixes for O3 or a follow-up.
