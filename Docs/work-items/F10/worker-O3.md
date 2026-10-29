# Worker report - O3 (F10-US3, notify subscribers on first publish, plus the O3 addendum)

Verdict: **complete**. Nothing was committed or staged. F10-US3 is In Progress in the backlog (tasks ticked, evidence paragraph added); it is not Done.

## Design choice (dependency rule, architecture 3.1)

Blog and Projects must tell the subscribers' module that something was published, but neither may reference Subscriptions. The seam is owned by **Shared**: `Shared/Announcements/IContentAnnouncer.AnnounceAsync(ContentAnnouncement)` (the record carries only public facts: kind, content id, title, summary text, public path). Blog and Projects call it; Subscriptions implements it (`ContentAnnouncer`) and registers it with `services.Replace`, while Blog and Projects register a quiet `NoContentAnnouncer` with `TryAdd` (`AddContentAnnouncerFallback`), so module order does not matter and the site still publishes if Subscriptions is removed. This is the same pattern as `IMailService`. Rejected: a direct Blog-to-Subscriptions call (wrong-way dependency) and an event bus (more machinery than one interface needs). A test (`BlogAndProjectsDoNotReferenceTheSubscriptionsModule_TheyOnlyCallTheSharedAnnouncer`) scans the Blog and Projects sources for any reference to `Modules.Subscriptions`.

## What changed

Backend (`src/GooseWebsite.Api`):
- `Shared/Announcements/ContentAnnouncement.cs` (record, interface, `NoContentAnnouncer`, `AddContentAnnouncerFallback`).
- `AnnouncedAtUtc` (nullable) on `BlogPost` and `Project`; migration `20261029194550_AddAnnouncements` (adds the two columns, backfills them with `COALESCE(PublishedAtUtc, CreatedAtUtc)` for everything published now or ever, creates the two new tables). `BlogAuthoringService.PublishAsync` and `ProjectAuthoringService.PublishAsync` set the marker on the first transition only and call the announcer right after their own save, in the same request/unit of work. Nothing else (create, update, unpublish, repeated publish) touches it. Not in any response contract.
- `Modules/Subscriptions`:
  - `Domain/Announcement.cs` (`Announcement`, `AnnouncementDelivery`) and `AnnouncementConfiguration.cs` (access rules `WriteAccess.Administrator`, table mappings, unique `(Kind, ContentId)`, unique `(AnnouncementId, SubscriberId)`, cascade FK to the announcement and to `Subscribers`, so unsubscribing removes the owed mail). Columns: ids, counters, timestamps, the public title/summary/path. No name, no address.
  - `Services/ContentAnnouncer.cs`: queues one `Announcement` plus one delivery row per confirmed subscriber at that moment; sends nothing; idempotent per content item.
  - `Services/AnnouncementDispatcher.cs` (`IAnnouncementDispatcher.DeliverPendingAsync`, returns `AnnouncementRound(Sent, Failed)`) and `AnnouncementDeliveryService.cs` (hosted loop, `PollSeconds`). Each recipient runs in its own scope, the result is saved before the next recipient, a failure waits `RetryDelayMinutes` x attempts and stops after `MaxAttempts`, an announcement closes when nobody is left to try, a recipient who unsubscribed meanwhile is skipped, logs carry counts only.
  - `Services/PublicText.cs`: the summary/title safety (Markdown markers, links and images reduced to words; control, Zl, Zp, Cf removed; one line; ellipsis at the configured length). `SubscriptionService.CleanName` now uses its `StripHidden`.
  - `Services/BrandedEmailLayout.cs` (frame extracted from the confirmation mail, which now uses it; same markup), `AnnouncementEmailRenderer.cs`, `SubscriptionEmailSender.SendAnnouncementAsync` (title, summary, button and plain public link, personal unsubscribe link, `List-Unsubscribe` headers from `ISubscriptionLinks`).
  - `Services/DataProtectionKeyRingGuard.cs` (`IModuleInitializer`, addendum a).
  - `Models/SubscriptionOptions.cs`: `Announcements` (`PollSeconds` 20, `MaxAttempts` 5, `RetryDelayMinutes` 10, `SummaryMaxLength` 300) and `RateLimit:OneClickPermitLimit` (120).
  - `SubscriptionsModule.cs`: registrations, the `subscriptions-one-click` policy. `SubscriptionsController.UnsubscribeOneClick`: `[RequestSizeLimit(1024)]`, `[RequestFormLimits(ValueCountLimit = 2, KeyLengthLimit = 64, ValueLengthLimit = 64, MultipartBodyLengthLimit = 1024)]`, own policy.
- Client: `features/subscriptions/unsubscribe-page.ts` keeps the token and shows a retry notice with the button for 429, 5xx and network errors; only 400 is final (addendum e).

Tests/verification: `AnnouncementsApiTests.cs` (new, 22 tests), additions to `UnsubscribeApiTests.cs` (8 tests, one test renamed), `TestMailService.FailRecipients`, `unsubscribe-page.spec.ts` (+4), `verify/runtime/70-blog.sh` and `75-projects.sh` (publish response carries no subscriber/announcement data; republish after unpublish succeeds). `Dockerfile.verify` now also copies `Caddyfile` and `Docs/architecture.md` into the verify image (see deviations).

Docs: `Docs/architecture.md` (3.1 dependency note, 3.2 Shared row, 3.6 and 3.6a publish/marker, 3.6c new rows and settings and one-click limits, section 4, new flow 5.5, config table, 7.3 key ring backup/never revoke and never-log note, testing, latest result, repo layout), `Docs/product-backlog.md` (F10-US3 In Progress, tasks ticked, evidence; old test name updated), `Docs/tests/README.md` (new section).

## Acceptance criteria and addendum, mapped

F10-US3 AC1 to AC6 and F10-US2 AC4: see the evidence paragraph in the backlog and the table in `Docs/tests/README.md`. Addendum: (a) architecture 7.3 text plus `DataProtectionKeyRingGuard` and two tests; (b) attributes and two tests (attribute shape, oversized value and too many fields answered 4xx and nothing deleted, the valid request still works); (c) own policy, defaults higher than confirm, test shows separate buckets; (d) architecture note plus a test over the host log, `Caddyfile` (no `log` directive), `Program.cs` (no `UseHttpLogging`) and the doc sentence; (e) four Karma tests; (f) test renamed to `TheUnsubscribeLinkHasNoExpiryOfItsOwn_ItOnlyStopsWorkingWhenTheSubscriberIsDeleted`, purpose-isolation test added (a rotated-key test was not added: rename chosen).

## Deviations and decisions to review

1. **Backfill is wider than "published"**: the migration also marks work that was published once and is a draft now (it has a `PublishedAtUtc`). Reason: republishing it would otherwise mail subscribers about an old post.
2. **Recipients are fixed at publish time** (confirmed subscribers then). Someone who confirms later is not told about earlier work; an unsubscribed person's delivery row is removed by the database cascade.
3. **Delivery is at least once.** If the process dies after a provider accepts a mail and before the row is saved, that recipient is mailed again on restart. Duplicates are limited to that window.
4. **Atomic publish plus queue.** The announcer saves inside the publish request's unit of work, so a failure to queue (database error) rolls the publish back instead of losing the announcement; a mail-provider failure can never fail the publish because no mail is sent in the request.
5. **`Dockerfile.verify` changed** (copies `Caddyfile` and `Docs/architecture.md`): my first full verify run failed one test, `TheOneClickQueryStringIsNeverLogged...`, because the verify image holds only `src` and `tests`. It is a test-infrastructure change outside the strict order scope; the alternative was dropping the Caddyfile/doc assertions.
6. **`BrandedEmailLayout` refactor** touches the O1 confirmation mail (the markup is the same; the existing confirmation tests still pass). Done to avoid a 60-line duplicate frame.
7. **`AnnouncementConfiguration.cs`** holds the two access rules and the two mappings in one file (four small types); split if you prefer one file per type.
8. **Summary rules**: Markdown is reduced by a small regex set in `PublicText`, not a Markdown parser (none is referenced by the API). It never produces a link or markup, and the HTML view encodes it, but exotic Markdown (nested brackets, reference links) may leave a few raw characters in the text.
9. **Editor UI is untouched** (no "subscribers will be emailed" notice), to keep the browser stage unaffected.

## Doubts and open points

- **No real delivery was exercised.** The container stack never holds a subscriber (a valid subscription would email a real address), so the runtime checks prove that a publish with zero subscribers succeeds and returns no subscriber data; the mail itself, retries and privacy are proven by xUnit with `TestMailService`. Real provider delivery and the announcement mail in real clients remain open gaps (project gaps 2 and 3).
- **Size limit under TestServer**: `[RequestSizeLimit(1024)]` is checked as an attribute; the in-memory TestServer does not enforce Kestrel's body limit, so the behavioural test relies on the form limits (4xx). The body-size enforcement itself rests on Kestrel.
- **Key ring guard** only sees "no usable key at startup while subscribers exist"; it cannot tell a key ring replaced by a different one. It runs before the ring creates a fresh key, so it fires on the first start after a loss, not on later ones (the new key then exists). The log line is the only signal.
- **Retention**: `Announcements` and `AnnouncementDeliveries` rows are never pruned (ids, counters and public text only, a few rows per post). Say if a retention job is wanted.
- **Single instance**: the delivery loop assumes one app instance (a `SemaphoreSlim` serialises rounds in-process only).
- Timing side channel (O1 S1) unchanged. The new `OneClickPermitLimit` of 120 per 10 minutes per client address is my choice; adjust if wanted.
- Windows note: the Bash tool failed on heredocs containing apostrophes; scripts were written to the scratchpad. New files created with the Write tool are CRLF, like several existing ones (the repo is mixed).

## Test totals (final)

- Karma: 234 of 234 pass (230 + 4 new in `unsubscribe-page.spec.ts`). One earlier run had 3 failures from my own spec (URL asserted before the router finished), fixed.
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 268 passed, 0 failed (238 + 30 new).
- `./scripts/verify.ps1 -SkipCi`: **1011 passed, 0 failed, 0 skipped, 0 known** (unit-dotnet 268, unit-angular 234, runtime, browser 321 of 321 listed tests on Chromium, Firefox and WebKit); `verify/browser/known-issues.json` still empty. It was run twice: the first run had 1010 passed and 1 failed (the Caddyfile test, deviation 5); the totals above are from the clean second run.

## Process notes

- TDD: `AnnouncementsApiTests`, the addendum tests and the Karma specs were written before the production code (they did not compile / failed first); several expectations were corrected afterwards (clock advanced a day so the admin cookie expired; a culture-sensitive `Assert.DoesNotContain` on invisible characters, now `Contains(char)`; the seed SQL columns).
- Docker: only `goosewebsite-caddy`, `goosewebsite-web` and the `goosewebsite-verify` run container (through `verify.ps1`) were touched; at the end I ran `docker stop goosewebsite-caddy goosewebsite-web`. No other container or process.
- No git write commands, no secrets, only `*.example.test` addresses, no real email sent (test mail service; the stack has no subscribers). Names, addresses and tokens are not logged (tests over the whole host log).
