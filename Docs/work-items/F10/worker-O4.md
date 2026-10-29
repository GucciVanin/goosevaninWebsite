# Worker report - O4 (atomic announcement and small hardening)

Verdict: **complete**. Nothing committed or staged. F10-US3 stays In Progress.

## What the test showed about the atomicity conflict

Reviewer O3 was right, Security O3 S1 was wrong about the effect. The marker save and the announcer's queue save are two `SaveChanges` calls, but both run inside the request unit of work (`UnitOfWorkFilter`), which commits only when the action ends and rolls back on any exception. New tests with an announcer that throws before queuing, and after its rows were saved, show: the request fails (500), the story/project stays a draft, `AnnouncedAtUtc` and `PublishedAtUtc` are null, no announcement or delivery rows exist, and publishing again works and mails once. These passed on first run, so no production change was needed for atomicity; only the misleading comments and docs were corrected. A failing mail provider never fails the publish (no mail is sent in the request; test added).

## Changes

- Cancellation: `ISubscriptionEmailSender.SendAnnouncementAsync` and `SubscriptionEmailSender` take a `CancellationToken` passed to `IMailService`; the dispatcher passes its token and rethrows cancellation instead of recording a failed attempt (MailService turns exceptions into failure results). Tests: token reaches the mail service; a cancelled send ends promptly, counts no attempt, and the next round sends.
- `PublicText`: Markdown rules see at most 4000 characters, each regex has a 200 ms timeout, a timeout keeps the text as is. Test with 200 000 brackets (it passed before the change too; this machine's regex engine did not show slowness, so the test is a regression guard, not proof of the old problem).
- Concurrency: two simultaneous first publishes. The shared in-memory test connection cannot hold two transactions (gave a 500 from SQLite itself, a harness artifact), so the tests use a temporary file database with separate connections: the second request waits for the write lock, sees the first announcement, queues nothing; no 500, one announcement, one mail. No production change. It passed repeatedly (3 runs); true overlap timing is not guaranteed by the test.
- Comments in `BlogAuthoringService`, `ProjectAuthoringService`, `ContentAnnouncer`; `Docs/architecture.md` (3.6c, delivery row, 5.5 diagram and text, tests, latest result); backlog evidence.
- Test helpers: `TestMailService` gained `OnRenderedSend` and `LastRenderedToken`; `Site.StartAsync` takes `configureServices`.

## Totals

- dotnet test: 279 passed, 0 failed (268 + 11).
- Karma via cmd /c: 201 of 201 in my direct run, but `verify.ps1` reports unit-angular 234 as before; I did not investigate the difference (no client code changed).
- `./scripts/verify.ps1 -SkipCi`: 1022 passed, 0 failed, 0 skipped, 0 known.

## Doubts

- The loser of a race relies on SQLite's single-writer lock; a different database would need an explicit conditional update.
- Docker: only goosewebsite-caddy and goosewebsite-web (plus verify's own run container) touched; both stopped by name at the end.
