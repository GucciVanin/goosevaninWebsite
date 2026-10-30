# TE11 Reviewer, round 3 (commit c5c1b93)

Verdict: **accurate** (the theory is plausible and consistent with the code; it is not proven, and the fix is sound).

## 1. Is the theory right, and can it happen in production or in the container?

What the code shows:
- `ContentAnnouncer.AnnounceAsync` builds one `Announcement` with its `Deliveries` list and saves the whole graph with one `SaveChangesAsync` (lines 40-52). The announcement and its delivery rows are never written separately, and the save runs inside the publish request's transaction, which `UnitOfWorkFilter` commits when the action ends (`UnitOfWork.CompleteAsync`).
- `AnnouncementDispatcher.CloseFinishedAsync` (lines 152-173) closes any announcement with `CompletedAtUtc == null` and no owed delivery. An announcement with zero delivery rows therefore closes at once. That is correct for the no-recipient case (the announcer sets `CompletedAtUtc` itself then), but it means a loop round that sees the announcement row without its delivery rows would close it and nothing would ever be sent.
- Test factory (`AuthApiFactory.cs:26,79`): one `SqliteConnection("Data Source=:memory:")` is shared by every `ApplicationDbContext`. The hosted loop's scopes therefore run on the same connection as the request, and inside its open transaction, so they can read the request's uncommitted rows. The window is between the announcement insert and the commit; with the loop at `PollSeconds = 1` a round can land in it. Within a single SaveChanges the rows are one statement batch, so the window is real only if the round interleaves between statements, or sees the announcement before the delivery inserts. I cannot rule that interleaving in or out by reading; this is the unreproduced part.
- Production and the container (`PersistenceServiceCollectionExtensions.cs:25`): `UseSqlite(connectionString)` with a file path, so each context gets its own pooled connection. There is no `Cache=Shared`, no read-uncommitted pragma and no WAL setting anywhere in `src`, `docker-compose.yml` or `Dockerfile`, so SQLite uses the default rollback journal. A second connection reads only committed data; if it meets the writer's commit it gets SQLITE_BUSY or waits, not a partial view. So the announcement row cannot be visible before its delivery rows, and `CloseFinishedAsync` cannot close it early. I found no production race of this kind.
- The failing run was in `verify.ps1`'s dotnet-tests stage, which uses the test factory, not the container. That fits the theory: the container never uses the shared-connection setup.

What I cannot establish: that this was the actual cause of the one failure (no reproduction, no log). An alternative that fits the evidence equally well is the old wall-clock poll (100 x 100 ms = 10 s) timing out on a loaded machine during the full verification run. The fix covers both causes (see 2), so the distinction does not change the verdict.

Residual production note, not a bug: any future code path that creates an `Announcement` first and adds its `Deliveries` in a later save, or in a different transaction, would hit the zero-rows-closes-early behaviour. Today there is none. A defensive guard (do not close announcements younger than one round, or mark zero-recipient announcements explicitly) is optional and Gustavo's decision.

## 2. Does the fix weaken the test?

No.
- The loop still does the delivery: the gate wraps the real `AnnouncementDispatcher` (registered as a singleton and resolved by the hosted `AnnouncementDeliveryService` through `IAnnouncementDispatcher`), which calls `inner.DeliverPendingAsync` unchanged once the gate opens. The hosted service is still the caller, with `PollSeconds = 1`.
- The editor request still does not wait: `PublishStoryAsync` completes before `publishDone.SetResult()`, so the request returns while the loop is held, and the mail is only sent afterwards. The "does not wait" claim is in fact asserted more strongly than before (no mail can have been sent when the request returned), although the test has no explicit assertion that `Rendered.Count == 0` at that moment. Advisory below.
- The gate cannot hide a real bug of the loop: it only delays the start of a round. Only the "see uncommitted rows" artefact of the shared connection is removed. A genuine bug in dispatch, closing, or the mail service would still fail the final `Assert.Single`.
- A hang cannot go unnoticed: the 60 s `WaitAsync` throws, and a failed publish leaves the gate closed, so the test fails at the publish call.
- `OnRenderedSend` runs inside the mail service on the loop's thread, so `sendStarted` is a real signal. The extra 600 x 100 ms loop waits for `Rendered` to be recorded after the call started. It is generous and harmless.

## 3. Production code unchanged

Confirmed. `git show --stat c5c1b93` lists only `tests/GooseWebsite.Api.Tests/AnnouncementsApiTests.cs` (35 insertions, 2 deletions). The working tree is clean.

## Advisories (none blocking)

1. Optional: add `Assert.Empty(site.Mail.Rendered)` immediately after `PublishStoryAsync` and before `publishDone.SetResult()` to make "the request did not wait for delivery" an explicit assertion. This is meaningful only because the gate guarantees it, so keep it only if you want the intent written down.
2. The comment says a round "can see its uncommitted rows". It is accurate as a hypothesis; consider saying "most likely" because the failure was not reproduced.
3. The gated dispatcher is registered for this test only. Other tests still use the plain singleton, with the loop asleep (`PollSeconds = 3600`), so they are unaffected.
4. Re-run the full dotnet test stage several times (for example 10 consecutive runs under load) to give some evidence that the flake is gone, since no reproduction exists.
