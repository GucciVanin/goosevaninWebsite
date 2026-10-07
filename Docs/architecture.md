# GooseWebsite Architecture

**Audience:** AI agents and developers. Read this before implementing a feature or changing existing behavior.  
**Also for:** Gustavo, to see the product as it is built today. Diagrams are Mermaid and render in VS Code, GitHub, and most Markdown viewers.  
**Last verified against the code:** 2026-10-01

This document describes **what exists now**, not the target. For the target see [project.md](./project.md) and [product-backlog.md](./product-backlog.md). Where the code falls short of the target, section 11 lists the gap. Keep this file in the same change as any structural change.

Contents: [1 System](#1-system-context) · [2 Repository](#2-repository-layout) · [3 Backend](#3-backend-modules) · [4 Data](#4-data-model) · [5 Flows](#5-key-flows) · [6 Frontend](#6-frontend) · [7 Config and operations](#7-configuration-build-and-operations) · [8 Conventions](#8-conventions) · [9 Extending](#9-how-to-add-change-or-remove-a-feature) · [10 Testing](#10-testing) · [11 Known gaps](#11-known-gaps-and-technical-debt)

## 1. System context

One ASP.NET Core process serves both the JSON API and the compiled Angular client from a single origin. In Docker, Caddy terminates TLS in front of it. State is a SQLite file and a folder of Data Protection keys on one volume. Outbound mail goes through SMTP.

```mermaid
flowchart LR
    visitor([Visitor browser])
    admin([Administrator browser])
    sender([Contact sender mailbox])

    subgraph host [Docker host]
        caddy[Caddy<br/>TLS reverse proxy<br/>ports 80 and 443]
        subgraph app [web container, port 8888]
            api[ASP.NET Core<br/>GooseWebsite.Api]
            spa[Angular build<br/>wwwroot static files]
        end
        vol[(web-data volume<br/>goosewebsite.db<br/>Data Protection keys)]
    end

    smtp[[MailerSend<br/>SMTP and HTTP API]]
    inbox([Gustavo's inbox])

    visitor -->|HTTPS| caddy
    admin -->|HTTPS| caddy
    caddy -->|HTTP, forwarded headers| api
    api --- spa
    api --> vol
    api -->|SMTP over TLS: account emails| smtp
    api -->|HTTPS API: templated contact emails| smtp
    smtp --> inbox
    smtp --> sender
```

Runtime decisions that other code depends on:

- **Single origin.** The client calls relative `/api/...` URLs. There is no CORS configuration, and none should be added without a decision.
- **Trusted proxy only.** `X-Forwarded-For` and `X-Forwarded-Proto` are honored only from the addresses in `ForwardedHeaders:KnownProxies` (Caddy's fixed address in Compose). Client IP drives rate limiting, so never widen this.
- **Authorization lives in the API.** Angular route guards are conveniences only.

## 2. Repository layout

```text
GooseWebsite/
├── GooseWebsite.slnx                 Solution (API + tests)
├── Directory.Build.props             Shared .NET settings (net10.0, nullable, implicit usings)
├── Dockerfile                        Three stages: client build, .NET publish, runtime
├── docker-compose.yml                web + caddy, private network, one data volume
├── Caddyfile                         TLS reverse proxy to web:8888
├── .env.example                      Template for SMTP and domain settings (copy to .env)
├── CLAUDE.md                         Instructions for AI agents working in this repo
├── Docs/
│   ├── goal.md                       Executive purpose (the ultimate goal)
│   ├── project.md                    Scrum roadmap and high-level technical spec
│   ├── architecture.md               This file
│   ├── product-backlog.md            Stories and acceptance tests
│   ├── f3-authentication-architecture.md   Design record for accounts
│   ├── Api/GooseWebsite.http         Sample requests for the REST client
│   └── archive/                      Superseded documents
├── src/
│   ├── GooseWebsite.Api/             ASP.NET Core application
│   │   ├── Program.cs                Composition root, one line per module
│   │   ├── Modules/                  One folder per feature (section 3)
│   │   │   ├── Accounts/
│   │   │   ├── Blog/
│   │   │   └── Contact/
│   │   ├── Shared/                   Cross-cutting infrastructure (section 3.2)
│   │   │   ├── Email/  Hosting/  Modules/  Persistence/
│   │   ├── Data/                     SQLite file for local development (git-ignored)
│   │   ├── data-protection-keys/     Local cookie-protection keys (git-ignored)
│   │   ├── wwwroot/                  Angular build output (git-ignored)
│   │   └── appsettings*.json
│   └── GooseWebsite.Client/          Angular application (section 6)
└── tests/
    └── GooseWebsite.Api.Tests/       xUnit tests (section 10)
```

Inside each backend module the folders mean the same thing everywhere:

| Folder | Holds |
| --- | --- |
| `Controllers/` | HTTP endpoints only: bind, authorize, call a service, map to a contract |
| `Contracts/` | Request and response DTOs that cross the HTTP boundary |
| `Domain/` | Entities, constants, and EF mapping owned by the module |
| `Services/` | Business logic, repositories, and adapters to outside systems |
| `Models/` | Internal types that never leave the module's services |
| `Authorization/` | Policies and handlers (Accounts only) |
| `<Name>Module.cs` | The single `Add<Name>Module` extension that registers everything above |

## 3. Backend modules

### 3.1 Dependency rules

```mermaid
flowchart TB
    program[Program.cs<br/>composition root]

    subgraph modules [Feature modules]
        contact[Contact]
        blog[Blog]
        accounts[Accounts]
    end

    subgraph shared [Shared]
        persistence[Persistence<br/>DatabaseService gateway<br/>ApplicationDbContext]
        email[Email<br/>ISmtpMailSender]
        hosting[Hosting<br/>trusted proxy headers]
        mod[Modules<br/>IModuleInitializer<br/>IModuleCommand]
    end

    program --> contact
    program --> blog
    program --> accounts
    program --> shared

    blog -->|AuthorizationPolicies, ApplicationUser| accounts
    accounts --> persistence
    blog --> persistence
    accounts --> email
    contact --> email
    accounts --> mod
    persistence --> mod
```

- `Program.cs` may reference every module; modules never reference `Program`.
- **Contact** depends on `Shared` only. **Accounts** depends on `Shared`. **Blog** depends on `Shared` and on `Accounts` (for the author entity and the authorization policy names). Do not add the reverse direction. If two modules need the same thing, move it into `Shared`.
- `Shared` never references a module, with one deliberate exception: `ApplicationDbContext` and `DatabaseService` know `ApplicationUser` from Accounts, because Identity shares the database. Every other module table is discovered through `IEntityTypeConfiguration<T>` without `Shared` naming it.
- **All database reads and writes go through `DatabaseService`.** A module keeps its own query logic in a service or repository (what to ask for) but calls the gateway (how to ask). Never inject `ApplicationDbContext`, `UserManager`, or `RoleManager` into a module class other than Identity registration. This keeps sanitization and access checks in one place as features grow.

### 3.2 Shared layer

| Component | What it does | Used by |
| --- | --- | --- |
| `Shared/Persistence/DatabaseService` | **The single gateway for every database read and write.** Generic entity access (`Read<T>`, `ReadForUpdate<T>`, `FindAsync<T>`, `Add`, `Remove`, `SaveChangesAsync`) plus the account store (users, roles). Modules never use `ApplicationDbContext`, `UserManager`, or `RoleManager` directly. Input sanitization and access checks are not implemented yet (F7-US1); this class is where they will go. | Accounts, Blog |
| `Shared/Persistence/ApplicationDbContext` | The EF Core context behind the gateway. Applies every `IEntityTypeConfiguration<T>` found in the assembly. | `DatabaseService` only |
| `Shared/Persistence/PersistenceServiceCollectionExtensions` | `AddSharedPersistence`: registers SQLite and resolves a relative database path against the content root, so the working directory never matters | `Program.cs` |
| `Shared/Persistence/DatabaseInitializer` | First `IModuleInitializer`: runs `EnsureCreated` | startup |
| `Shared/Modules/IModuleInitializer` | A module's startup hook (for example, create roles). Run in registration order after the database exists. | Accounts, Persistence |
| `Shared/Modules/IModuleCommand` | A module's operator command-line entry point. If one handles the arguments, the web host does not start. | Accounts (`admin provision`) |
| `Shared/Email/ISmtpMailSender` | The only class that talks to SMTP. Returns `false` and logs only the exception type on failure. | Accounts, Contact |
| `Shared/Email/IMailerSendClient` | The only class that talks to the MailerSend HTTP API. Sends one templated email (template id plus variables). Returns `false` and logs only the HTTP status code on failure. | Contact |
| `Shared/Hosting/AddTrustedProxyHeaders` | Forwarded-header trust from configuration | `Program.cs` |

Startup order in `Program.cs`: register shared services, register modules, build, run all `IModuleInitializer`s, offer the arguments to all `IModuleCommand`s, then configure the pipeline.

### 3.3 Request pipeline

```mermaid
flowchart TD
    req([Request]) --> fwd[UseForwardedHeaders<br/>trusted proxies only]
    fwd --> ref[Referrer-Policy: no-referrer]
    ref --> rl[UseRateLimiter<br/>policy: reader-account-email]
    rl --> https[UseHttpsRedirection]
    https --> files[UseDefaultFiles and UseStaticFiles<br/>wwwroot]
    files --> authn[UseAuthentication<br/>Identity cookie]
    authn --> authz[UseAuthorization<br/>AdminOnly, VerifiedReader]
    authz --> ctrl{{MapControllers}}
    ctrl -->|no route| spa[MapFallbackToFile index.html<br/>Angular router handles the URL]
```

### 3.4 Accounts module

Owns identity, sessions, and authorization. Source: `Modules/Accounts/`.

| Piece | File | Notes |
| --- | --- | --- |
| Registration of everything | `AccountsModule.cs` | Identity options, cookie, antiforgery, Data Protection, rate limit, policies |
| Roles | `Domain/AccountRoles.cs` | `Admin`, `Reader`. Server-managed; never accepted from a client |
| User entity | `Domain/ApplicationUser.cs` | Identity user plus `DisplayName` |
| Policies | `Authorization/AuthorizationPolicies.cs` | `AdminOnly`, `VerifiedReader`. Other modules reference these constants |
| Live verification check | `Authorization/VerifiedReaderAuthorizationHandler.cs` | Reads `EmailConfirmed` and role from the database on every call, so a stale cookie cannot authorize |
| Endpoints | `Controllers/AuthController.cs` | See the table below |
| Role seeding | `Services/AccountRoleInitializer.cs` | Creates missing roles at startup, never users |
| Admin provisioning | `Services/AdminProvisioningService.cs`, `AdminProvisioningCommand.cs` | Interactive operator command; refuses existing accounts; never promotes a user |
| Verification email | `Services/IAccountEmailSender.cs`, `SmtpAccountEmailSender.cs` | Separate from contact delivery on purpose |

Security settings in force: unique email, password minimum 12 characters with no composition rule, lockout after 5 failures for 15 minutes, email confirmation required to sign in, confirmation token lifetime 24 hours, cookie `goosewebsite.auth` (HttpOnly, SameSite=Lax, Secure always in Production, 30-minute sliding expiry, not persistent), antiforgery cookie `goosewebsite.csrf` with header `X-CSRF-TOKEN`. Cookie events return 401 and 403 rather than redirecting.

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /api/auth/csrf` | anonymous | Issue antiforgery cookie and request token |
| `GET /api/auth/reader-registration` | anonymous | Whether reader registration is enabled |
| `POST /api/auth/login` | anonymous, CSRF | Sign in; same generic error for unknown, wrong, locked, or unconfirmed |
| `POST /api/auth/logout` | signed in, CSRF | Sign out |
| `GET /api/auth/me` | signed in | Display name and server-derived `isAdmin` |
| `POST /api/auth/register` | anonymous, CSRF, rate limited | Create unconfirmed Reader; always the same response. Returns 404 unless `Auth:ReaderRegistration:Enabled` is true |
| `POST /api/auth/verification/resend` | anonymous, CSRF, rate limited | Resend confirmation; generic response |
| `POST /api/auth/verify-email` | anonymous, CSRF, rate limited | Confirm with `userId` and `token` from the link |

### 3.5 Contact module

Owns the public contact form. Source: `Modules/Contact/`. It keeps no durable data.

| Piece | File | Notes |
| --- | --- | --- |
| Registration | `ContactModule.cs` | Binds `ContactOptions`; singletons for the token store and abuse guard |
| Endpoints | `Controllers/ContactController.cs` | `POST /api/contact` (emails the link to the sender), `POST /api/contact/verify` (body `{token}`; the emailed link opens the client page `/contact/verify#token=...`) |
| Abuse guard | `Services/ContactSubmissionAbuseGuard.cs` | Thread-safe sliding windows in two separate bounded stores: per socket IP, and per submitted address (stored only as a SHA-256 hash, case-insensitive). Configured under `Contact:RateLimit` |
| Pending store | `Services/ContactVerificationService.cs` | In-memory, 30-minute lifetime, single use, removed on use or expiry |
| Delivery | `Services/ContactEmailDeliveryService.cs` | Three MailerSend templates through `IMailerSendClient`: the confirm link to the sender, then the notice to Gustavo, then the receipt to the sender. Template ids come from `Contact:Templates`; the variables each template needs are in `Models/ContactEmailTemplateVariables.cs` and pinned by `ContactEmailTemplateTests` |

Validation on `POST /api/contact`: honeypot field `website` must be empty, all four fields required, `reason` must be `Work or collaboration` or `Personal note`, email must be well formed, then the rate limit applies (HTTP 429 before any token is created). Logs carry the reason and a client key, never the sender's address or the message body.

### 3.6 Blog module

Owns posts. Source: `Modules/Blog/`. It is the seed for Stories and Projects (F4, F5).

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /api/blog?search&category&page&pageSize` | public | Published posts only; page size clamped to 50 |
| `GET /api/blog/{slug}` | public | One published post |
| `POST /api/blog` | `AdminOnly`, CSRF | Create; records the author and generates a collision-safe slug |
| `PUT /api/blog/{id}` | `AdminOnly`, CSRF | Update |
| `DELETE /api/blog/{id}` | `AdminOnly`, CSRF | Delete |

`BlogController` depends on `IBlogPostRepository` (implemented by `BlogPostRepository`), which holds the blog's query logic and performs every read and write through `DatabaseService`. The table mapping is in `Domain/BlogPostConfiguration.cs`.

### 3.7 Authorization matrix

| Operation | Anonymous | Reader, unconfirmed | Reader, confirmed | Admin |
| --- | --- | --- | --- | --- |
| Read published blog posts | yes | yes | yes | yes |
| Submit contact form | yes (rate limited) | yes | yes | yes |
| Create, update, delete posts | no (401) | no (cannot sign in) | no (403) | yes |
| Comment actions (R3, `VerifiedReader`) | no | no | yes | not by role alone |

## 4. Data model

```mermaid
erDiagram
    ApplicationUser ||--o{ BlogPost : "authors (restrict delete)"
    ApplicationUser }o--o{ IdentityRole : "AspNetUserRoles"

    ApplicationUser {
        string Id PK
        string Email
        string UserName
        string DisplayName
        bool EmailConfirmed
        string PasswordHash
        datetime LockoutEnd
    }
    IdentityRole {
        string Id PK
        string Name "Admin or Reader"
    }
    BlogPost {
        int Id PK
        string Slug UK
        string Title
        string Excerpt
        string Content
        string Category
        string Tags "comma separated"
        bool IsPublished
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
        string AuthorId FK
    }
```

Identity also creates its standard claim, login, and token tables; they are omitted here. Contact messages are **not** stored. The pending verification dictionary exists only in process memory.

The schema is created with `EnsureCreated`. It cannot change an existing database, so the first schema change (expected with F4) must be preceded by enabler TE1 (migrations).

## 5. Key flows

### 5.1 Sign-in with CSRF protection

```mermaid
sequenceDiagram
    autonumber
    participant B as Angular client
    participant A as AuthController
    participant I as Identity (UserManager, SignInManager)

    B->>A: GET /api/auth/csrf
    A-->>B: antiforgery cookie plus requestToken
    B->>A: POST /api/auth/login with X-CSRF-TOKEN
    A->>A: burn dummy password-hash work
    A->>I: FindByEmail, PasswordSignIn (lockout on)
    alt unknown, wrong, locked, or unconfirmed
        A-->>B: 401 generic error
    else success
        I-->>B: Set-Cookie goosewebsite.auth (HttpOnly)
        A-->>B: 200 displayName, isAdmin
    end
    B->>A: GET /api/auth/me
    A-->>B: displayName, isAdmin (UI hint only)
```

### 5.2 Contact message

The link is emailed to the submitted address and never returned to the browser, so only the owner of that inbox can confirm it. Confirmation is an explicit POST from the client page, so mail link scanners that fetch the URL cannot trigger delivery.

```mermaid
sequenceDiagram
    autonumber
    participant B as Angular client
    participant C as ContactController
    participant G as AbuseGuard
    participant V as VerificationService (memory)
    participant D as DeliveryService
    participant S as MailerSend API

    B->>C: POST /api/contact {name, email, reason, message, website}
    C->>C: honeypot, required fields, reason, email format
    C->>G: TryAllow(client IP)
    alt over the limit
        C-->>B: 429
    else allowed
        C->>V: CreateVerification (30 minute token)
        C->>D: SendVerificationRequestAsync (link only, no message body)
        D->>S: confirmation template to the sender
        alt email failed
            C->>V: Discard(token)
            C-->>B: 502
        else sent
            C-->>B: 200 verificationRequired (no token or URL)
        end
    end
    Note over B: Sender opens /contact/verify#token=... and presses Confirm
    B->>C: POST /api/contact/verify {token}
    C->>V: Verify (single use)
    alt invalid, expired, or reused
        C-->>B: 400, 410, or 409
    else valid
        C->>D: SendAsync(pending message)
        D->>S: owner template to Gustavo, then receipt template to the sender
        C-->>B: 200, or 502 if delivery failed
    end
```

### 5.3 Reader registration and verification (built, disabled by default)

```mermaid
sequenceDiagram
    autonumber
    participant R as Reader
    participant B as Angular client
    participant A as AuthController
    participant M as SmtpAccountEmailSender

    R->>B: display name, email, password
    B->>A: POST /api/auth/register (CSRF, rate limited)
    A->>A: create Reader with EmailConfirmed=false
    A->>M: confirmation link with token in the URL fragment
    A-->>B: generic "check your email" (same for every case)
    R->>B: opens /verify-email#userId=...&token=...
    Note over B: Page loads without posting. The fragment is never sent to the server.
    R->>B: presses the confirm button
    B->>A: POST /api/auth/verify-email (CSRF)
    A-->>B: verified, or generic invalid or expired
```

## 6. Frontend

Angular 20 with standalone components, signals, SCSS, and the built-in router. Source: `src/GooseWebsite.Client/src/app/`.

```mermaid
flowchart TD
    root[App<br/>app.ts shell, scroll and header state]
    router{{RouterOutlet}}
    root --> router
    router -->|/sign-in| signin[SignInPage]
    router -->|/verify-email| verify[VerifyEmailPage]
    root -->|any other URL| shell[Site shell]

    shell --> header[SiteHeader]
    shell --> home[HomePage]
    shell --> blog[BlogPage]
    shell --> contact[ContactPage]
    shell --> footer[SiteFooter]

    home --> hero[Hero]
    home --> work[SelectedWork]
    home --> approach[Approach]
    home --> toolkit[Toolkit]

    signin --> authapi[AuthApi]
    verify --> authapi
    contact -->|HttpClient| apic["/api/contact"]
    blog -->|HttpClient| apib["/api/blog"]
    authapi -->|HttpClient with CSRF header| apia["/api/auth/*"]

    profile[(shared/models/profile.ts<br/>single source of profile content)]
    hero --> profile
    header --> profile
    footer --> profile
    contact --> profile
```

| Folder | Holds |
| --- | --- |
| `app/features/auth/` | `api/` (AuthApi with CSRF handling), `models/`, `pages/` (sign-in, verify-email). Authentication pages hide the public shell. |
| `app/features/home/` | The home page and its sections: `hero`, `selected-work`, `approach`, `toolkit` |
| `app/features/blog/`, `app/features/contact/` | Self-contained feature components with their own template and styles |
| `app/layout/` | `site-header`, `site-footer` |
| `app/shared/` | `models/` (profile, navigation item), `styles/site.scss` shared layout rules |
| `src/styles.scss` | Global design tokens and font families |

The public site is one long page with anchors (`home`, `work`, `approach`, `blog`, `contact`); only the authentication screens are routed URLs. The client keeps the CSRF token in memory and requests a fresh one before each mutation (`AuthApi.postWithCsrf`). The contact form and blog section call the API with plain `HttpClient`.

Build facts: output goes to `../GooseWebsite.Api/wwwroot`; production budgets warn at 500 kB and fail at 1 MB for the initial bundle; `ng serve` proxies `/api` to `https://localhost:8889` through `proxy.conf.json`.

## 7. Configuration, build, and operations

### 7.1 Configuration reference

Configuration binds in this order of precedence, last wins: `appsettings.json`, `appsettings.{Environment}.json`, user-secrets (Development only), environment variables. Nested keys use `__` in environment variables (`Email__Smtp__Host`).

| Key | Purpose | Default |
| --- | --- | --- |
| `ConnectionStrings:Default` | SQLite database; a relative path resolves against the project folder | `Data Source=Data/goosewebsite.db` |
| `Email:Smtp:Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName` | The shared SMTP sender. **Secrets: user-secrets or environment only** | empty; mail is skipped with a warning |
| `MailerSend:ApiKey`, `FromEmail`, `FromName` | MailerSend HTTP API for the templated contact emails. **The API key is a secret: user-secrets or environment only** (Docker: `MAILERSEND_API_KEY`, sender defaults to `SMTP_FROM_EMAIL`) | empty; contact emails are not sent |
| `Contact:Templates:SenderConfirmation`, `SenderReceipt`, `OwnerNotification` | MailerSend template ids (identifiers, not secrets) | the three ids in `appsettings.json` |
| `Contact:LogoUrl` | Public address of the email logo | `{PublicBaseUrl}/email/gcv-logo.png` |
| `Contact:RecipientEmail` | Inbox for verified contact messages | empty; delivery is skipped |
| `Contact:RateLimit:MaxRequestsPerWindow`, `WindowMinutes`, `MaxTrackedClients`, `MaxRequestsPerRecipient` | Contact abuse guard | 5, 10, 10000, 2 |
| `Auth:ReaderRegistration:Enabled` | Reader registration endpoints | `false`; `true` in Development |
| `PublicBaseUrl` | Base for links in emails | `https://localhost` |
| `DataProtection:KeysPath` | Cookie-key folder; must persist across restarts | `data-protection-keys` next to the project |
| `ForwardedHeaders:KnownProxies:N` | Trusted proxy IP addresses | none |

Set development secrets with `dotnet user-secrets` from `src/GooseWebsite.Api` (id `goosewebsite-api`), for example `dotnet user-secrets set "Email:Smtp:Password" "<value>"`. For Docker, copy `.env.example` to `.env`. Never commit either.

### 7.2 Build and run

| Task | Command |
| --- | --- |
| Run API (serves built client) | `dotnet run --project src/GooseWebsite.Api` (HTTPS on 8889, HTTP 8888 redirects) |
| Build client | `npm ci` then `npm run build` in `src/GooseWebsite.Client` |
| Client dev server with proxy | `npm start` in `src/GooseWebsite.Client` |
| Run all .NET tests | `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` |
| Run client tests | `npm test -- --watch=false --browsers=ChromeHeadless` |
| Provision an administrator | `dotnet run --project src/GooseWebsite.Api -- admin provision` |
| Container stack | `docker compose up --build -d` (site at `https://localhost`) |

The API project's `BuildClient` target runs `npm run build` before every build unless `-p:SkipClientBuild=true` is passed. Some integration tests request `/verify-email` and expect the SPA fallback, so build the client at least once first.

```mermaid
flowchart LR
    src[Source] --> ng[ng build<br/>stage client-build]
    ng --> wwwroot[(Api/wwwroot)]
    src --> restore[dotnet restore]
    restore --> publish[dotnet publish<br/>stage publish]
    wwwroot --> publish
    publish --> image[Runtime image<br/>aspnet 10]
    image --> compose[docker compose<br/>web plus caddy]
```

### 7.3 Operations

- **State to back up:** the `web-data` volume (`/data/goosewebsite.db` and `/data/keys`). Losing the keys signs everyone out; losing the database loses accounts and posts.
- **TLS:** Caddy uses its local CA for `localhost`. To trust it on Windows, export the root with `docker compose cp caddy:/data/caddy/pki/authorities/local/root.crt ./certs/caddy-root.crt` and import it into the current user's trusted roots. For a public site set `SITE_DOMAIN` to a real DNS name that points at the host with ports 80 and 443 open, and Caddy obtains a certificate automatically. Do not share the CA key held in the `caddy-data` volume.
- **TLS and the container's HTTP port:** the browser always talks to Caddy over HTTPS (port 443; port 80 redirects). Caddy terminates TLS with a certificate from its own local CA and forwards to the app container over the private Docker network, so the app's `Now listening on http://[::]:8888` log line is expected: port 8888 is not published to the host. Production will replace the local CA with a public one by setting `SITE_DOMAIN` to a real DNS name.
- **Trusting the local CA:** the CA lives in the `goosewebsite_caddy-data` volume, so it survives restarts but a new volume (for example after a project rename or `docker compose down -v`) creates a new CA that the machine does not trust, and the browser shows a certificate error. After that, export the root with `docker cp goosewebsite-caddy:/data/caddy/pki/authorities/local/root.crt certs/caddy-root.crt`, import it with `certutil -user -addstore Root certs\caddy-root.crt` (Windows asks for confirmation), and remove the stale root with `certutil -user -delstore Root <old thumbprint>`. Compare `openssl x509 -in certs/caddy-root.crt -noout -fingerprint -sha256` with what is trusted.
- **Renamed volumes:** the Compose project is `goosewebsite` (containers `goosewebsite-web` and `goosewebsite-caddy`, image `goosewebsite-web`, volumes `goosewebsite_web-data`, `goosewebsite_caddy-data`, `goosewebsite_caddy-config`, network `goosewebsite_app-private`). Stacks created under the earlier names `testwebsite` (volume `testwebsite_portfolio-data`, database `testwebsite.db`) or `vaninwebsite` (database `vaninwebsite.db`) keep their data in the old volumes. To keep it, stop the old stack with `docker compose -p testwebsite down` (or `-p vaninwebsite`) (never `-v`), copy the old volume's contents into `goosewebsite_web-data`, and rename the database file to `goosewebsite.db*`. An old stack left running also blocks the new one, because it holds ports 80 and 443 and the `172.30.100.0/24` subnet.
- **Logging policy:** log categories and counts only. Never log message bodies, email addresses, tokens, or passwords. Keep this when adding log lines.
- **Mail troubleshooting:** the API logs `SMTP delivery failed` with the exception type and SMTP status code. `smtpStatusCode=MustIssueStartTlsFirst` is .NET's name for any 530 reply, and it usually means the provider rejected the credentials (a `535` on AUTH). Confirm with a handshake that sends no data: `openssl s_client -connect smtp.mailersend.net:587 -starttls smtp -crlf -quiet`, then `EHLO`, `AUTH PLAIN <base64 of \0user\0pass>`, `MAIL FROM`, `RCPT TO`, `QUIT`. `235` means the credentials work. For a MailerSend trial domain, send from an address on that domain.

## 8. Conventions

- **Layout:** one module per feature under `Modules/<Name>/`, folders as in section 2. Namespaces match folders (`GooseWebsite.Api.Modules.<Name>.<Folder>`).
- **Registration:** every module exposes exactly one `Add<Name>Module(...)` extension and is wired by one line in `Program.cs`. Startup work goes in an `IModuleInitializer`; operator commands go in an `IModuleCommand`.
- **Persistence:** entities and their `IEntityTypeConfiguration<T>` live in the owning module. Query logic lives in an interface in the module's `Services/`, and every read or write goes through `DatabaseService`. Controllers and modules do not use the context or Identity managers directly. Add a generic method to `DatabaseService` rather than bypassing it.
- **Contracts:** never return an entity from an endpoint. Use a request and response type in `Contracts/` with validation attributes. Never bind privilege-bearing fields (role, `EmailConfirmed`, ids of other users) from a request.
- **Authorization:** protect every write with a policy from `AuthorizationPolicies`, not bare `[Authorize]`, and add `[ValidateAntiForgeryToken]` to cookie-authenticated mutations.
- **Configuration:** bind settings to an options class (see `SmtpOptions`, `ContactOptions`). Do not read `IConfiguration` ad hoc in new code, and never hard-code addresses or credentials.
- **Mail:** account emails are built in the module and sent through `ISmtpMailSender`. Contact emails are MailerSend templates sent through `IMailerSendClient`: the module picks the template id and supplies every variable it reads (case-sensitive names, constants in `ContactEmailTemplateVariables`), and a test pins them. A variable the request omits renders as its own name in the inbox.
- **Style:** C# with nullable references enabled, file-scoped namespaces, primary constructors for dependency injection, `sealed` classes by default, a short comment saying why a type exists. TypeScript uses standalone components, signals, and typed models; one feature per folder under `app/features/`.
- **Naming:** `I<Name>` for seams, `<Name>Module`, `<Name>Controller`, `<Name>Repository`, `<Name>Configuration` for EF mappings.

### 8.1 Pitfalls found in review (do not repeat)

Each item is a defect that a code review found in this repository. Check new code against the list before it is committed.

| Pitfall | What went wrong (found 2026-10-02, F2-US2) | Rule |
| --- | --- | --- |
| Consuming a one-time token before the side effect succeeds | `Verify` removed the token, then delivery to Gustavo failed. The client was told to try again, but the retry was rejected and the message was lost | If a failure response tells the caller to retry, the retry must work. Restore the state (`Restore`) or consume the token only after the side effect succeeds. Test the failure, then the retry |
| A public endpoint that emails a caller-supplied address | The contact form emailed any address, limited only per IP, so it could flood a third party from Gustavo's sender | Rate limit by the target address as well as the caller. Keep only a hash of the address. Test the limit, and that a different address is unaffected |
| Cleanup skipped on an exception | Pending data was discarded only when the send returned `false`, not when the mail library threw on a malformed address | Treat exceptions from the mail library as failed sends, and clean up in the same path. Test the throwing case |
| Returning secrets to the caller | The verification token and URL were returned in the API response, so anyone could verify any address | The token goes only to the channel it proves, such as the inbox. An API test must assert it is absent from the response body |
| State change on `GET` | A verify link that changed state would be triggered by mail scanners | State changes use `POST` after an explicit user action |
| Secrets or mail settings in `appsettings.json` | Working MailerSend credentials were placed in `appsettings.json` under a `MailerSend:Smtp` section the code never reads, while the values the app did read (user-secrets) were stale, so every send failed with `535` | Mail settings live only under `Email:Smtp:*` in user-secrets or environment variables, never in a tracked file (CLAUDE.md rule 4). When a setting seems ignored, check the key name against `SmtpOptions.SectionName` |
| Logging only the exception type for a failure | `SmtpException` alone hid the cause, and .NET reports every SMTP 530 reply as `MustIssueStartTlsFirst`, which misled the diagnosis | Log safe categories that identify the cause (exception type, SMTP status code). To diagnose an SMTP failure, replay the handshake (EHLO, AUTH, MAIL FROM, RCPT TO) without sending data |
| Template variables that do not match the request | A MailerSend template renders any variable the request omits (or spells differently) as its own name, for example `{{name}}` | Keep the variable names in one constants class, document them next to the templates, and pin them with a test that checks every variable for every template. Variable names in the MailerSend editor must be copied exactly |
| Check-then-act on a one-time token | `Verify` looked the token up, then removed it, so two simultaneous confirmations both succeeded and the message was delivered twice | Claim the token with one atomic operation (`TryRemove`) and act only if it returned the value. Test with many parallel requests and assert exactly one success |
| Retrying a multi-step action from the start | A failed receipt made the retry notify Gustavo a second time | Record how far an attempt got (`ContactDeliveryOutcome`, `OwnerNotified`) and resume from there. Test the partial failure and the retry |
| Backlog status from reading code | F2-US2 was recorded Done before the browser flow was exercised | Run the acceptance tests and a runtime check before marking Done (CLAUDE.md rule 7) |

## 9. How to add, change, or remove a feature

### Add a feature module (example: Comments)

1. Create `Modules/Comments/` with `Controllers/`, `Contracts/`, `Domain/`, `Services/` as needed.
2. Add the entity and a `CommentConfiguration : IEntityTypeConfiguration<Comment>` in `Domain/`. `ApplicationDbContext` finds it automatically.
3. Put the module's query logic behind `ICommentRepository` in `Services/`; it reads and writes only through `DatabaseService`.
4. Protect endpoints with `AuthorizationPolicies.VerifiedReader` (and CSRF on writes). Do not copy role checks.
5. Create `CommentsModule.cs` with `AddCommentsModule(...)` that registers the services (and an `IModuleInitializer` if needed).
6. Add one line to `Program.cs`: `builder.Services.AddCommentsModule();`.
7. Add tests in `tests/GooseWebsite.Api.Tests/`. Add a client feature under `app/features/comments/`.
8. Update this document (sections 3, 4, and the matrix) and the backlog evidence.
9. Until TE1 lands, a new table needs the migration work first; `EnsureCreated` will not add it to an existing database.

### Change a feature

Read its module's section here and its stories in the backlog. Keep changes inside the module and its contracts. If another module must change, the dependency rules in section 3.1 are probably being bent; stop and reconsider.

### Remove a feature

Delete the module folder, its registration line in `Program.cs`, its tests, and its client folder. `DatabaseService` and the other modules do not need edits (Blog is the only one that depends on another module, Accounts). Removing Accounts also removes the policies Blog uses, so remove or rework Blog first.

## 10. Testing

| Level | Location | What it covers |
| --- | --- | --- |
| Integration (host in memory) | `tests/GooseWebsite.Api.Tests/AuthApiIntegrationTests.cs`, `AuthApiFactory.cs` | Login, CSRF, cookie flags, lockout, expiry, registration, verification, authorization, rate limit. The factory swaps in an in-memory SQLite connection and a test mail sink. |
| Unit and service | `AdminProvisioningTests.cs`, `ContactFeatureTests.cs`, `SchemaCompatibilityTests.cs` | Provisioning rules; contact abuse guard (sequential, concurrent, bounded, expiry), honeypot, log privacy; pinned table and index names so existing databases keep working |
| Client | `*.spec.ts` beside components | Header sign-in link; verify-email page does not post until confirmed |

Test doubles: `TestAccountEmailSender` captures verification links; `StubContactEmailDeliveryService` and `StubContactVerificationService` replace contact collaborators; `AdjustableTimeProvider` and `CapturingLogger` support time and log assertions.

Current result (2026-10-01): 20 API tests and 2 client tests pass. Not covered: contact controller end to end through HTTP, blog endpoints, and every browser-level acceptance test (320 px, keyboard, contrast); see enabler TE5.

## 11. Known gaps and technical debt

Each item is also tracked in [project.md](./project.md) or the backlog.

| Gap | Where | Consequence | Tracked as |
| --- | --- | --- | --- |
| `EnsureCreated` instead of migrations | `Shared/Persistence/DatabaseInitializer.cs` | No safe schema evolution | TE1 |
| Pending contact messages are in process memory | `ContactVerificationService` | Lost on restart; single instance only | Accepted for R1 |
| Blog section ships hard-coded sample posts, the "selected work" section is placeholder, and the local dev database holds two demo posts | `blog-page.ts`, `selected-work` | Violates the R1 "no sample content" decision. Gustavo: leave until just before deployment | TE4 (deployment gate) |
| `DatabaseService` applies no input sanitization or access checks yet | `Shared/Persistence/DatabaseService.cs` | Safeguards must be repeated by callers until added | F7-US1 |
| No reader display-name update, password reset, or administrator recovery | Accounts | Recovery paths missing | F3-US3, F3-US4 |
| `SQLitePCLRaw.lib.e_sqlite3` advisory warning (NU1903) | `GooseWebsite.Api.csproj` | Known vulnerability in a transitive package | TE3 |
| No CI | repository | Regressions are not caught automatically | TE2 |
| Blog HTTP behavior and browser-level contact flow lack tests | `tests/` | Contact API behavior is covered by `ContactVerificationApiTests`; the rest is not | TE5 |
