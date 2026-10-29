# Security - O4 (atomic announcement and small hardening)

Verdict: **pass**

Scope: `git show HEAD`, the dispatcher, `PublicText`, `SubscriptionEmailSender`, `ContentAnnouncer`, `DataSafeguardsInterceptor`. Read-only. `dotnet test` run by me: 279 passed, 0 failed.

## Findings

No blocking finding. Security O3 S1-S3 are resolved or correctly reassessed.

- **S1 (marker saved without queue).** Not a real exposure. The marker and the queue rows are two saves inside one request transaction (opened by `DataSafeguardsInterceptor` on the first save, rolled back by the unit of work on any exception, and also when a save fails and the caller swallows it, via `UnitOfWorkFailed`). Tests with a throwing announcer show a draft, unmarked, unqueued, retriable item, so a subscriber can never be silently skipped by a half-done publish. Comments and docs now say this accurately.
- **Provider failure vs. publish.** No mail is sent in the request, so a provider outage cannot fail or roll back a publish.
- **Concurrent first publish.** Duplicate mail is prevented twice: the `AnyAsync(Kind, ContentId)` guard in `ContentAnnouncer` (run after the loser acquires the write lock) and the unique `(Kind, ContentId)` index, whose violation would roll back the loser's whole click instead of creating a second queue. Residual: on a database without SQLite's single writer the loser could get a 500 instead of a quiet 200 (the worker lists this as a doubt). It cannot duplicate mail.
- **Cancellation.** No delivery is lost or doubled. A cancelled send changes no row (attempt not counted, `SentAtUtc` not set), so the next round retries it. A send the provider already accepted is saved without the token, so shutdown cannot lose that record and cause a re-send. No new log fields: counts and exception type names only.
- **PublicText hardening.** The 4000-character cap and the 200 ms timeouts reduce regex denial-of-service exposure (the input is Gustavo's own text, so the risk was low already). The timeout fallback keeps the text as it is, but it still goes through `OneLine` (control, line-separator and format characters removed, one line) and `Shorten`, and the HTML view HTML-encodes everything, so raw Markdown or HTML in the fallback appears only as inert escaped text: no link, markup or header injection. A raw `[x](url)` left in the plain-text part could show a URL that a mail client auto-links; the author is the site owner and a bare URL already survives today, so this is informational. The cap is a prefix cut with a surrogate guard and cannot produce an invalid string.
- **Privacy.** No new log fields; no names, addresses, titles or tokens. Test helpers add nothing to production paths.

## Informational

1. `Spaces()` (used by `OneLine` for titles and summaries) now has a 200 ms timeout but is outside the `try/catch`; a `RegexMatchTimeoutException` there would fail and roll back a publish. The pattern is linear and the title is capped at 200 characters, so it is not reachable in practice.
2. The timeout fallback and the cap boundary have no direct test (the fallback is unreachable on this engine).
