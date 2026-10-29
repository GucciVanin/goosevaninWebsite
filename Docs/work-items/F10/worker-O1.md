# Worker report - O1 (F10-US1, subscribe with a name and an email)

Verdict: **complete**. Nothing was committed or staged. F10-US1 stays In Progress in the backlog (evidence paragraph added).

## What changed

Backend (`src/GooseWebsite.Api`):
- New module `Modules/Subscriptions/` (one line in `Program.cs`: `AddSubscriptionsModule(configuration)`):
  - `Domain/Subscriber.cs` (+ `SubscriberState`), `SubscriberConfiguration.cs`, `SubscriberAccessRule.cs` (`WriteAccess.Anyone`; Name 100 and Email 254 as `IsUserText` with limits).
  - `Contracts/SubscribeRequest.cs`, `ConfirmSubscriptionRequest.cs`.
  - `Controllers/SubscriptionsController.cs`: `POST /api/subscriptions`, `POST /api/subscriptions/confirm` (both `[AllowAnonymous]`, `[ValidateAntiForgeryToken]`, own per-IP rate-limit policy).
  - `Services/ISubscriptionService.cs`, `SubscriptionService.cs`, `SubscriptionTokens.cs`, `ISubscriptionEmailSender.cs`, `SubscriptionEmailSender.cs`, `SubscriptionEmailRenderer.cs` (branded HTML + plain text, derived from the account email layout), `PendingSubscriberCleanupService.cs` (hosted).
  - `Models/SubscriptionOptions.cs`, `Models/SubscribeOutcome.cs`; `SubscriptionsModule.cs`.
- Migration `Shared/Persistence/Migrations/20261029184328_AddSubscribers` (+ Designer, snapshot updated).
- `Program.cs`: using + registration line.

Client (`src/GooseWebsite.Client/src/app`):
- New `features/subscriptions/`: `subscription-api.ts`, `subscribe-form` (ts/html/scss), `subscribe-section` (ts/html/scss, the short home section `#subscribe`), `subscribe-confirm-page.ts`, and the two specs.
- Footer now contains the form (`layout/site-footer/*`); `app.html`/`app.ts` add the section to the home page and `/subscribe/confirm` to the no-header/footer shell mode; `app.routes.ts` adds the route with title "Confirm subscription"; `app.spec.ts` extended.

Tests and verification:
- `tests/GooseWebsite.Api.Tests/SubscriptionsApiTests.cs` (31 test cases).
- `verify/runtime/65-subscriptions.sh` (new), `verify/runtime/30-pages.sh` (route), `verify/browser/tests/subscribe.spec.ts` (new), `verify/browser/helpers/screens.ts` (screen `subscribe-confirmation`).

Docs: `Docs/architecture.md` (repo layout, dependency note, section 3.6c, matrix row, flow 5.4, frontend table and shell modes, config table, testing, latest result), `Docs/product-backlog.md` (F10-US1 status In Progress, tasks ticked, evidence), `Docs/tests/README.md` (browser table row, screen list).

## Behaviour and decisions

- Subscribe: the honeypot (`website`), an invalid email or an invalid name (empty, > 100) stores nothing and sends nothing. Every outcome (new, pending, confirmed, invalid, honeypot, mail failure) answers `200` with the same fixed message; only the per-IP limit differs (`429`, as Contact does). Defaults: 5 subscribe requests / 10 min / client address; confirm 20 / 10 min (separate policy).
- Tokens: 32 random bytes, Base64Url; SHA-256 hex stored. Confirmation token lifetime 7 days. Confirm clears the hash and expiry, so a link works once.
- Email uniqueness case-insensitive via column collation `NOCASE` + unique index. A race on the unique index is caught and answered identically.
- Pending resubmit: new token (old link stops working), new expiry, name updated, fresh mail; no second record. Confirmed address: no mail.
- Cleanup: `BackgroundService`, first run 30 s after start, then hourly; deletes pending where `ConfirmationTokenExpiresAtUtc <= now`; tested through `ISubscriptionService.RemoveExpiredPendingAsync`.
- Email validation is a regex (ASCII `local@domain.tld`, length <= 254, no whitespace/commas/angle brackets). I first used `System.Net.Mail.MailAddress`, but the existing architecture test `MailServiceTests.NoModuleTalksToSmtpOrMailerSendDirectly` forbids the string `System.Net.Mail` in modules. Internationalised (non-ASCII) addresses are therefore refused.
- Mail goes through `IMailService.SendAsync` (rendered path), so tests use the existing `TestMailService`; no real address is used anywhere.
- Logs: `Subscription request handled; outcome=<category>`, a warning when the mail was not sent, cleanup count / exception type. No names, emails, tokens. Test `NeitherTheNameNorTheEmailNorTheTokenIsEverLogged` captures the whole host log.

## Deviations from the Captain defaults / order (please review)

1. **Link format**: the decisions file says `/subscribe/confirm?token=`; I used the fragment `/subscribe/confirm#token=...` because `Docs/architecture.md` (PublicLinkBuilder, "values travel in the URL fragment so they never reach access logs or the Referer") and the other emailed links (`/verify-email`, `/contact/verify`) do that. Behaviour is otherwise as decided (client page POSTs the token).
2. **Confirmation needs a button press**: AC3 reads "when it is opened, then the subscriber becomes confirmed". I made the page confirm only when the person presses "Confirm subscription", following the project's own pitfall "state change on GET / mail scanners" (architecture 8.1) and because a scanner that runs scripts could otherwise confirm an address a third party typed (defeats double opt-in). The orders for O2 describe the same pattern for unsubscribe. If Gustavo wants auto-confirm on open, it is a one-line change in `SubscribeConfirmPage`.
3. **Resend cooldown (addition)**: a pending address is not mailed again within 2 minutes of the previous mail (`ResendCooldownMinutes`), regardless of IP, per pitfall 8.1 "rate limit by target address". It is derived from the expiry (no extra column). Tell me if the cooldown should go.
4. **Cleanup age**: "older than 7 days" is implemented as "confirmation link expired", i.e. 7 days after the latest confirmation mail (equals creation + 7 days unless the person resubmitted).
5. **Runtime and browser checks send no valid subscription.** The stack's `.env` may hold real SMTP/MailerSend settings and there is no mail sink in the container, so (like `40-contact.sh`) the runtime file only sends refusals/neutral requests and the browser tests stub both subscription endpoints with `page.route`. The valid flow, double opt-in and mail content are covered by xUnit with `TestMailService`. A container-level mail sink would be a new feature (not built).
6. Honeypot input has no label and sits in an `aria-hidden` wrapper with `tabindex="-1"`, moved off-screen (same approach as the contact form).
7. Confirmation page heading uses `overflow-wrap: anywhere` because "SUBSCRIPTION" in the display font overflowed 320 px (found by the browser stage, fixed).

## Doubtful / for the Reviewer and later orders

- **Read access**: `SubscriberAccessRule` has no `VisibleWhen`, i.e. every caller can read rows through the gateway, because the anonymous subscribe/confirm service must look addresses up. Protection is that no endpoint returns subscriber rows (test `NoEndpointListsSubscribers`, runtime "no endpoint lists subscribers"). A stricter data-layer design would need a gateway change (out of scope).
- **O2/O3 design issue**: only the *hash* of the unsubscribe token is stored (decided). A mail to an already confirmed subscriber (O3 notifications) needs the raw token in the link, which cannot be recovered from the hash. O2/O3 will need to either derive the unsubscribe token (for example an HMAC of the subscriber id with a Data Protection key) or store it differently. Today the unsubscribe hash is generated at creation / each resend and the raw value is discarded.
- **Timing side channel**: a new/pending address does a database write and a mail send before answering; a confirmed or invalid one does not, so response time can differ. Not mitigated (could send mail in the background).
- The hosted cleanup timer was not run for real; only its logic and registration are tested.
- Real mail delivery and the email's look in real clients: untested (project-wide open gap).
- The migration file uses the block-namespace style the generator emits, like the existing migrations (IDE0161 warning, same as before).

## Test totals (final)

- Karma: 225 of 225 pass (new: `subscribe-form.spec.ts`, `subscribe-confirm-page.spec.ts`, one new shell case and the extended link-page loop in `app.spec.ts`).
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 222 passed, 0 failed (31 new in `SubscriptionsApiTests`).
- `./scripts/verify.ps1 -SkipCi`: 922 passed, 0 failed, 0 skipped, 0 known; browser stage 303 of 303 tests reported on Chromium, Firefox and WebKit; `verify/browser/known-issues.json` still empty. (An earlier run found 3 failures, the 320 px overflow on the confirmation page, fixed; the totals above are from the clean re-run, so `verify.ps1` was run twice in total.)

## Process notes

- TDD: xUnit and Karma specs were written first and failed for the right reason (module and components did not exist: compile errors). Then code; the 320 px browser failure was fixed after the stage ran.
- Docker: only the compose stack of this project was touched (`verify.ps1` rebuilds and starts `goosewebsite-web`, `goosewebsite-caddy` and the `goosewebsite-verify` run container); at the end I ran `docker stop goosewebsite-caddy goosewebsite-web`. No other container or process was touched.
- No git write commands, no secrets, no real addresses (test addresses are `*.example.test`).
- Files changed in `Docs/product-backlog.md`/`architecture.md` were edited preserving their line endings.
