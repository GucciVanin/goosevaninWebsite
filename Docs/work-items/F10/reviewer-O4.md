# Reviewer - O4 (atomic announcement and small hardening)

Verdict: **accurate**

Scope: `git show HEAD` against order O4 and `worker-O4.md`. Read-only; no Docker, no verify.ps1. I ran `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` myself: 279 passed, 0 failed, 0 skipped (matches the worker's 268 + 11).

## Do the tests prove the claims

- **Atomicity (stories and projects, throw before queuing and after the rows were saved).** Yes. `FaultyAnnouncer` wraps the real `ContentAnnouncer` and throws in either position. The tests assert a 5xx, the item unpublished with null `PublishedAtUtc` and null `AnnouncedAtUtc`, empty `Announcements` and `AnnouncementDeliveries`, and (stories) a 404 on the public page. Then the fault is cleared and a second publish works and mails once. The `AfterQueueSaved` case is the one that matters: it proves the second `SaveChanges` rolls back with the first. The mechanism checks out in code: `DataSafeguardsInterceptor` opens the transaction on the first save in a request and `UnitOfWorkFilter` rolls back on exception. The worker's conclusion (Reviewer O3 right, Security O3 S1 wrong in effect, no production change) is correct.
- **Provider failure never fails a publish.** Yes, and structurally: nothing is sent in the request. `AMailProviderThatFailsAfterThePublishNeverUndoesItNorTheMarker` shows 200, marker set, one failed attempt recorded. It is a weaker test than it looks (the mail service is not called during the request, so it would pass with any provider behavior), but the claim is true and the architecture text says why.
- **Cancellation ends promptly without counting an attempt.** Mostly. `ARoundCancelledDuringASendEndsPromptly...` cancels inside the mail service, which then throws; it asserts `OperationCanceledException`, under 10 s, `Attempts == 0`, `SentAtUtc` null, and that the next round sends. That proves the throwing path. It does not exercise the new `if (!accepted) ThrowIfCancellationRequested()` branch, which is the path the real `MailService` takes (it catches every exception, cancellation included, and returns a failed result). That branch is correct by reading but untested (advisory 1). `TheCancellationTokenOfARoundReachesTheMailService` is a real check (`Assert.Equal(source.Token, LastRenderedToken)` fails without the plumbing).
- **Regex timeouts and the 4000-character cap leave normal output unchanged.** Only by the existing tests. The hostile and normal summary tests still pass, and the changed code differs only for inputs over 4000 characters or on a timeout. No test covers normal text near or over the cap, or the timeout fallback itself (unreachable on this engine). The worker is candid that the 200 000-bracket test also passed before the change, so it is a regression guard, not proof of the old problem (advisory 2).
- **Concurrent first publish: one announcement, one mail.** The tests use a real file database and two connections and assert no 5xx, one `Announcement`, one mail. The code supports the result: both requests read the item before either saves, the loser's first save waits for the SQLite write lock, and once it has the lock `ContentAnnouncer`'s `AnyAsync(Kind, ContentId)` check (backed by the unique index) finds the winner's row and returns. The worker says honestly that overlap is not guaranteed, so the test can pass by plain serialisation (advisory 3). The architecture sentence "serialised by the database write lock" is true for SQLite only; the worker lists that as a doubt.

## Are the corrected comments and docs true

Yes. The comments in `BlogAuthoringService`, `ProjectAuthoringService` and `ContentAnnouncer`, architecture 3.6c, the announcements and delivery rows, the 5.5 diagram note ("first save, opens the transaction", rollback on exception), the test paragraph, the latest-result line (1022; .NET 279) and the backlog evidence match the code and the tests. F10-US3 correctly stays In Progress.

## Regression check

- `SendAnnouncementAsync` gained `CancellationToken cancellationToken = default`: source compatible. `SubscriptionEmailSender` forwards it to `IMailService.SendAsync`.
- Dispatcher: a send the provider accepted is always saved, because `SaveChangesAsync()` takes no token and runs after acceptance, so a shutdown right after acceptance does not lose or repeat that delivery. Cancellation during a send leaves state untouched and the next round retries it. `AnnouncementDeliveryService` already swallows `OperationCanceledException` at shutdown. The `catch ... when (exception is not OperationCanceledException)` filter is unchanged; an `OperationCanceledException` not caused by shutdown would abort the round, but `MailService` converts transport exceptions to failures, so it cannot reach there today.
- `TestMailService.SendAsync` became async; behavior is unchanged for the other tests. Full suite green.

## Advisories (none blocks)

1. Add a test with a mail service that returns a failed result after the token is cancelled, to cover `if (!accepted) ThrowIfCancellationRequested()`.
2. `PublicText.Summary` cuts at 4000 characters before the Markdown rules, so a link or image straddling the cut leaves its raw `[words](partial-url` as text, and a cut text whose cleaned length is under the limit gets no ellipsis. Cosmetic; normal summaries are far below 4000. The comment mentions leftover markers for the timeout but not for the cap.
3. `Spaces()` in `OneLine` also got a 200 ms timeout but sits outside the `try/catch`. The pattern is linear and titles are capped at 200 characters, so it cannot realistically fire, but if it did the publish would fail with a 500 (and roll back). Leave it or drop that timeout.
4. The Karma count difference (worker direct run 201 vs verify 234) is unexplained and no client code changed. The captain already routed it to Testing.
