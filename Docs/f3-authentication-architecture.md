# F3 User Accounts and Authentication Architecture

**Status (2026-10-01):** Design record, partly implemented. The code lives in `src/GooseWebsite.Api/Modules/Accounts/`; [architecture.md](./architecture.md) describes it as built. This note keeps the reasoning and the remaining design.

| Area | State |
| --- | --- |
| R1: CSRF endpoint, shared login, logout, `/me`, `AdminOnly`, cookie flags, lockout, operator provisioning | Implemented and covered by integration tests |
| R3: reader registration, resend, verify-email, `VerifiedReader` policy | Implemented ahead of R3; gated by `Auth:ReaderRegistration:Enabled` (off by default, on in Development) |
| Reader display-name update (`PATCH /api/auth/me`) | Not implemented |
| Reader password reset (`forgot` / `reset`) | Not implemented; story F3-US4 |
| Administrator recovery command | Not implemented; story F3-US3 |
| EF Core migrations replacing `EnsureCreated` | Not implemented; enabler TE1 in [project.md](./project.md) |

**Deviations from the proposals below:** the password minimum is 12 characters as proposed (it was 8 until 2026-10-02) and lockout is 5 attempts for 15 minutes; the verification link carries its token in the URL fragment; registration, resend, and verification share one fixed-window limit of 5 requests per client IP per 10 minutes. Treat the "Decisions to confirm" table as still open for Gustavo.

**Scope:** F3-US1 administrator authentication in R1 and F3-US2 reader registration and email verification in R3. This design provides the authentication and authorization seam used by F4 editor APIs and F6 comment APIs; it does not implement those features.

## Goals and constraints

- Keep authentication on the existing same-origin ASP.NET Core API and use the existing ASP.NET Core Identity and EF Core SQLite stack.
- Provision administrator accounts out of band. More than one administrator may exist; there is no public administrator registration, role-selection field, or default administrator credential.
- Let readers register only as readers. A reader must verify their email before signing in for commenting or accessing comment operations.
- Keep passwords, authentication tokens, private email addresses, and role-changing operations out of public responses and logs.
- Use an HttpOnly cookie session rather than putting credentials or bearer tokens in Angular storage.
- Enforce authorization in the API. Client-side route guards are only navigation aids and never grant access.
- Keep R1 useful without reader registration. Add reader account endpoints and screens with R3.

## Target system shape

```text
Angular same-origin client
    |  GET /api/auth/me; POST /api/auth/*
    |  antiforgery request token on state-changing requests
    v
ASP.NET Core API
    |-- AuthController
    |     |-- UserManager<ApplicationUser> / SignInManager<ApplicationUser>
    |     |-- AdminOnly policy
    |     `-- VerifiedReader policy (live email-confirmation check)
    |-- AccountEmailSender -> configured SMTP provider
    `-- ASP.NET Core Identity cookie authentication
              |
              v
        ApplicationDbContext -> SQLite
        AspNetUsers / AspNetRoles / AspNetUserRoles / Identity token data
```

The Identity cookie is protected by ASP.NET Core Data Protection. Production Data Protection keys must persist on the existing mounted data volume so a container replacement does not invalidate every session. Serve the site over HTTPS in production; configure trusted forwarded headers before HTTPS redirection or secure-cookie decisions when behind a proxy.

## Identity and persistence model

Retain `ApplicationUser : IdentityUser` and its `DisplayName`. Identity remains responsible for normalized account identifiers, password hashes, email-confirmation state, lockout, security stamps, roles, and protected confirmation/reset tokens. Configure `RequireConfirmedEmail` so Identity itself refuses an unverified reader's sign-in, in addition to the API authorization policy.

Use only these server-managed roles:

| Role | Assignment | Authorization use |
| --- | --- | --- |
| `Admin` | An explicit operator-authorized provisioning operation only | Private editor and other administrator-only APIs |
| `Reader` | Assigned by the server after a reader registration is created | Reader account classification; never implies verified status |

`EmailConfirmed` is the source of truth for reader verification. The `VerifiedReader` authorization policy must check the persisted user state rather than trusting a client field or a potentially stale cookie claim. The policy requires an authenticated `Reader` and a currently confirmed email. F6 comment endpoints must use this policy. F4 endpoints must use `AdminOnly`; `[Authorize]` alone is insufficient.

Do not accept or bind `Id`, role names, `EmailConfirmed`, `LockoutEnabled`, or other privilege-bearing Identity fields from public request bodies. Derive the user for comment ownership and account operations from the authenticated principal, not a submitted user ID.

Public comment/profile DTOs must never include email, password hashes, confirmation codes, security stamps, or role-management data. Use separate response contracts for public display names and the signed-in user's own account state. Do not reuse an Identity entity as an API response.

Use EF Core migrations for Identity schema changes and deploy them without dropping the existing SQLite database. The current startup `EnsureCreated` path must be replaced or carefully baselined before relying on migrations; back up the database and preserve existing content during that transition. Remove the demo user/content seed from production initialization. If existing content has authorship references, backfill ownership to Gustavo's provisioned account as an explicit migration rather than preserving a synthetic author.

## Provisioning administrator accounts (R1)

Provide a maintenance-only provisioning command executed by an authorized deployment operator on the application host. It must:

1. Ensure the database schema and required roles exist.
2. Prompt interactively for the target administrator's email and a new password, with password entry masked. Never accept the password as a command-line argument, checked-in setting, seed value, or log value.
3. Allow additional administrators when explicitly provisioned. Refuse to create an account if the requested normalized email/account already exists; never silently reset credentials or promote an existing reader. The presence of other administrators is not a reason to refuse provisioning.
4. Create the target account with `EmailConfirmed = true` because an authorized operator is provisioning a known administrator, then assign the `Admin` role.
5. Report success/failure without printing credentials or unnecessary personal data.

There is no HTTP endpoint for bootstrap, promotion, or administrator registration. A failed partial operation must be safe to rerun only after the operator has inspected and corrected the state; it must not grant privileges to an arbitrary existing reader. Each administrator is a distinct account, and every account created by the authorized provisioning operation receives the `Admin` role. If Gustavo's account needs recovery, the proposed R1 path is another explicitly authorized, interactive operator command that resets only the selected existing administrator's password and never creates or promotes an account. Confirm this recovery choice before F3-US1 is marked `Ready`.

The R1 command is available as `dotnet run --project src/GooseWebsite.Api -- admin provision` during development and `dotnet GooseWebsite.Api.dll admin provision` in deployment. Password and confirmation are read interactively without echo and are never command-line arguments. Startup creates the `Admin` role if missing but does not create a user or seed content.

## API contract

All routes are same-origin under `/api/auth`. Requests and responses use dedicated DTOs with server-side validation. All state-changing requests require a valid antiforgery token.

| Route | Availability | Behavior |
| --- | --- | --- |
| `GET /api/auth/csrf` | R1 | Issues the antiforgery cookie and returns a request token for the Angular client. |
| `POST /api/auth/login` | R1 | Checks credentials with Identity lockout enabled; establishes a non-persistent cookie only for an eligible account. Return one generic failure for unknown, locked, invalid-password, or unconfirmed accounts. |
| `POST /api/auth/logout` | R1 | Requires authentication and antiforgery validation; clears the Identity cookie. |
| `GET /api/auth/me` | R1 | Returns the authenticated account's safe UI state (for example, display name and authorization flags); returns 401 when signed out. Never return a password, token, or public email field. |
| `POST /api/auth/register` | R3 | Creates a `Reader`, issues email verification, and does not sign the new account in. Return a generic accepted response that does not disclose whether an email already has an account. |
| `POST /api/auth/verification/resend` | R3 | Sends a fresh confirmation link for an eligible unconfirmed account; return the same generic response for all account states. |
| `POST /api/auth/verify-email` | R3 | Accepts the verification subject/code from an explicit confirmation action; confirms a valid, unexpired token. Invalid, expired, or already-used links fail without changing state. |
| `PATCH /api/auth/me` | R3 | Updates only the authenticated reader's public display name; derive the account from the cookie and reject role, email, and identifier fields. |
| `POST /api/auth/password/forgot` | R3 | Sends a reader reset link when eligible and always returns the same public response regardless of account existence. |
| `POST /api/auth/password/reset` | R3 | Validates a protected, time-limited reset token, changes the password, and invalidates existing sessions by updating Identity security state. |

`/api/auth/login` is a single shared sign-in route for all account types. After login, the client may read `/me` to determine the current account's access-level state and choose the appropriate UI. The API must still authorize every operation independently. Administrator access requires the `Admin` role; reader comment access requires the live `VerifiedReader` policy. The server returns an `isAdmin` (or equivalent access-level) signal to the client, but does not accept the role from the client.

For registration and resend, return a generic response with the same status and shape for new, confirmed, unknown, and awaiting-confirmation addresses. For a duplicate unconfirmed account, the resend action may issue a new link; for a confirmed or unknown account it sends nothing. Rate-limit email-triggering operations and do not return Identity validation descriptions that reveal whether an email is registered. If delivery fails, return a generic temporary-service failure rather than a success-shaped response, log only a safe failure category, and leave the account unconfirmed so a rate-limited resend can recover.

## Reader registration and verification flow (R3)

```text
Reader submits email, password, display name
    -> POST /api/auth/register
    -> validate and normalize input; apply registration limits
    -> create unprivileged Reader with EmailConfirmed=false
    -> generate Identity email-confirmation token
    -> send confirmation email through AccountEmailSender
    -> return generic "check your email" response; no session is created

Reader opens verification screen and explicitly confirms
    -> POST /api/auth/verify-email { userId, code }
    -> ConfirmEmailAsync validates protected token and expiry
    -> on success set EmailConfirmed=true
    -> subsequent login may establish a cookie
```

Use the Identity Data Protection email-confirmation token provider with an explicitly configured 24-hour lifetime as the proposed default. A mail link must open a confirmation screen; only the screen's explicit POST consumes the confirmation action, so mail-security link scanners do not verify accounts by fetching a URL. The confirmation page must not load third-party analytics, must use `Referrer-Policy: no-referrer`, and verification codes in query strings must be redacted from application/proxy logs. Once an account is confirmed, subsequent submissions for that account are rejected as expired/invalid/reused and do not change state.

Use the same generic public response for an unknown, invalid, expired, or already-used verification token. Do not sign a reader in as a side effect of registration or confirmation; require a deliberate login after verification. An unconfirmed reader cannot sign in for commenting, and every comment API independently rejects that account even if an old cookie exists.

The account-email sender is a feature-level abstraction over the configured mail transport; authentication code must not depend on contact-message delivery behavior or reuse contact DTOs. Verification/reset messages contain only the required link and expiry guidance. Never log token values, email addresses, or message bodies.

## Cookie, CSRF, and abuse controls

- Cookie: `HttpOnly=true`, `Secure=Always` in production, `SameSite=Lax`, host-only (no broad `Domain`), path `/`, and no persistent `RememberMe` cookie for the proposed baseline. Set an explicit idle timeout (proposed 30 minutes) and sliding expiration; document that an Identity cookie is not a durable server-side inbox/session record.
- Data Protection: persist keys in the mounted `/data/keys` location in production; restrict filesystem access to the application. Do not share keys across unrelated environments.
- CSRF: configure ASP.NET Core antiforgery with a request-token response at `GET /api/auth/csrf`, an HttpOnly antiforgery cookie, and a custom request header (for example `X-CSRF-TOKEN`). Angular keeps the request token in memory and attaches it to all state-changing same-origin requests, including login, registration, logout, verification, and reset. Validate the token server-side; do not rely on SameSite alone.
- Transport: require HTTPS in production, use secure cookies, and accept forwarded scheme/client information only from configured trusted proxies. Do not trust arbitrary `X-Forwarded-*` headers for security decisions.
- Credential defense: configure Identity password hashing, a minimum password length (proposed 12 characters, no arbitrary composition rules), account lockout, and bounded IP-based request limits for login, registration, verification, resend, and reset. Registration/email-triggering routes need stricter limits than ordinary reads. Exact thresholds and lockout duration are deployment settings to confirm.
- Enumeration/privacy: use generic sign-in, registration, verification, and reset messages; do not log submitted credentials, email addresses, tokens, or display-name/email pairs. Limit and redact request-body and query-string logging on auth routes.
- Session changes: logout clears the cookie; password reset and security-sensitive account changes update the Identity security stamp so existing cookies are invalidated on the configured security-stamp validation interval. HTTPS, Data Protection key persistence, lockout, and session expiry are all deployment gates.

Do not add JWTs, local storage token handling, public OAuth providers, roles chosen by a client, or public administrator setup for this feature.

## Angular feature boundary

Add a small account feature that owns one shared `/sign-in` screen for all account types, plus sign-out, registration, verification-result, display-name editing, and reset screens. It uses typed auth request/response contracts and the shared `HttpClient`; a single interceptor obtains/attaches the antiforgery request token for mutations and handles 401/403 responses without redirect loops. The backend returns the authenticated account's access-level state, while every privileged API continues enforcing its own role policy. Keep registration and reader-specific screens out of the R1 public navigation until R3.

Angular route guards may hide editor links for signed-out/non-admin users, but API policies remain authoritative. Account screens must follow the existing form-label, validation, focus, keyboard, reduced-motion, contrast, and narrow-viewport patterns. Show a neutral verification-pending state and session-expired feedback without exposing account-existence information.

## Rollout and dependencies

1. R1: baseline/preserve the existing database, remove demo identity seeding, add safe provisioning for one or more administrators, configure cookie/CSRF/lockout behavior, and implement login/logout/me plus AdminOnly policy.
2. F4: protect all editor reads and mutations with `AdminOnly`; confirm anonymous and reader identities receive no editor data or side effects.
3. R3: add reader registration, email confirmation and reset mail, verification UI, and the live `VerifiedReader` policy.
4. F6: apply `VerifiedReader` to every comment write/action and source comment identity/display name from the authenticated user record.

Do not expose public registration in R1. F3-US2 and its registration UI are R3 work, although their contracts are defined here to avoid a second identity model.

## Verification plan

Automated API/integration tests must cover:

- Provisioning supports multiple distinct confirmed Admin accounts; an existing administrator does not block creation of another, while duplicate-account attempts cannot create, promote, or reset an account; no public request can assign a role or administrator identifier.
- Correct and incorrect login, generic failures for unknown/unconfirmed/locked users, lockout/rate limits, cookie flags, protected `/me`, and logout/expiry behavior.
- Missing/invalid antiforgery tokens block all cookie-authenticated mutations, including login and logout.
- R3 registration creates only an unconfirmed Reader, never signs them in, and returns no email/role/credential data; duplicate submissions do not reveal account existence.
- Reader display-name updates affect only the signed-in reader's name and cannot change email, account ID, or roles.
- Valid confirmation works; malformed, expired, replayed, and already-used links do not confirm an account; GET link scanning does not mutate account state.
- Verified and unverified authentication/authorization, including a stale-cookie attempt against `VerifiedReader`.
- Password reset token expiry/use, generic forgot-password responses, and security-stamp invalidation.
- Public DTO and comment responses omit email and all credentials/tokens; logs omit submitted email, password, and token values.

Browser checks cover keyboard-only and 320 CSS-pixel flows for the shared `/sign-in` flow and reader registration/verification screens, validation and status feedback, visible focus, and no horizontal overflow. Exercise the same-origin client/API deployment and production HTTPS cookie configuration.

## Decisions to confirm before F3 is Ready

These are recommendations in this design, not recorded product decisions:

| Decision | Proposed default |
| --- | --- |
| Administrator recovery | **Confirmed 2026-10-02.** Authorized operator-only interactive reset command for the selected existing administrator; no public administrator password-recovery endpoint |
| Reader recovery | Generic-response email reset flow in R3 |
| Confirmation token lifetime | 24 hours |
| Idle cookie lifetime | 30 minutes, non-persistent |
| Password minimum | 12 characters; no composition rules |
| Lockout and request limits | Identity lockout plus bounded endpoint/IP limits; exact values configured and reviewed before release |
| Registration timing | R3 only; generic response and no automatic sign-in |

Confirm these choices and configure the exact mail provider, rate-limit thresholds, lockout duration, and reset/verification email copy before marking the affected stories `Ready`.
