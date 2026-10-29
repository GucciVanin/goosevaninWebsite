# Reviewer - O3 (F10-US3 and the O3 addendum a-f)

Verdict: **accurate**

Scope: `git show HEAD` against the O3 order, the O3 addendum (a)-(f), F10-US3 AC1-AC6, F10-US2 AC4, and `worker-O3.md`. Read-only; no Docker, no verify.ps1. I ran `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` myself: 268 passed, 0 failed, 0 skipped (matches the worker's total).

## Acceptance criteria and addendum, each against a test that fails if the behavior breaks

- AC1 (first publish, confirmed only, one mail each): `TheFirstPublishOfAStoryMailsEachConfirmedSubscriberOnce_AndNoPendingOne`, the project variant, and `EveryMailIsPersonal_...`. Recipients are `State == Confirmed` in `ContentAnnouncer`; a pending recipient or a duplicate would fail the test.
- AC2 (edit, republish, unpublish): `EditingRepublishingAndPublishingAgainNeverMailAgain_AndTheMarkerIsSetOnce` and the project test. The marker `AnnouncedAtUtc` is set once in `PublishAsync`, and `ContentAnnouncer` also has a unique `(Kind, ContentId)` guard. Create is always a draft, so "create as published" does not exist and needs no path.
- AC3 (draft): `SavingDraftsAndEditingThemSendsNothingAndQueuesNothing`. A refused publish announces nothing and the later publish does (`APublishThatIsRefused...`).
- AC4 (provider failure, retry without duplicates): `PublishingReturnsBeforeAnyMailIsSent_...`, `APartialFailureIsRetriedLater...` (asserts the delivered set equals each address exactly once after a retry), `ARecipientThatKeepsFailing...`, and the hosted-loop test. The dispatcher records `SentAtUtc` per recipient before the next, so a retry skips those reached.
- AC5 (privacy): `NeitherThePublishResponseNorTheLogsNorTheQueueTablesHold...` and `TheQueueKeepsOnlyIdsAndCounters`. The `Announcements` and `AnnouncementDeliveries` columns hold ids, counters and public text only. Logs use counts and exception type names.
- AC6 (none or only pending): `APublishWithNoSubscribersSendsNothingAndFailsNothing`, `OnlyPendingSubscribersMeansNothingIsSent`.
- F10-US2 AC4 (unsubscribed skipped): `ASubscriberWhoUnsubscribedBeforeTheMailWasSentGetsNothing`. There are two protections: the FK cascade removes the delivery row, and the dispatcher re-reads the subscriber as Confirmed. Also `ASubscriberWhoConfirmsAfterThePublish...` documents the late-confirmer rule.
- Addendum (a): `DataProtectionKeyRingGuard` plus two tests, and the architecture 7.3 text on backup and never revoking. (b): `TheOneClickActionHasATinyRequestAndFormLimit` and `AnOversizedOneClickFormIsRefusedAndChangesNothing`. (c): `TheOneClickAddressHasItsOwnHigherRateLimitThanConfirm` and the defaults test. (d): the log test plus the architecture note. (e): Karma specs for 429, 5xx and network (token kept, button shown) and for 400 (final). (f): the test was renamed and `ATokenProtectedUnderAnotherPurposeIsRejected` was added. All present.

## Other checks

- Backfill: the migration marks `COALESCE(PublishedAtUtc, CreatedAtUtc)` where `IsPublished = 1 OR PublishedAtUtc IS NOT NULL`. It is safe on existing data (adds a nullable column, then an UPDATE) and it is the safe direction (never mails about old work). `ContentPublishedBeforeSubscriptionsExistedIsMarkedAsAnnounced_...` runs the real migration on a pre-migration database with seeded rows: live, pulled-back and never-published stories, and live and draft projects. A live project with a NULL `PublishedAtUtc` falls back to `CreatedAtUtc`, which is correct. Worker deviation 1 (the wider backfill) is sound.
- Dependency rule: grep shows no `Subscriptions` reference in `Modules/Blog` or `Modules/Projects`. They use `Shared/Announcements/IContentAnnouncer`. The `NoContentAnnouncer` fallback uses `TryAdd` and the Subscriptions module uses `Replace`. A test scans the sources, and another covers the quiet fallback.
- Atomicity: `PublishAsync` saves the item and then the announcer saves the queue. Both run inside the request unit of work (`UnitOfWorkFilter`: the first save opens a transaction, an exception rolls back, architecture line 350). So a failed queue write rolls back the publish, and a mail-provider failure cannot affect it because no mail is sent in the request. The design is correct, but see advisory 1.
- Docs vs code: architecture (3.1, 3.2, 3.6c, 5.5, 7.3, config), the backlog evidence paragraph and `Docs/tests/README.md` match the code (table names, columns, settings, test names). F10-US3 stays In Progress, not Done. The backlog evidence names real tests.
- CLAUDE.md rules: all DB access goes through `DatabaseService` (the dispatcher uses `Read`/`ReadForUpdate` in its own scopes), the access rules are declared next to the entities, and the migration is present. No secrets. No message bodies, addresses or tokens in logs. One commit for the story, with the attribution line.

## Mismatches

None found at blocker, major or minor severity.

## Advisories

1. (Low, `AnnouncementsApiTests.cs`) Atomicity is asserted only in comments and docs. No test makes the announcer throw and then checks that the story is still a draft with `AnnouncedAtUtc` null (rollback). A regression that swallowed the exception or saved outside the unit of work would pass the suite. Consider adding one test with a throwing `IContentAnnouncer`.
2. (Low) Two simultaneous first-publish clicks on one item would both pass the null-marker check. The unique `(Kind, ContentId)` index makes the second request fail and roll back (500 for the loser), so there is no duplicate mail. Acceptable, and not tested.
3. (Info) Delivery is at least once, and the single-instance assumption (`SemaphoreSlim`) is documented by the worker. `Announcements` rows are never pruned. The key ring guard fires only on the first start after a loss. The real provider and real mail clients remain unverified (project gaps 2 and 3); the runtime checks prove only a zero-subscriber publish.
4. (Info) `Dockerfile.verify` now copies `Caddyfile` and `Docs/architecture.md`, which is outside the strict order scope, so the log-privacy test can read them. It is reasonable and disclosed. The assertion on a doc sentence is slightly brittle.
5. (Info) The migration files trigger IDE0161 warnings, as the earlier migrations do. This is not new.
