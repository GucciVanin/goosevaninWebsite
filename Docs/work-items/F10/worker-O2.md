# Worker report - O2 (F10-US2, unsubscribe, plus the O2 addendum)

Verdict: **complete**. Nothing was committed or staged. F10-US1 and F10-US2 stay In Progress in the backlog (evidence paragraph added to F10-US2; F10-US1 acceptance test 3 reworded to "when the visitor opens the link and confirms on the page").

## What changed

Backend (`src/GooseWebsite.Api`):
- `Shared/Email/MailModels.cs`: `RenderedMail` gains an optional `Headers` dictionary (default null, so every existing caller compiles unchanged). `SmtpMailTransport.CreateMessage(mail, fromEmail, fromName)` (public static) builds the MIME message and writes the headers; `SendAsync` uses it.
- `Modules/Subscriptions`:
  - `Services/ISubscriptionLinks.cs`, `SubscriptionLinks.cs` (singleton): the reusable piece for O3. `UnsubscribePageUrl(id)`, `OneClickUnsubscribeUrl(id)`, `UnsubscribeHeaders(id)` (`List-Unsubscribe` and `List-Unsubscribe-Post`), `TryReadSubscriberId(token)`.
  - `ISubscriptionService.UnsubscribeAsync(token)` + implementation (token read, subscriber loaded by id, removed; false for anything invalid, with no distinction).
  - `Controllers/SubscriptionsController.cs`: `POST /api/subscriptions/unsubscribe` (body `{token}`, CSRF, confirm rate-limit policy) and `POST /api/subscriptions/unsubscribe/one-click?token=` (RFC 8058: no CSRF, form body must be `List-Unsubscribe=One-Click`, same policy). No GET handler. `Contracts/UnsubscribeRequest.cs`.
  - The confirmation mail now carries the unsubscribe link (text and HTML, after the confirmation link) and both headers (`SubscriptionEmailSender` / `SubscriptionEmailRenderer`; `ISubscriptionEmailSender.SendConfirmationAsync` takes the subscriber id).
  - `CleanName` strips Zl, Zp and Cf (and Cc) per Unicode scalar, so non-BMP tag characters go too (S4).
  - `Subscriber.UnsubscribeTokenHash` and its index removed; migration `20261029191605_DropSubscriberUnsubscribeTokenHash`.
- `.env.example` and `docker-compose.yml`: comments documenting that `PublicBaseUrl` is set by compose to `https://SITE_DOMAIN` (it was already set in compose; now documented).

Client: `features/subscriptions/unsubscribe-page.ts` (+ spec), `SubscriptionApi.unsubscribe`, route `/unsubscribe` (title "Unsubscribe"), shell mode without header/footer (`app.ts`), `app.spec.ts` loop extended.

Tests/verification: `tests/GooseWebsite.Api.Tests/UnsubscribeApiTests.cs` (partial of `SubscriptionsApiTests`, 17 new cases; the three O1 tests that referenced the dropped column were updated); `SubscriptionsApiTests` made `partial` and its harness takes an optional confirm limit. `verify/runtime/65-subscriptions.sh` (refusals, GET changes nothing, one-click refusals, the 429), `verify/runtime/30-pages.sh` (route), `verify/browser/helpers/screens.ts` (screen `unsubscribe`, so all generic checks incl. axe, 320 px, keyboard run on it), `verify/browser/tests/subscribe.spec.ts` (2 `unsubscribe:` tests, endpoint stubbed).

Docs: `Docs/architecture.md` (3.6c rows for endpoints, unsubscribe links, email, entity; shared mail row; matrix; flow note; frontend; shell modes; config rows `PublicBaseUrl` and `DataProtection:KeysPath`; testing), `Docs/product-backlog.md` (F10-US1 AC3 reworded; F10-US2 In Progress, tasks ticked, evidence), `Docs/tests/README.md`.

## Deviation from the decision (please review)

The decision says "HMAC-SHA256 of the subscriber id with a server secret from the Data Protection key ring". The key ring does not expose raw key bytes, and `IDataProtector.Protect` is randomised, so I used the Data Protection API directly: the token is the subscriber id sealed with `IDataProtector` (purpose `GooseWebsite.Subscriptions.Unsubscribe.v1`). Everything else in the decision holds: nothing stored, secret = the persisted key ring, forged or tampered tokens fail the integrity check (constant-time inside Data Protection), valid until the subscriber is deleted (old ring keys remain readable after rotation). Visible consequences: the token is about 100 characters, and each mail to one subscriber carries a different (equally valid) token. If a literal deterministic HMAC is wanted, it needs a separate persisted secret; say so and I will change it (the `ISubscriptionLinks` interface would not change).

## Key ring persistence

Already persisted: `AccountsModule.AddDataProtection` uses `DataProtection:KeysPath`, and `docker-compose.yml` sets `DataProtection__KeysPath: /data/keys` on the `web-data` volume (also used for cookies and antiforgery). Nothing new was needed. After the verification run `docker exec goosewebsite-web ls /data/keys` showed the key file present. A test proves a token from one ring is unreadable by another; the restart survival rests on the persisted path (not exercised by a restart test).

## S2 evidence (rate limit keyed on the real client address behind Caddy)

Run on the container stack after the full verification (verify container is 172.30.100.4, Caddy 172.30.100.2, the host connects through the published port):
- The verify container exhausted its unsubscribe/confirm bucket (20 per 10 min) during the full run; a second runtime-only run seconds later got 429 on every request of that policy (the 14 failures in that narrowed re-run are exactly those 429s, an expected side effect of repeating the stage inside the window; not part of the final totals).
- At the same time, from the host: requests 1-20 to `POST /api/subscriptions/unsubscribe` answered 400 (own fresh bucket) and the 21st answered 429. Two different clients behind the same Caddy therefore have separate buckets, so the key is the real client address (the `ForwardedHeaders:KnownProxies` value 172.30.100.2 is correct).
- Spoofing: sending `X-Forwarded-For: 1.2.3.4` and `9.9.9.9, 8.8.8.8` from the exhausted host stayed at 429 (Caddy overwrites the header).
The runtime file has a permanent check that the endpoint becomes rate limited; the two-address comparison itself is manual evidence.

## Doubts and notes for the Reviewer/Captain

- The one-click address carries the token in the query string (RFC 8058 needs a plain HTTPS POST URL). The application does not log request URLs (`Microsoft.AspNetCore` is at Warning; a test checks the whole host log) and Caddy has no access log configured; a reverse proxy with access logs would record it. The token only unsubscribes that one person.
- The one-click POST has no CSRF token by design (mail clients have no session); it needs the sealed token plus the fixed body.
- The runtime and browser checks send no valid unsubscribe (a valid token needs a mailed link and a real mail provider may be configured); the happy path, headers and one-click with a real token are covered by xUnit with the test mail service.
- Real mail clients honouring `List-Unsubscribe` are unchecked (existing project gap 2).
- GET of `/api/subscriptions/unsubscribe` returns the Angular fallback page (200), as the confirm endpoint does; the tests assert it changes nothing instead of expecting 405.
- F10-US2 acceptance test 4 (nothing sent to a deleted subscriber on the next notification) is satisfied structurally (the record is gone) and is to be exercised in O3.
- The timing side channel from O1 (S1) is unchanged.

## Test totals (final)

- Karma: 230 of 230 pass (225 + 5 in `unsubscribe-page.spec.ts`; the shell loop gained one URL).
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 238 passed, 0 failed (222 + 16 net: 17 new, and the O1 tests adjusted).
- `./scripts/verify.ps1 -SkipCi` (once, full): **971 passed, 0 failed, 0 skipped, 0 known** (unit-dotnet 238, unit-angular 230, runtime, browser 321 of 321 listed tests on Chromium, Firefox and WebKit); `verify/browser/known-issues.json` still empty. One extra narrowed runtime-only run (`VERIFY_STAGES=30`) was made for the S2 evidence only.

## Process notes

- TDD: the xUnit and Karma tests were written before the code (compile errors first); one test expectation (GET answering 405) was corrected to "changes nothing" after seeing the SPA fallback answer 200.
- Docker: only `goosewebsite-caddy`, `goosewebsite-web` and the `goosewebsite-verify` run container (via `verify.ps1`/`docker compose run`) were touched; at the end I ran `docker stop goosewebsite-caddy goosewebsite-web`. No other container or process.
- No git write commands, no secrets, no real addresses (`*.example.test`), no real email sent (the test mail service; the stack's runtime and browser checks never send a valid subscription). Names, emails and tokens are not logged (test `UnsubscribeTokensAndAddressesAreNeverLogged` and the O1 log test).
- Windows note: the Bash tool failed on large heredocs containing apostrophes; scripts were written to the scratchpad instead (not in the repo).
