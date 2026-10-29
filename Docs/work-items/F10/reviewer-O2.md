# Reviewer report - O2 (F10-US2 and the O2 addendum)

Verdict: **accurate**

Basis: `git show HEAD` (868a4f7), `orders.md` (O2 and addendum), F10-US2 in the backlog, `worker-O2.md`, `decisions.md`. I ran `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 238 passed, 0 failed (matches the worker's count). Nothing else was run (no Docker, no verify.ps1), so the container evidence for S2 and the Karma/browser totals are taken from the worker report, not re-checked.

## Acceptance criteria

| AC | Test that fails if the behavior breaks | Result |
| --- | --- | --- |
| AC1 valid link, confirm, record deleted, page confirms | `Unsubscribe_DeletesTheConfirmedSubscriberAndOnlyThatOne`, `Unsubscribe_AlsoRemovesAPendingSubscriber` (store asserted empty or only the other subscriber); Karma `unsubscribe-page.spec.ts` and browser `unsubscribe:` tests for the page | Met |
| AC2 invalid or used link neutral, nothing changes | `Unsubscribe_RefusesUsed...` (used, tampered, unknown, empty, missing, over-long: identical 400 and body, no "exist", store unchanged); `AnOldUnsubscribeLinkNeverRemovesALaterSubscription...` | Met |
| AC2 mail scanners (GET changes nothing) | `UnsubscribingNeedsAPostWithTheCsrfToken_AndFetchingTheLinksChangesNothing` (GET page, API and one-click, POST without CSRF refused, store still has 1); Karma asserts no request until the button is pressed | Met. The controller has no GET action for these routes. |
| AC3 headers and body carry the link | `EverySubscriptionMailCarries...` (link in text and HTML, `List-Unsubscribe` format and decodable token, `List-Unsubscribe-Post`), `TheSmtpTransportWritesTheCustomHeadersIntoTheMessage` (real MIME message), one-click endpoint test | Met. Only the confirmation mail exists today; O3 must reuse `ISubscriptionLinks` for its mails. |
| AC4 deleted subscriber gets nothing | Structural (record gone); worker says exercised in O3 | Not testable until O3; honestly stated in the backlog. Story correctly stays In Progress. |

## Addendum

- (a) S4: `TheNameLosesLineSeparatorsParagraphSeparatorsAndFormatCharacters` covers Zl, Zp, Cf (BMP and non-BMP tag character via `EnumerateRunes`), asserts both stored name and mail addressee. Met.
- (b) `PublicBaseUrl`: was already set in compose; documented in `.env.example` and `docker-compose.yml`. Met.
- (c) S2: evidence exists only as a manual run recorded in `worker-O2.md` (20 x 400 then 429 from the host while the verify container was also exhausted, spoofed `X-Forwarded-For` ignored). Plausible and consistent with `KnownProxies` 172.30.100.2. No automated test of the two-address comparison (stated by the worker). Accepted as recorded evidence; I did not reproduce it.
- (d) Column dropped: entity, configuration, snapshot, migration Up/Down, `Subscriber.cs` comment, the three O1 tests, and the column-list assertion in `SubscriptionsApiTests` are consistent. The migration drops the index before the column and Down restores both. The `AddSubscribers` migration uses SQLite Autoincrement, so ids are never reused, which the "old link cannot hit a later subscriber" reasoning depends on. Met.

## Data Protection token deviation (decisions.md row 9)

- Nothing stored: confirmed. `TheUnsubscribeLinkIsNotStored...` scans every column of every row for both tokens. The sole schema is `Id, Name, Email, State, CreatedAtUtc, ConfirmedAtUtc, ConfirmationTokenHash, ConfirmationTokenExpiresAtUtc`.
- Tamper-proof: holds by design (authenticated encryption; `CryptographicException` caught, then false). Test covers a modified token, a foreign key ring (`TheSharedLinkServiceReadsItsOwnTokens...`), and garbage.
- Valid until deletion: holds by design (Data Protection keeps expired keys for decryption unless revoked, key ring persisted at `/data/keys`). The test `TheUnsubscribeLinkStaysValidUntilTheSubscriberIsDeleted` advances only the app `TimeProvider`, but Data Protection reads the system clock, so the test does not actually exercise key expiry or rotation (see advisory 1). Several tokens issued at different times staying valid is genuinely covered.
- Purpose isolation: holds by construction (`CreateProtector("GooseWebsite.Subscriptions.Unsubscribe.v1")`; account tokens use other providers/purposes), but there is no test that a token protected under another purpose is rejected (advisory 2).
- The deviation from "HMAC" is disclosed, accepted in the decisions log, and the backlog and architecture describe the real design.

## GET changes nothing, privacy, CLAUDE.md

- GETs change nothing (above). The GET of an API route returns the SPA fallback (200), as for confirm; asserted by "unsubscribed" absent and store unchanged.
- Logging: the service logs only the constant "Subscriber removed by an unsubscribe request"; the failed-send warning is a constant. `UnsubscribeTokensAndAddressesAreNeverLogged` checks name, email and tokens against the whole host log. Rules 4 and 6 hold. No secrets added.
- Rule 2: the service uses `DatabaseService` only (`ReadForUpdate`, `Remove`, `SaveChangesAsync`). Rule 5: CSRF on the page POST; the one-click POST is `[IgnoreAntiforgeryToken]` by RFC 8058 design, protected by the sealed token plus the fixed body, and rate limited.
- Rule 8: architecture and backlog updated in the same commit. Rule 10: one commit.

## Mismatches

None of severity high or medium.

- Low, `Docs/product-backlog.md` F10-US2 evidence / `worker-O2.md`: the evidence paragraph cites "S2 evidence in worker-O2.md", a work-item file, as the only record of the container check. Fine for now; the Captain should carry it into the final F10 evidence if S2 is to be closed.

## Advisories

1. Strengthen `TheUnsubscribeLinkStaysValidUntilTheSubscriberIsDeleted`: it implies key-lifetime coverage it does not have. Either rename it, or add a test that creates the second key ring entry (e.g. `KeyManagement` with an expired or rotated key) and shows an old token still unprotects.
2. Add a small purpose-isolation test: a token produced by `CreateProtector("other")` for the same id must fail `TryReadSubscriberId`.
3. Restart survival of the key ring rests on the persisted path only (worker states this). The TE10 run could add a restart then unsubscribe check once a valid link can be produced in the container.
4. The one-click URL carries the token in the query string; fine today (no access log in Caddy, app does not log URLs), but any future proxy access log would record tokens. Keep that in the deployment notes.
5. The tampered-token test edits the last three characters of a base64url string; it works because the payload is authenticated, but it does not pinpoint which bytes (MAC vs ciphertext) were changed. Low value to improve.
6. The timing side channel S1 from O1 is unchanged, as the worker says.
