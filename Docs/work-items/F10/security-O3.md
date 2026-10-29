# Security review O3 (F10-US3: announce new stories and projects)

Reviewer: Security. Scope: commit 212b822 (announcement queue, `ContentAnnouncer`, `AnnouncementDispatcher` and hosted job, renderers, `PublicText`, `BrandedEmailLayout`, key-ring guard, one-click limits, migration `AddAnnouncements`, the two authoring services). Read-only static review; nothing was run.

**Verdict: pass** (no critical or high findings; three low and four informational items, none blocking).

## What was checked and held up

- Content injection, HTML: title, summary and name are HTML-encoded in the HTML view (`WebUtility.HtmlEncode`) and appear only as text; the summary is never a link or markup. `BrandedEmailLayout` encodes its own inputs and receives the card already encoded. No remote resources other than the site's own logo, no scripts, no tracking images.
- Content injection, headers: the subject is `Title` after `PublicText.Title` (hidden, control, Cf and line-separator characters removed, whitespace collapsed to one line, bounded to 200). The display name goes through `CleanName` (same stripping) and `MailAddress`. `List-Unsubscribe` values come from Data Protection output and `PublicBaseUrl`; `MailMessage.Headers` also refuses line breaks. No title, summary or slug reaches a header name or value other than Subject, which is single-line.
- Link spoofing: the public link is `PublicLinkBuilder.SiteLink("/stories/{slug}")`. The slug is produced by `Slug.From` (ASCII letters, digits and hyphens only), so it cannot carry `//host`, `..`, quotes or schemes. The base is `PublicBaseUrl` from configuration, never the request Host header, so a forged Host cannot redirect links. The link is built at send time from the stored path.
- `PublicText.Summary`: removes fences, turns image and link Markdown into their words, strips line markers and emphasis, then `OneLine`. Raw HTML in the Markdown is not interpreted; it is shown as encoded text. Truncation is surrogate-safe.
- Queue tables: `Announcements` holds only public facts (kind, content id, title, summary, path, timestamps). `AnnouncementDeliveries` holds ids, counters and timestamps; no name, address or token. No endpoint returns either table, and the publish responses return only the post or project result. Access rules require Administrator or system writes.
- Recipient isolation: one `RenderedMail` per recipient with a single `To` (`SmtpMailTransport.CreateMessage` adds one `To`, no Cc or Bcc, no shared header). Name and address are re-read per delivery from the live `Subscribers` row with `State == Confirmed`; an unsubscribed person gets nothing, and the cascading foreign key deletes their owed rows.
- Per-recipient unsubscribe: each mail carries its own sealed token (body link plus `List-Unsubscribe` and `List-Unsubscribe-Post`), built per `subscriberId`.
- Dispatcher: singleton with a `SemaphoreSlim`, so rounds never overlap in one process; each delivery runs in its own scope; `SentAtUtc` is saved right after the provider accepts; a unique index on (AnnouncementId, SubscriberId) prevents a second delivery row; failures back off linearly (`RetryDelayMinutes * Attempts`) and stop at `MaxAttempts` (5), so there is no retry storm. `DbUpdateConcurrencyException` (unsubscribe mid-send) is a skip. Exceptions are caught per recipient and per round and logged by type only. Cancellation ends the hosted service cleanly. Logs carry counts and kind only: no names, addresses, titles or tokens.
- Trigger surface for `IContentAnnouncer`: the only callers are the `PublishAsync` methods of `BlogAuthoringService` and `ProjectAuthoringService`, behind the administrator policy and antiforgery on the authoring controllers. No unauthenticated path reaches the announcer, and `AnnouncedAtUtc` is in no request contract. The marker is set once and never cleared. `ContentAnnouncer` also refuses a second announcement for the same (kind, content id), and a unique index enforces it in the database.
- Migration backfill: sets `AnnouncedAtUtc` for everything published, or published once, before the feature, so republishing old work cannot mail subscribers. Static SQL, no interpolation; `Down` reverses cleanly.
- Key-ring guard: logs one category-only warning when no usable key exists while subscribers exist; no personal data in the log.
- One-click endpoint: `RequestSizeLimit(1024)` plus `RequestFormLimits` (2 values, 64-character keys and values) closes O2 finding S2; its own policy (120 per window) is higher than the other public forms by design.

## Findings

### S1 - Low - A failed announce after the publish leaves the work marked announced and nobody told
Location: `BlogAuthoringService.PublishAsync`, `ProjectAuthoringService.PublishAsync`.
Risk: `AnnouncedAtUtc` is saved by the first `SaveChangesAsync`, and `AnnounceAsync` does its own second save. The comments say they share one unit of work; they are two saves. If the second throws, Gustavo gets an error, the work is published and marked announced, and publishing again will not announce it. This fails closed (no spam, no leak) but silently loses the announcement.
Fix: add the announcement and set the marker before a single `SaveChanges`, or correct the comment and surface a clear error.

### S2 - Low - Announcement send ignores cancellation
Location: `AnnouncementDispatcher.DeliverOneAsync` (`SendAnnouncementAsync` takes no token).
Risk: on shutdown the in-flight SMTP send runs to completion or to its own timeout, and a round is interrupted only between recipients. Not exploitable; it delays a graceful stop.
Fix: pass the token down to the mail service, or keep the SMTP timeout short.

### S3 - Low - Regexes in `PublicText` have no timeout
Location: `PublicText` generated regexes (`Image`, `Link` with `[^\]]*` and `[^)]*`).
Risk: a very long run of `[` with no closing bracket can cost quadratic time. The input is Gustavo's own excerpt or overview (administrator only), so the exposure is slowness inside his own publish request.
Fix: truncate the input to a few thousand characters before the regexes, or add a match timeout.

### I1 - Info - At-least-once delivery
A crash between provider acceptance and the save mails that one recipient again on the next round. Documented in the code; accepted.

### I2 - Info - Plain-text URLs and look-alike characters in content are not neutralized
`Summary` keeps the words of a Markdown link, so link text such as a URL becomes visible text that mail clients may auto-link; homoglyphs in a title are not filtered (only hidden and format characters are). The content is Gustavo's own, so this is not an attack path; the button and printed address always point at `PublicBaseUrl`.

### I3 - Info - `PublicBaseUrl` defaults to `https://localhost`
If the setting is missing, announcement links point at localhost instead of failing. No injection risk (the Host header is never used). Require `PublicBaseUrl` in the deployment checklist before announcements go to real subscribers.

### I4 - Info - Rate-limit partition depends on `ForwardedHeaders:KnownProxies`
The one-click limit partitions by `RemoteIpAddress`; behind Caddy without `KnownProxies`, all clients share one bucket, which could throttle legitimate provider posts. Existing deployment concern, unchanged by O3.

## Residual

Key-ring loss (O2 S1) is now logged at startup. Real SMTP or MailerSend delivery of announcements is not exercised here (known gap 3).
