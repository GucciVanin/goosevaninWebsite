# GooseWebsite Architecture

**Audience:** AI agents and developers. Read this before implementing a feature or changing existing behavior.  
**Also for:** Gustavo, to see the product as it is built today. Diagrams are Mermaid and render in VS Code, GitHub, and most Markdown viewers.  
**Last verified against the code:** 2026-10-29 (Subscriptions module, F10-US1)

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
│   │   │   ├── Contact/
│   │   │   ├── Media/
│   │   │   ├── Subscriptions/
│   │   │   └── Projects/
│   │   ├── Shared/                   Cross-cutting infrastructure (section 3.2)
│   │   │   ├── Announcements/  Email/  Hosting/  Modules/  Persistence/
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
        email[Email<br/>IMailService]
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
- **Contact** depends on `Shared` only. **Accounts** depends on `Shared`. **Media** depends on `Shared` only. **Subscriptions** depends on `Shared` only. **Blog** and **Projects** depend on `Shared` and on `Accounts` (for the author entity and the authorization policy names); they do not depend on each other. Do not add the reverse direction. If two modules need the same thing, move it into `Shared`.
- **Announcing new work (F10-US3) crosses modules without a dependency.** Blog and Projects must tell the subscribers' module that something was published, but they may not reference Subscriptions (and Subscriptions must not reference them). So the seam is owned by `Shared`: `Shared/Announcements/IContentAnnouncer` (one method, `AnnounceAsync(ContentAnnouncement)`; the record carries only public facts: kind, content id, title, summary text, public path). Blog and Projects call it after the first publish and know nothing about who listens. Subscriptions implements it (`ContentAnnouncer`) and registers it with `Replace`; Blog and Projects register a quiet `NoContentAnnouncer` with `AddContentAnnouncerFallback()` (`TryAdd`), so either module order works and the site still publishes if Subscriptions is ever removed. This is the same pattern as `IMailService`: Shared owns the interface, a module owns the behavior. `AnnouncementsApiTests.BlogAndProjectsDoNotReferenceTheSubscriptionsModule...` guards it. A direct call, a Blog-to-Subscriptions reference, or a domain-event bus were rejected: the first two break the rule above, and a bus is more machinery than one interface needs.
- `Shared` never references a module, with one deliberate exception: `ApplicationDbContext` and `DatabaseService` know `ApplicationUser` from Accounts, because Identity shares the database. Every other module table is discovered through `IEntityTypeConfiguration<T>` without `Shared` naming it.
- **All database reads and writes go through `DatabaseService`.** A module keeps its own query logic in a service or repository (what to ask for) but calls the gateway (how to ask). Never inject `ApplicationDbContext`, `UserManager`, or `RoleManager` into a module class other than Identity registration. The sanitization, access rules and one-click-one-unit-of-work guarantee (F7-US1) are applied beneath the gateway, so they hold for every module; declare an `EntityAccessRule<T>` next to every new entity (see section 4).

### 3.2 Shared layer

| Component | What it does | Used by |
| --- | --- | --- |
| `Shared/Persistence/DatabaseService` | **The single gateway for every database read and write.** Generic entity access (`Read<T>`, `ReadForUpdate<T>`, `Add`, `Remove`, `SaveChangesAsync`, `CanConnectAsync`) plus the account store (users, roles). There is no find-by-key, because it would skip the read filters. Modules never use `ApplicationDbContext`, `UserManager`, or `RoleManager` directly (an architecture test enforces it). The safeguards live beneath it, in the context. | Accounts, Blog |
| `Shared/Persistence/ApplicationDbContext` | The EF Core context behind the gateway. Applies every `IEntityTypeConfiguration<T>` found in the assembly, builds each entity's read filter from its `EntityAccessRule<T>`, and runs `DataSafeguardsInterceptor` on every save. | `DatabaseService` only |
| `Shared/Persistence/PersistenceServiceCollectionExtensions` | `AddSharedPersistence`: registers SQLite and resolves a relative database path against the content root, so the working directory never matters | `Program.cs` |
| `Shared/Persistence/DatabaseInitializer` | First `IModuleInitializer`: applies EF Core migrations, adopting a database that `EnsureCreated` made earlier | startup |
| `Shared/Modules/IModuleInitializer` | A module's startup hook (for example, create roles). Run in registration order after the database exists. | Accounts, Persistence |
| `Shared/Modules/IModuleCommand` | A module's operator command-line entry point. If one handles the arguments, the web host does not start. | Accounts (`admin provision`, `admin reset-password`) |
| `Shared/Announcements/IContentAnnouncer` | The seam for "something was published for the first time" (F10-US3, see 3.1): the `ContentAnnouncement` record, the interface, and the quiet `NoContentAnnouncer` fallback (`AddContentAnnouncerFallback`). | Blog, Projects (call it); Subscriptions (implements it) |
| `Shared/Email/IMailService` (`MailService`) | The only mail interface modules use (TE8). `SendAsync(RenderedMail)` sends content the caller rendered (recipient, subject, HTML, plain text, and optional extra headers such as `List-Unsubscribe`, written by `SmtpMailTransport.CreateMessage`) over SMTP; `SendTemplateAsync(TemplateMail)` sends a MailerSend template (template id plus variables). Returns a `MailResult` (success or a `MailFailure` category) and never throws. It is also the one place mail is logged: categories only (kind, transport, failure, HTTP or SMTP status, exception type), never an address, name, subject, body, link, token, key or provider response text. `MailServiceTests` captures the log for success, failure, unconfigured and throwing transports. | Accounts, Contact, Subscriptions |
| `Shared/Email/SmtpMailTransport`, `MailerSendTransport` | The two transports behind `IMailService` (`IRenderedMailTransport`, `ITemplateMailTransport`). Each is the only class that talks to its provider; they never log and report back a failure category plus log-safe diagnostics. Modules must not reference them (`MailServiceTests.NoModuleTalksToSmtpOrMailerSendDirectly`). | `IMailService` only |
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
| Reader accounts | `Services/IReaderAccountService.cs`, `ReaderAccountService.cs` | Registration, resend and email Verification return neutral outcomes (the controller maps them), so nothing reveals whether an address has an account. A role assignment that fails (or throws because the role is missing) removes the new account again |
| Verification email | `Services/IAccountEmailSender.cs`, `AccountEmailSender.cs`, `AccountVerificationEmailRenderer.cs` | Separate from contact delivery on purpose. Sends a multipart message: branded HTML (tables and inline styles, no remote resources except the logo at `{PublicBaseUrl}/email/gcv-logo.png`, built with `PublicLinkBuilder.SiteLink`) plus a plain-text alternative. The display name is HTML-encoded in the HTML view. See `Docs/email-templates/README.md` |

Security settings in force: unique email, password minimum 12 characters with no composition rule, lockout after 5 failures for 15 minutes, email confirmation required to sign in, confirmation token lifetime 24 hours, cookie `goosewebsite.auth` (HttpOnly, SameSite=Lax, Secure always in Production, 30-minute sliding expiry, not persistent), antiforgery cookie `goosewebsite.csrf` with header `X-CSRF-TOKEN`. A sign-in marker cookie `goosewebsite.signedin` (F9-US7) is set with the value `1` when sign-in succeeds and cleared at sign-out; it is the one cookie that is deliberately not HttpOnly (Secure, SameSite=Lax, path `/`, no expiry, no identity or token in it) because the client reads it to decide whether to call `GET /api/auth/me`; it is a hint only and grants nothing. Cookie events return 401 and 403 rather than redirecting. Signing out also rotates the account's security stamp, so a session cookie captured before sign-out stops working too (cookies are checked against the stamp on every request).

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /api/auth/csrf` | anonymous | Issue antiforgery cookie and request token |
| `GET /api/auth/reader-registration` | anonymous | Whether reader registration is enabled |
| `POST /api/auth/login` | anonymous, CSRF | Sign in; same generic error for unknown, wrong, locked, or unconfirmed |
| `POST /api/auth/logout` | signed in, CSRF | Sign out |
| `GET /api/auth/me` | signed in | Display name and server-derived `isAdmin` (the client calls it only while the sign-in marker cookie is present) |
| `POST /api/auth/register` | anonymous, CSRF, rate limited | Create unconfirmed Reader; always the same response. Returns 404 unless `Auth:ReaderRegistration:Enabled` is true |
| `POST /api/auth/verification/resend` | anonymous, CSRF, rate limited | Resend confirmation; generic response |
| `POST /api/auth/verify-email` | anonymous, CSRF, rate limited | Confirm with `userId` and `token` from the link |

### 3.5 Contact module

Owns the public contact form. Source: `Modules/Contact/`. It keeps no durable data.

| Piece | File | Notes |
| --- | --- | --- |
| Registration | `ContactModule.cs` | Binds `ContactOptions`; singletons for the token store and abuse guard; the module interface `IContactSubmissionService` |
| Module interface | `Services/IContactSubmissionService.cs`, implemented by `Services/ContactSubmissionService.cs` | `SubmitAsync(message, clientKey)` and `VerifyAsync(token)` return outcomes (`ContactSubmitStatus`, `ContactVerifyStatus`). It owns the order of checks, discarding a Pending message when the link cannot be sent, restoring it after a failed delivery (remembering that Gustavo was already notified), and the logging policy |
| Endpoints | `Controllers/ContactController.cs` | Maps outcomes to status codes only. `POST /api/contact` (emails the link to the sender), `POST /api/contact/verify` (body `{token}`; the emailed link opens the client page `/contact/verify#token=...`) |
| Abuse guard | `Services/ContactSubmissionAbuseGuard.cs` | Thread-safe sliding windows in two separate bounded stores: per socket IP, and per submitted address (stored only as a SHA-256 hash, case-insensitive). Configured under `Contact:RateLimit` |
| Pending store | `Services/ContactVerificationService.cs` | In-memory, 30-minute lifetime, single use, removed on use or expiry |
| Delivery | `Services/ContactEmailDeliveryService.cs` | Three MailerSend templates through `IMailService.SendTemplateAsync`: the confirm link to the sender, then the notice to Gustavo, then the receipt to the sender. Template ids come from `Contact:Templates`; the variables each template needs are in `Models/ContactEmailTemplateVariables.cs` and pinned by `ContactEmailTemplateTests` |

Validation on `POST /api/contact` (performed by `ContactSubmissionService`, which the controller calls): honeypot field `website` must be empty, all four fields required, `reason` must be `Work or collaboration` or `Personal note`, email must be well formed, then the rate limit applies (HTTP 429 before any token is created). Logs carry the reason and a client key, never the sender's address or the message body.

### 3.6 Blog module

Owns stories (the product name; the code keeps its historical "blog post" name, table `BlogPosts` and route `/api/blog`). Source: `Modules/Blog/`. A story body is Markdown, shown safely; drafts are never public.

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /api/blog?search&category&page&pageSize` | public | Published stories only, newest publication date first; no upper limit on `pageSize` (only the internal stories index uses it, asking for every story); `pageSize` below 1 becomes 1, default 12 |
| `GET /api/blog/{slug}` | public | One published story (a draft is a 404) |
| Public response shape | public | Both public reads return `PublicStoryResponse` (R1-G2, W1): `title, slug, excerpt, content, category, tags, coverImage, coverImageAlt, createdAtUtc, publishedAtUtc`. No `id`, `authorId`, `authorName`, `isPublished` or `updatedAtUtc`. The editor endpoints keep returning `BlogPostResponse` (with `id`, `authorId`, `authorName`, state). Tests: `PublicContractTests`, `verify/runtime/70-blog.sh` |
| `GET /api/editor/stories` | `AdminOnly` | The workspace list: every story, drafts included, without the body |
| `GET /api/editor/stories/{id}` | `AdminOnly` | One story in full, drafts included (the preview source) |
| `POST /api/editor/stories` | `AdminOnly`, CSRF | Create a **draft** (always a draft, whatever the request says); records the author and makes a unique slug |
| `PUT /api/editor/stories/{id}` | `AdminOnly`, CSRF | Save edits. Never publishes or unpublishes, never changes the slug; may set the publication date |
| `POST /api/editor/stories/{id}/publish` | `AdminOnly`, CSRF | Publish once: needs an excerpt and text; records the publication date if there is none; repeating changes nothing. The first publish also sets `AnnouncedAtUtc` and queues the announcement to subscribers (see 3.6c); nothing else does |
| `POST /api/editor/stories/{id}/unpublish` | `AdminOnly`, CSRF | Return to draft, keeping the publication date |
| `DELETE /api/editor/stories/{id}` | `AdminOnly`, CSRF | Delete for good |

`BlogController` (public reads) and `StoryEditorController` (everything that changes or lists drafts) are thin: the rules live in `IBlogAuthoringService` (`BlogAuthoringService`, TE9), which returns an outcome (`Succeeded`, `NotFound`, `Invalid` with the failing fields) that the controller maps to a response. The service owns: the slug (made from the title with letters and digits only, accents dropped, `story` when nothing is left, numeric suffix on a collision, never changed afterwards so links stay stable), the tags (trimmed, no empty tags, no duplicates that differ only in case, no commas inside a tag), the timestamps, and the rules for publishing. Saving a draft needs only a title; publishing needs an excerpt and text. Query logic is in `IBlogPostRepository` (`BlogPostRepository`), which reads and writes only through `DatabaseService`; the access rule (everybody reads published stories, administrators read drafts and write, no length limit) is `Domain/BlogPostAccessRule.cs` and the table mapping `Domain/BlogPostConfiguration.cs`. Stories published before `PublishedAtUtc` existed received their creation date in the `AddStoryPublishedAt` migration.

In the client (`app/features/editor/`, loaded on demand so visitors never download it) `StoryFormPage` is the form: save draft, preview (Markdown rendered by `markdown-it` with raw HTML off), publish, return to draft, with the status written in words. While a story is not published, a visible reminder above the buttons asks Gustavo to review it for personal or sensitive details; it informs only and never disables or replaces the explicit Publish action (F5-US2).

### 3.6a Projects module

Owns projects (title, overview in Markdown, Gustavo's contribution, technologies, optional demo and source links). Source: `Modules/Projects/`. It follows the same pattern as stories: drafts are never public, saving never publishes, and publishing is explicit.

| Method and path | Who | What |
| --- | --- | --- |
| `GET /api/projects`, `GET /api/projects/{slug}` | Everyone | Published projects only (a draft is a 404). They return `PublicProjectResponse` (`title, slug, overview, contribution, technologies, demoUrl, sourceUrl, coverImage, coverImageAlt, publishedAtUtc`): no `id`, `isPublished`, `createdAtUtc` or `updatedAtUtc`; the editor keeps `ProjectResponse`. Tests: `PublicContractTests`, `75-projects.sh` |
| `GET /api/editor/projects`, `GET /api/editor/projects/{id}` | Administrator | List (summaries) and open any project, drafts included |
| `POST /api/editor/projects`, `PUT /api/editor/projects/{id}` | Administrator + CSRF | Create or save; always leaves the state as it was (new = draft) |
| `POST /api/editor/projects/{id}/publish`, `.../unpublish` | Administrator + CSRF | Explicit state changes; publishing needs an overview and is idempotent |
| `DELETE /api/editor/projects/{id}` | Administrator + CSRF | Removes the project |

`IProjectAuthoringService` owns the rules (title required; links optional but only absolute `http` or `https` addresses; technologies trimmed, de-duplicated by case, no commas; slug from `Shared/Text/Slug` fixed at creation with a numeric suffix on a collision; publication date set at first publish). Queries are in `IProjectRepository`; the access rule is `Domain/ProjectAccessRule.cs`; table `Projects` (migration `AddProjects`). The client has `ProjectFormPage` and `ProjectApi` beside the story ones, and the workspace (`EditorPage`) switches between the two lists.

**Announced once (F10-US3).** Stories (`BlogPosts`) and projects (`Projects`) have a nullable `AnnouncedAtUtc` marker (migration `AddAnnouncements`). The publish action sets it on the first transition to published (`post.AnnouncedAtUtc is null`) and calls `IContentAnnouncer` in the same request; it is never cleared. Saving a draft, editing, unpublishing, publishing a published item again and publishing again after unpublishing do not announce. The marker is not part of any response contract. The migration marks everything already published, or published once and pulled back, as announced (`COALESCE(PublishedAtUtc, CreatedAtUtc)`), so old work is never mailed by accident. Stories: summary = the excerpt, link = `/stories/{slug}`; projects: summary = the overview, link = `/projects/{slug}`.

### 3.6b Media module

Owns image uploads (F4-US5). Source: `Modules/Media/`. Files live in `Media:Directory` (`/data/uploads` in Docker, `uploaded-images` beside the app in development) under generated names; table `ContentImages` records them (readable by administrators only).

| Method and path | Who | What |
| --- | --- | --- |
| `POST /api/editor/images` | Administrator + CSRF | Multipart upload of one file; JPEG, PNG, WebP or GIF by its first bytes, at most 5 MB (413 over); returns the reference `/uploads/<name>` |
| `GET /api/editor/images` | Administrator | Lists uploaded images |
| `DELETE /api/editor/images/{id}` | Administrator + CSRF | Removes the row and the file |
| `GET /uploads/<name>` | Everyone | Static file, `nosniff`; only files the module wrote exist |

Stories and projects store only a reference of the form `/uploads/<32 hex>.<jpg|png|webp|gif>` (`Shared/Text/UploadedImageReference` checks it), so they do not depend on this module. Unreferenced images are kept until Gustavo deletes them. Deleting an image does not check whether a story or project still uses it (known gap, found in review; it matters once a screen for deleting images exists, and a cover is checked only for its form, not for the file existing). The client has `ImageApi` and the `ImageControls` component used by both forms.

### 3.6c Subscriptions module

Owns the subscriber list (F10): a visitor leaves a name and an email, confirms through an emailed link (double opt-in), can leave from any mail with one click (F10-US2), and is told once about each new story or project (F10-US3). Source: `Modules/Subscriptions/`. It depends on `Shared` only (persistence, mail, the public link builder) and registers with `AddSubscriptionsModule(configuration)`.

| Piece | File | Notes |
| --- | --- | --- |
| Entity | `Domain/Subscriber.cs`, `SubscriberConfiguration.cs` | Table `Subscribers` (migration `AddSubscribers`): `Id, Name (100), Email (254, NOCASE, unique), State (Pending or Confirmed), CreatedAtUtc, ConfirmedAtUtc, ConfirmationTokenHash, ConfirmationTokenExpiresAtUtc` (the O1 column `UnsubscribeTokenHash` was dropped by migration `DropSubscriberUnsubscribeTokenHash`). No IP address and no user agent. The confirmation token is 32 random bytes (URL-safe) and only its SHA-256 hash (upper-case hex) is stored; the unsubscribe token is not stored at all (see Unsubscribe links) |
| Access rule | `Domain/SubscriberAccessRule.cs` | `WriteAccess.Anyone` (an anonymous request writes), `Name` and `Email` are user text with limits 100 and 254. The rows have no read filter, because the anonymous service must look an address up; no endpoint returns them, and `SubscriptionsApiTests.NoEndpointListsSubscribers` guards that |
| Endpoints | `Controllers/SubscriptionsController.cs` | `POST /api/subscriptions` (body `{name, email, website}`; anonymous, CSRF, rate limited per client address) and `POST /api/subscriptions/confirm` (body `{token}`; anonymous, CSRF, rate limited). Subscribe answers `200` with one fixed message for every outcome (new, pending, confirmed, invalid input, honeypot, mail failure); only the rate limit differs (`429`). Confirm answers `200` or `400` with one fixed message. `POST /api/subscriptions/unsubscribe` (body `{token}`; anonymous, CSRF, rate limited with the confirm policy) deletes the subscriber and answers `200`, or `400` with one fixed message for every token that is invalid, forged, used or whose subscriber is gone. `POST /api/subscriptions/unsubscribe/one-click?token=...` is the RFC 8058 target of the `List-Unsubscribe` header: no CSRF token (mail clients have no session; the sealed token is the credential), the form body must be `List-Unsubscribe=One-Click`, same answers; it has its own, higher rate limit (mail providers post for many subscribers from a few addresses) and a tiny request and form limit (`[RequestSizeLimit(1024)]`, at most 2 form fields of 64 characters), because it is anonymous and parses a form. The token is in the query string (RFC 8058 needs a plain URL), so **the query string of `/api/subscriptions/unsubscribe/one-click` must never be logged**: no access log in Caddy, no HTTP logging middleware, no external proxy log format that includes the query (`TheOneClickQueryStringIsNeverLogged...` checks the app log, the `Caddyfile` and `Program.cs`). There is no GET handler: a mail scanner that fetches either address changes nothing |
| Rules | `Services/ISubscriptionService.cs`, `SubscriptionService.cs` | A filled honeypot (`website`) or invalid name or email stores nothing and sends nothing. New address: pending record plus one confirmation email. Pending address: a fresh token (the old link stops working) and a fresh email, but not within `ResendCooldownMinutes` (2) of the previous mail, so the form cannot flood an inbox; no second record. Confirmed address: no mail. A race on the unique index looks the same to the caller. Confirm: the hash must match a pending record whose expiry has not passed; the token is cleared at once, so a link works once. Logs carry the outcome category only |
| Unsubscribe links | `Services/ISubscriptionLinks.cs`, `SubscriptionLinks.cs` | The one place that makes and reads unsubscribe tokens, reusable by every mail the feature sends (F10-US3 calls it per subscriber): `UnsubscribePageUrl(id)` (`{PublicBaseUrl}/unsubscribe#token=...`), `OneClickUnsubscribeUrl(id)`, `UnsubscribeHeaders(id)` (`List-Unsubscribe` and `List-Unsubscribe-Post`) and `TryReadSubscriberId(token)`. The token is the subscriber id sealed with the ASP.NET Data Protection key ring (`IDataProtector`, purpose `GooseWebsite.Subscriptions.Unsubscribe.v1`): nothing is stored, only this server can make or read one, a tampered or foreign token fails the integrity check (compared in constant time inside Data Protection), and it stays valid until the subscriber is deleted (key rotation keeps old keys for reading). The ring is persisted on the data volume (`DataProtection:KeysPath`, `/data/keys` in Docker, shared with the cookie and antiforgery keys), so links survive restarts; losing the volume invalidates every outstanding link. Ids are never reused (SQLite autoincrement), so a link for a deleted subscriber cannot reach a later subscriber. Unlike a random token, two mails to one subscriber carry different (equally valid) tokens |
| Email | `Services/SubscriptionEmailSender.cs`, `SubscriptionEmailRenderer.cs` | Branded HTML plus plain text through `IMailService.SendAsync`, built like the account email (`Accounts/Services/AccountVerificationEmailRenderer`). The link is `{PublicBaseUrl}/subscribe/confirm#token=...` (the token is in the fragment, like the other emailed links). The name is HTML-encoded in the HTML view. Every subscription mail carries the unsubscribe link in the body and the `List-Unsubscribe` and `List-Unsubscribe-Post` headers (`RenderedMail.Headers`). The subscriber name is cleaned of control characters, line and paragraph separators (Zl, Zp) and format characters (Cf: bidirectional overrides, zero-width marks) before it is stored or put in a mail |
| Cleanup | `Services/PendingSubscriberCleanupService.cs` | Hosted `BackgroundService`: 30 seconds after start and then every hour it deletes pending records whose confirmation link expired (`RemoveExpiredPendingAsync`, 7 days after the latest mail). Confirmed records are never removed by it |
| Announcements | `Domain/Announcement.cs`, `AnnouncementConfiguration.cs`, `Services/ContentAnnouncer.cs` | Tables `Announcements` (`Kind, ContentId` unique, `Title`, `Summary`, `PublicPath`, `CreatedAtUtc`, `CompletedAtUtc`) and `AnnouncementDeliveries` (`AnnouncementId, SubscriberId, SentAtUtc, Attempts, LastAttemptAtUtc`; unique per announcement and subscriber; cascade-deleted with the announcement **and with the subscriber**). Only public facts and ids and counters: **no name, no address**. `ContentAnnouncer` runs in the publish request, after the item's own save and in the same unit of work, so the publish and its queue are committed together or not at all. It stores one delivery row per **confirmed** subscriber at that moment (pending ones never; someone who confirms later is not told about earlier work; someone who unsubscribes first loses the row with the record). It sends nothing. The title and the summary are made plain text (`Services/PublicText`: Markdown markers, links and images reduced to their words, control, line-separator and format characters removed, one line, cut to `Announcements:SummaryMaxLength` characters) before they are stored, so the summary goes through the same safety as the public site (which shows an excerpt as escaped text and an overview as Markdown without raw HTML) and can never become a link or markup in the mail |
| Delivery | `Services/AnnouncementDispatcher.cs`, `AnnouncementDeliveryService.cs` | The hosted `AnnouncementDeliveryService` wakes every `Announcements:PollSeconds` (20) and calls `IAnnouncementDispatcher.DeliverPendingAsync`; the publish request never waits for a provider. A round lists the owed deliveries that are due, and for each one, in its own scope: re-reads the recipient (someone who unsubscribed in the meantime gets nothing), sends one personal mail through `IMailService`, and saves the result before the next recipient. A failed recipient waits `RetryDelayMinutes` (10) times the attempts so far and is given up on after `MaxAttempts` (5); recipients already reached are never selected again, so a retry cannot duplicate. An announcement is closed (`CompletedAtUtc`) when nobody is left to try. Delivery is at least once: a crash between the provider accepting a mail and the row being saved mails that one recipient again on the next round. A provider failure is a category in the log (`MailService` logs it) and a count in `Announcement delivery round finished; sent=..; failed=..`; nothing names a person. Queue rows survive a restart because they are in the database |
| Announcement email | `Services/AnnouncementEmailRenderer.cs`, `BrandedEmailLayout.cs`, `SubscriptionEmailSender.SendAnnouncementAsync` | Subject `New story: <title>` or `New project: <title>`; branded HTML plus plain text in the shared frame (`BrandedEmailLayout`, also used by the confirmation mail); title, summary, a button and the plain public link (`PublicBaseUrl` + the path), the recipient's own unsubscribe link in the footer and the `List-Unsubscribe` and `List-Unsubscribe-Post` headers from `ISubscriptionLinks`. Everything a person wrote is HTML-encoded and appears only as text |
| Key ring guard | `Services/DataProtectionKeyRingGuard.cs` | An `IModuleInitializer`: at startup, if the Data Protection key ring has no usable key while `Subscribers` rows exist, it logs one warning (no count, no personal data) that the unsubscribe links already mailed will not work until the key directory is restored (S1, see 7.3) |
| Settings | `Models/SubscriptionOptions.cs` | Section `Subscriptions`: `ConfirmationLifetimeDays` (7), `ResendCooldownMinutes` (2), `RateLimit:PermitLimit` (5 subscribe requests per client address per window), `RateLimit:ConfirmPermitLimit` (20, confirm and the page unsubscribe), `RateLimit:OneClickPermitLimit` (120, the one-click address only), `RateLimit:WindowMinutes` (10), and `Announcements:PollSeconds` (20), `MaxAttempts` (5), `RetryDelayMinutes` (10), `SummaryMaxLength` (300). The policies are `subscriptions-subscribe`, `subscriptions-confirm` and `subscriptions-one-click` |

### 3.7 Authorization matrix

| Operation | Anonymous | Reader, unconfirmed | Reader, confirmed | Admin |
| --- | --- | --- | --- | --- |
| Read published blog posts | yes | yes | yes | yes |
| Submit contact form | yes (rate limited) | yes | yes | yes |
| Subscribe, confirm a subscription (F10) | yes (rate limited, CSRF) | yes | yes | yes |
| Unsubscribe (F10-US2): `POST /api/subscriptions/unsubscribe` and the one-click address | yes (rate limited; CSRF, except the one-click address, which needs the sealed token and the fixed body) | yes | yes | yes |
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

Subscriptions add `Subscribers`, `Announcements` and `AnnouncementDeliveries` (section 3.6c), and `BlogPosts` and `Projects` carry a nullable `AnnouncedAtUtc`. Identity also creates its standard claim, login, and token tables; they are omitted here. Contact messages are **not** stored. The pending verification dictionary exists only in process memory.

The schema is built by EF Core migrations (`Shared/Persistence/Migrations/`), applied at startup by `DatabaseInitializer`. A database created before migrations existed (by `EnsureCreated`) has no migration history; the initializer records the first migration (`InitialCreate`) as applied and leaves its data alone. The baseline assumes that database matches the model as of `InitialCreate`; if the key tables are missing, startup stops with an error instead of adopting it. Copy `goosewebsite.db` before the first start of a version that applies a migration to real data. Add a change with `dotnet dotnet-ef migrations add <Name> --project src/GooseWebsite.Api --output-dir Shared/Persistence/Migrations` (the tool is pinned in `dotnet-tools.json`; run `dotnet tool restore` once). `DatabaseInitializerTests` fails if the model and the migrations disagree.

### Data access safeguards (F7-US1)

The rules are beneath `DatabaseService`, in `Shared/Persistence`, where no caller can switch them off. Each entity declares an `EntityAccessRule<T>` next to its mapping (found by assembly scan, like the configurations); an architecture test fails if an entity has none or if a module mentions the context, the Identity managers, `IgnoreQueryFilters`, `ExecuteUpdate`, `ExecuteDelete`, `FromSql` or `ExecuteSql` (the set-based and raw-SQL calls would skip the write checks).

| Piece | Behavior |
| --- | --- |
| Caller (`ICurrentCaller`) | For a web request: the signed-in user from the session cookie (`Anonymous`, `Authenticated` Reader, `Administrator`). Outside a request (startup, migrations, operator commands): `System`. Modules never pass identity by hand |
| Read | A rule can name the rows everybody may read (`VisibleWhen`). Administrators and the system read all rows. Blog posts: everybody reads published posts, drafts only administrators. Implemented as an EF query filter, so every query obeys it |
| Write | `WriteAccess.Administrator` (blog posts), `Anyone` (Identity tables and users: written by the Identity managers, including for anonymous visitors who register or fail a sign-in), `System`. A refused write throws `DataAccessDeniedException`; no rule at all is refused for everyone but the system |
| Text | Properties declared `IsUserText` are trimmed and cleaned of control characters (line breaks and tabs stay); a declared `maxLength` rejects longer text with `DataRejectedException`. Blog posts have no limit (only Gustavo writes); a user's display name is limited to 100; comments will be limited to 500 (F6-US1) |
| Answers | `DataSafeguardExceptionHandler` maps the exceptions to 403 and 400 with fixed messages that never echo what was submitted. Hidden rows simply do not exist for the caller (a draft is a 404). Logs carry the kind of refusal, the entity and the caller kind, never values |
| One click, one unit of work | The first save inside a web request opens a transaction; `UnitOfWorkFilter` commits it when the action finishes normally and rolls it back when the action throws (a refused write included), so nothing from a failed click persists. An answer with a failure status (a 401 after a wrong password) is not an exception: the failed-attempt counter it saved is kept. A refused or failed save marks the click failed even if the code caught the exception and answered normally. `UnitOfWorkMiddleware` completes saves made outside an action (middleware, minimal endpoints). Commit and rollback ignore a client that disconnects |

### Editor access (F4-US2)

Who may change content is decided in the API, never in the page. Every blog write carries `[Authorize(Policy = AdminOnly)]` and `[ValidateAntiForgeryToken]`; the data layer refuses a non-administrator write again beneath it (F7-US1). `DataAccessArchitectureTests` fails if any controller write action lacks `[Authorize]` or `[AllowAnonymous]` (the public contact form says `[AllowAnonymous]` on purpose), or if a signed-in write lacks the anti-forgery attribute, so a future endpoint cannot ship open. `EditorAccessTests` runs create, update and delete for an anonymous caller (401), a Reader (403), an expired session (401) and an administrator, and asserts the stored content.

The client mirrors this only to spare people a page they cannot use: `/editor` is behind `editorGuard`, the public header shows an "Editor" link to administrators only, and `sessionInterceptor` handles the CSRF token and the "session expired" case. Sign-in returns to the page the person came from only when `safeReturnUrl` accepts it (a path on this site).

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
        A-->>B: Set-Cookie goosewebsite.signedin=1 (readable marker, no identity)
        A-->>B: 200 displayName, isAdmin
    end
    B->>A: GET /api/auth/me (only while the goosewebsite.signedin marker is present)
    A-->>B: displayName, isAdmin (UI hint only)
```

### 5.2 Contact message

The link is emailed to the submitted address and never returned to the browser, so only the owner of that inbox can confirm it. Confirmation is an explicit POST from the client page, so mail link scanners that fetch the URL cannot trigger delivery.

```mermaid
sequenceDiagram
    autonumber
    participant B as Angular client
    participant C as ContactController (maps outcomes; the module in `ContactSubmissionService` runs the steps below)
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
    participant M as AccountEmailSender (called by ReaderAccountService)

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

### 5.4 Subscribe and confirm (F10-US1)

```mermaid
sequenceDiagram
    autonumber
    participant B as Angular client
    participant S as SubscriptionsController
    participant R as SubscriptionService
    participant D as DatabaseService
    participant M as IMailService

    B->>S: POST /api/subscriptions {name, email, website} (CSRF, rate limited per IP)
    S->>R: SubscribeAsync
    alt honeypot filled, or name or email invalid
        R-->>S: nothing stored, nothing sent
    else new address
        R->>D: add Pending subscriber (token hashes only)
        R->>M: confirmation email with /subscribe/confirm#token=...
    else pending address, last mail older than the cooldown
        R->>D: new token hash and expiry
        R->>M: fresh confirmation email
    else pending inside the cooldown, or confirmed
        R-->>S: nothing sent
    end
    S-->>B: 200 with the same message for every case
    Note over B: Visitor opens /subscribe/confirm#token=... and presses Confirm
    Note over B: Unsubscribe (F10-US2): /unsubscribe#token=... and the button posts the sealed id; the one-click header posts the same token without CSRF. GET never deletes
    B->>S: POST /api/subscriptions/confirm {token}
    S->>R: ConfirmAsync (hash lookup, pending, not expired)
    alt valid
        R->>D: State = Confirmed, token cleared
        S-->>B: 200 confirmed
    else unknown, expired, malformed or used
        S-->>B: 400 no longer valid (nothing changes)
    end
```

Pending records disappear on their own: the hosted cleanup deletes them once the confirmation link has expired.

### 5.5 Announce new work (F10-US3)

```mermaid
sequenceDiagram
    autonumber
    participant G as Gustavo (editor)
    participant E as Blog or Projects authoring service
    participant A as IContentAnnouncer (Subscriptions)
    participant D as DatabaseService
    participant J as AnnouncementDeliveryService (hosted)
    participant M as IMailService

    G->>E: POST publish (CSRF)
    alt already published, or AnnouncedAtUtc is set
        E-->>G: 200, nothing queued
    else first publish
        E->>D: IsPublished, PublishedAtUtc, AnnouncedAtUtc = now (save)
        E->>A: AnnounceAsync(kind, id, title, summary, path)
        A->>D: Announcement + one delivery row per confirmed subscriber (same unit of work)
        E-->>G: 200 (no subscriber data, no mail sent yet)
    end
    loop every PollSeconds
        J->>D: owed deliveries that are due
        loop each recipient, own scope
            J->>D: recipient still confirmed?
            J->>M: personal email (own unsubscribe link and headers)
            alt accepted
                J->>D: SentAtUtc = now
            else failed
                J->>D: Attempts + 1, LastAttemptAtUtc (retry later, give up after MaxAttempts)
            end
        end
    end
```

A draft save, an edit, an unpublish and a repeated publish queue nothing. If the mail provider is down or unconfigured the publish still succeeds; the queue keeps the owed recipients and the job retries them without touching those already reached.

## 6. Frontend

Angular 20 with standalone components, signals, SCSS, and the built-in router. Source: `src/GooseWebsite.Client/src/app/`.

```mermaid
flowchart TD
    root[App<br/>app.ts shell, scroll and header state]
    router{{RouterOutlet}}
    root --> router
    router -->|/sign-in| signin[SignInPage]
    router -->|/verify-email| verify[VerifyEmailPage]
    router -->|/projects| projects[ProjectsIndexPage]
    router -->|/projects/slug| project[ProjectPage]
    router -->|/stories| stories[StoriesIndexPage]
    router -->|/stories/slug| story[StoryPage]
    router -->|any other URL| notfound[NotFoundPage]
    root -->|home, projects, stories, not-found| shell[Site shell]

    shell --> header[SiteHeader]
    shell --> home[HomePage]
    shell --> blog[BlogPage]
    shell --> contact[ContactPage]
    shell --> footer[SiteFooter]

    home --> hero[Hero]
    home --> work[SelectedWork]
    home --> approach[Approach]

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
| `app/features/auth/` | `api/` (AuthApi with CSRF handling, and `session-marker.ts`: `getCurrentUser()` makes no request without the sign-in marker and clears the marker when the API answers 401), `models/`, `pages/` (sign-in, verify-email). Authentication pages hide the public shell. |
| `app/features/editor/` | The editor workspace (`EditorPage`: stories with status filter, title search and confirmed deletion), the story form (`pages/story-form-page`), `StoryApi`, `editorGuard` (administrator in; Reader to `/access-denied`; not signed in to `/sign-in?returnUrl=`) and the no-access page. Standalone routes, like the authentication pages |
| `app/shared/http/` | `sessionInterceptor`: gives every non-GET request to `/api/` the CSRF token and, when a non-auth API call answers 401, sends the person to sign in with `reason=session-expired` and the page they were on |
| `app/features/home/` | The home page and its sections, all built from the design tokens: `hero` and `categories` over the animated scene, `approach` (About: the portrait and only the name, positioning line, short bio and professional summary from `shared/models/profile.ts`, nothing Gustavo has not written), `selected-work` (Projects: published projects from `/api/projects` as `ProjectPanel`s). `categories` shows three cards (Projects to the `/projects` page, Life stories to `/stories`, Competitions to `/stories?tag=competition`; each is a plain link whose address carries the whole filter, so the root `StoryFilter` service was removed in F5-US2). The Skills section was removed 2026-10-27 (TE4). Stories (`features/blog`, now a preview of the latest three stories with an "All stories" link) and Contact (`features/contact`) follow, then the footer |
| `app/features/projects/` | F5-US1. `ProjectsIndexPage` (`/projects`: every published project from `GET /api/projects` as a compact, unframed entry with cover thumbnail, title, one-line overview and technologies; loading, empty and failure states in words) and `ProjectPage` (`/projects/:slug` from `GET /api/projects/{slug}`: title, overview as safe Markdown, Gustavo's contribution, technologies, cover image with its alt text, then demo and source links that open in a new tab with `rel="noopener noreferrer"` and say so; a 404 shows `NotFoundPage` in place). `PublicProjectApi` is the read-only client. Both pages are lazy routes with a readable 70-character column and no sideways scroll at 375 px |
| `app/features/stories/` | F5-US2. `StoriesIndexPage` (`/stories`: every published story from `GET /api/blog` with an unbounded `pageSize` (no cap) as a compact, unframed entry with category, date, title, excerpt and tags; the search box and category buttons that used to live on the home section; `?tag=` filters by one tag, case-insensitive and exact, with a "Showing stories tagged #x" line, a "Show all stories" link to plain `/stories`, and "No stories tagged #x yet."; the address is the whole filter state) and `StoryPage` (`/stories/:slug` from `GET /api/blog/{slug}`: title first, the summary (excerpt) under it, optional cover image with alt text, the body as safe Markdown, then category, date and tags as secondary metadata in a 70-character column; a 404, which covers a draft, shows `NotFoundPage` in place). `PublicStoryApi` is the read-only client; `shared/story-date.ts` formats dates in UTC (`22 Sep 2026`) from the publication date, or the creation date when there is none. Public pages render Markdown with `renderMarkdown(source, { headingOffset: 1 })`, so a `#` in a body becomes an `h2` and the page keeps one `h1` (the editor preview keeps the author's levels). Routed pages use `gcv-section--page`, whose top padding is the token `--gcv-header-clearance`, so the first line clears the fixed header; the story body and the project overview use `gcv-reading` for heading, quote and code styling; GIFs work as cover and inline images like any other upload (`<img>`, so they animate) |
| `app/features/not-found/` | `NotFoundPage`: the site's not-found state (a `main` landmark, one `h1`, a link home). It is the wildcard route (`**`, last in `app.routes.ts`) and is also shown by `ProjectPage` and `StoryPage` for an unknown slug. While it shows it sets `PageState.notFound` (`shared/page-state.ts`), so the header marks no section current |
| `app/features/subscriptions/` | F10-US1. `SubscribeForm` (`app-subscribe-form`, inputs `idPrefix` and `label`): labelled name and email fields, a honeypot field named `website` inside an `aria-hidden` wrapper with `tabindex="-1"`, errors in words next to the field with `aria-invalid` and `aria-describedby`, focus moved to the first invalid field, states in words ("Subscribing...", the server's message, "Too many attempts...", "could not be completed"). It appears twice: in the home page's short `SubscribeSection` (`#subscribe`, between Stories and Contact) and in the footer of every public page (so on the home page the ids are prefixed `home-subscribe-` and `footer-subscribe-`). `SubscribeConfirmPage` (`/subscribe/confirm#token=...`, title "Confirm subscription", no header or footer like the other link pages) confirms only when the person presses the button, so a mail scanner that opens the link cannot confirm an address. `UnsubscribePage` (`/unsubscribe#token=...`, title "Unsubscribe", no header or footer) does the same for F10-US2: the button posts the token, the token leaves the address bar, an invalid or used link gets one neutral message and a link without a token offers no button. `SubscriptionApi` posts to the three endpoints (the session interceptor adds the CSRF token) |
| `app/features/blog/`, `app/features/contact/` | Self-contained feature components with their own template and styles (`blog` is the home Stories preview) |
| `app/layout/` | `site-header`, `site-footer` |
| `app/shared/` | `models/` (profile, navigation item), `styles/site.scss` shared layout rules |
| `src/styles.scss` | Global design tokens and font families |

The home page is one long page with anchors (`home`, `work`, `approach`, `blog`, `contact`). The shell (`app.ts`) has three modes by URL: the authentication, link-confirmation (`/verify-email`, `/contact/verify`, `/subscribe/confirm`, `/unsubscribe`) and editor screens (no header or footer), the home page (`/`, which renders the sections inside the shell), and every other public URL (`/projects`, `/projects/:slug`, `/stories`, `/stories/:slug`, and any unknown URL), where the routed page renders between the same header and footer through the single `router-outlet`. From a routed public page the header and footer links go to `/` and then scroll to the section, except the Projects and Stories items of the header and menu, which have a `route` (`NavigationItem.route`) and are real links to `/projects` and `/stories` (a plain link to `/stories` clears a `?tag=` filter). The header marks Projects or Stories current on those pages, and nothing while the not-found page shows. The router starts every new page at the top (`withInMemoryScrolling`, scroll restoration and anchor scrolling enabled in `app.config.ts`). Every route has its own document title through `SiteTitleStrategy` (`shared/site-title-strategy.ts`): `<page> - Gustavo Couto Vanin`, the site name alone for routes without a title (home, sign-in); a project page refines it with the project's title once loaded, and `NotFoundPage` sets its own. `plainSummary` (Markdown to a short plain summary) lives in `shared/plain-summary.ts`. The Projects card on the home page links to `/projects`; the home Projects section previews three projects and links to `/projects`, the home Stories section previews the latest three stories (each links to `/stories/<slug>`; there is no in-page reading view) and links to `/stories`, and each `ProjectPanel` links to its `/projects/<slug>` page. The client keeps the CSRF token in memory and requests a fresh one before each mutation (`AuthApi.postWithCsrf`). The contact form and blog section call the API with plain `HttpClient`.

Build facts: output goes to `../GooseWebsite.Api/wwwroot`; production budgets warn at 500 kB and fail at 1 MB for the initial bundle; `ng serve` proxies `/api` to `https://localhost:8889` through `proxy.conf.json`.

### 6.1 Design system (F9-US1)

Direction (Gustavo, 2026-10-16): a mostly black/charcoal interface that stays extremely clean, with electric cyan as the main accent and magenta as the small secondary one; the artwork carries the visual complexity. The three category cards keep their own magenta (Projects), cyan (Life stories) and orange (Competitions) accents. Source material is in `Docs/Inspirations/`.

- **Tokens** are CSS custom properties in `app/shared/styles/tokens.scss`, all named `--gcv-*`: surfaces (`bg-0..2`, `line`), text (`text`, `text-muted`, `text-faint`), accents (`cyan`, `magenta`, `violet`, `green`, `orange`), type (families Michroma / DM Sans / JetBrains Mono, a size scale, tracking), space (4 px base), shape (`radius-card`, `bracket-size`), motion (`motion-fast/base/slow`, which become instant under `prefers-reduced-motion`) and focus. The file records how each value traces to `LandingPageInspiration.png`. The old names (`--paper`, `--accent`...) remain as aliases so earlier screens already follow the palette; they are removed once F9-US2 to F9-US5 have moved every screen to the `gcv` names. **Rule: no screen uses a colour, font, size or spacing literal; it uses a token.**
- **Base components** are global classes in `app/shared/styles/components.scss`: `gcv-eyebrow`, `gcv-heading` (with `--hero`, `--section`, `--card`; an `<em>` inside gets the cyan-to-white gradient), `gcv-label`, `gcv-link`, `gcv-btn` (corner brackets; `--solid`), `gcv-field`, `gcv-chip`, `gcv-rule`, and one site-wide visible focus style.
- **Angular brand components** in `app/shared/brand/`: `BrandLogo` (resized WebP copies of the supplied raster logo: `public/brand/gcv-logo-192.webp` and `-512.webp`; descriptive alt text; never below 48 px; clear space equal to half its height), `ArtScene` and `ProjectPanel` (a "system diagnostic" panel: picture or GIF, technology stack, and a VIEW CASE control that opens the case in place with the demo and source links and a link to `/projects/<slug>`, the full post built by F5-US1).
- **React island.** The animated hero scene (from `Docs/Inspirations/cyberpunk-react-loop`) is a React component in `client/art/`, built by Vite (`npm run build:art`, run automatically before `npm run build` and `npm start`) into `public/art/gcv-scene.js` (git-ignored, about 85 kB compressed), which defines the `<gcv-scene>` custom element in a shadow root. `ArtScene` loads that script the first time a page needs it, so pages without the scene never download React. The scene lays its effects on a 3:2 "stage" sized to cover the area (so every effect stays on the right part of the art at any screen shape): shifting sky light and drifting clouds, four ships crossing the skyline continuously at different heights and speeds, a pulsing terminal glow with scrolling scanlines and a sweep fitted to the laptop screen, flickering neon signs (the koi tower, the kanji signs), drifting haze, and a slow camera breath; React adds a gentle pointer parallax (skipped on touch screens). On phones the focus moves to the figure and laptop and a 960 px image is used. The scene is decoration (hidden from assistive technology, no pointer input), and every animation, the ships and the parallax stop under `prefers-reduced-motion`. On the home page the scene runs from the top of the page to just behind the top of the category cards and fades out there (a mask on the element; `--scene-fade-start` sets where the fade begins). The hero artwork is `public/brand/hero-city.webp` (and a 960 px copy for narrow screens), optimized from the supplied PNG (2.3 MB to 207 kB). Future React-only animation goes in `client/art/` the same way. React is used only there; the application stays Angular.
### 6.2 Page transition (F9-US2)

A route change cross-fades the page content with a small upward settle. `app.config.ts` registers `withViewTransitions(pageTransitionOptions)` (`shared/page-transition.ts`), so the router wraps each navigation in the browser's View Transitions API. The first load never animates (`skipInitialTransition`), and under `prefers-reduced-motion: reduce` the transition is skipped in `onViewTransitionCreated` (the page changes instantly), and it is also skipped when the navigation keeps the same page (a confirmation page stripping its token from the address). `shared/styles/site.scss` gives the header and footer their own view-transition names (`gcv-header`, `gcv-footer`) so they stay still, and animates the remaining page snapshot: the old page fades out, the new page fades in while settling up 12 px, both over `--gcv-motion-base` with `--gcv-ease` (tokens only). The footer's old and new images are not cross-faded (`animation: none`), so it does not pop on short pages. Chromium, Firefox and WebKit all run the real animation path in the browser stage; a browser without the API would navigate instantly, and the in-page scrolling of the home sections is not a navigation, so it is unaffected. Scroll restoration is unchanged. Tests: Karma `Page transition` and `App shell`; browser `page-transition.spec.ts`.

## 7. Configuration, build, and operations

### 7.1 Configuration reference

Configuration binds in this order of precedence, last wins: `appsettings.json`, `appsettings.{Environment}.json`, user-secrets (Development only), environment variables. Nested keys use `__` in environment variables (`Email__Smtp__Host`).

| Key | Purpose | Default |
| --- | --- | --- |
| `ConnectionStrings:Default` | SQLite database; a relative path resolves against the project folder | `Data Source=Data/goosewebsite.db` |
| `Email:Smtp:Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName` | The shared SMTP sender. **Secrets: user-secrets or environment only** | empty; mail is skipped with a warning |
| `Email:MailerSend:ApiKey`, `FromEmail`, `FromName` | MailerSend HTTP API for the templated contact emails. Both mail transports sit under the one `Email` section; the older `MailerSend:*` keys (existing user-secrets, environment `MailerSend__*`) are still read as a fallback, and `Email:MailerSend:*` wins when both are set. **The API key is a secret: user-secrets or environment only** (Docker: `MAILERSEND_API_KEY`, sender defaults to `SMTP_FROM_EMAIL`) | empty; contact emails are not sent |
| `Contact:Templates:SenderConfirmation`, `SenderReceipt`, `OwnerNotification` | MailerSend template ids (identifiers, not secrets) | the three ids in `appsettings.json` |
| `Contact:LogoUrl` | Public address of the email logo | `{PublicBaseUrl}/email/gcv-logo.png` |
| `Contact:RecipientEmail` | Inbox for verified contact messages | empty; delivery is skipped |
| `Contact:RateLimit:MaxRequestsPerWindow`, `WindowMinutes`, `MaxTrackedClients`, `MaxRequestsPerRecipient` | Contact abuse guard | 5, 10, 10000, 2 |
| `Subscriptions:ConfirmationLifetimeDays`, `ResendCooldownMinutes`, `RateLimit:PermitLimit`, `ConfirmPermitLimit`, `OneClickPermitLimit`, `WindowMinutes` | Subscription link lifetime and pending lifetime, minimum gap between confirmation mails to one address, per-client-address limits | 7, 2, 5, 20, 120, 10 |
| `Subscriptions:Announcements:PollSeconds`, `MaxAttempts`, `RetryDelayMinutes`, `SummaryMaxLength` | How often the background job looks for owed announcement mail, attempts per recipient before giving up, the wait after a failure (minutes times attempts so far), and the length the summary is cut to | 20, 5, 10, 300 |
| `Auth:ReaderRegistration:Enabled` | Reader registration endpoints | `false`; `true` in Development |
| `PublicBaseUrl` | Base for links in emails (in Docker `docker-compose.yml` sets it to `https://SITE_DOMAIN`; the default `https://localhost` is for development only). Verification links for both Contact and Accounts are built by `Shared/Hosting/PublicLinkBuilder` (values go in the URL fragment); the Contact email also reads it for the plain site address and logo URL | `https://localhost` |
| `HSTS_MAX_AGE` | Seconds of the `Strict-Transport-Security` header sent by Caddy. It is a deployment setting: set it in the deployment `.env` (read by `docker-compose.yml`, passed to Caddy). Short by default so a local browser is not pinned to HTTPS; use a long value such as `31536000` only once the real domain serves HTTPS with a public certificate. The verification stack keeps `300` | `300` |
| `DataProtection:KeysPath` | Key-ring folder (cookies, antiforgery and the subscription unsubscribe links); must persist across restarts (`/data/keys` on the Docker data volume) | `data-protection-keys` next to the project |
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
| Reset an administrator's password | `dotnet run --project src/GooseWebsite.Api -- admin reset-password` (existing administrators only; ends their sessions and clears any lockout) |
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

- **State to back up:** the `web-data` volume (`/data/goosewebsite.db` and `/data/keys`). Losing the database loses accounts, posts and subscribers. Losing the keys signs everyone out **and breaks every unsubscribe link already sent in email** (the link is the subscriber id sealed with the key ring and nothing is stored, see 3.6c), so subscribers could no longer leave. The **key ring needs exactly the same backup as the database, taken together and restored together, and must never be revoked** (no `RevokeAllKeys`, no deleting `/data/keys`, no change of `DataProtection:KeysPath` without moving the files). Normal key rotation is safe: old keys stay readable. `DataProtectionKeyRingGuard` logs a warning at startup when the ring is empty while subscribers exist, which is the sign that the keys were lost; the fix is to restore the key directory from the backup that matches the database.
- **Never log the one-click unsubscribe query string:** the address `/api/subscriptions/unsubscribe/one-click?token=...` carries a token that never expires. The app does not log request URLs and the `Caddyfile` has no `log` directive; if access logging, HTTP logging middleware or an external proxy is added, redact or drop the query string of that path first.
- **TLS:** Caddy uses its local CA for `localhost`. To trust it on Windows, export the root with `docker compose cp caddy:/data/caddy/pki/authorities/local/root.crt ./certs/caddy-root.crt` and import it into the current user's trusted roots. For a public site set `SITE_DOMAIN` to a real DNS name that points at the host with ports 80 and 443 open, and Caddy obtains a certificate automatically. Do not share the CA key held in the `caddy-data` volume.
- **TLS and the container's HTTP port:** the browser always talks to Caddy over HTTPS (port 443; port 80 redirects). Caddy terminates TLS with a certificate from its own local CA and forwards to the app container over the private Docker network, so the app's `Now listening on http://[::]:8888` log line is expected: port 8888 is not published to the host. Production will replace the local CA with a public one by setting `SITE_DOMAIN` to a real DNS name.
- **Trusting the local CA:** the CA lives in the `goosewebsite_caddy-data` volume, so it survives restarts but a new volume (for example after a project rename or `docker compose down -v`) creates a new CA that the machine does not trust, and the browser shows a certificate error. After that, export the root with `docker cp goosewebsite-caddy:/data/caddy/pki/authorities/local/root.crt certs/caddy-root.crt`, import it with `certutil -user -addstore Root certs\caddy-root.crt` (Windows asks for confirmation), and remove the stale root with `certutil -user -delstore Root <old thumbprint>`. Compare `openssl x509 -in certs/caddy-root.crt -noout -fingerprint -sha256` with what is trusted.
- **Renamed volumes:** the Compose project is `goosewebsite` (containers `goosewebsite-web` and `goosewebsite-caddy`, image `goosewebsite-web`, volumes `goosewebsite_web-data`, `goosewebsite_caddy-data`, `goosewebsite_caddy-config`, network `goosewebsite_app-private`). Stacks created under the earlier names `testwebsite` (volume `testwebsite_portfolio-data`, database `testwebsite.db`) or `vaninwebsite` (database `vaninwebsite.db`) keep their data in the old volumes. To keep it, stop the old stack with `docker compose -p testwebsite down` (or `-p vaninwebsite`) (never `-v`), copy the old volume's contents into `goosewebsite_web-data`, and rename the database file to `goosewebsite.db*`. An old stack left running also blocks the new one, because it holds ports 80 and 443 and the `172.30.100.0/24` subnet.
- **Logging policy:** log categories and counts only. Never log message bodies, email addresses, tokens, or passwords. Keep this when adding log lines.
- **Mail troubleshooting:** the API logs `Mail delivery failed; kind=rendered; transport=SMTP` with the exception type and SMTP status code. `smtpStatusCode=MustIssueStartTlsFirst` is .NET's name for any 530 reply, and it usually means the provider rejected the credentials (a `535` on AUTH). Confirm with a handshake that sends no data: `openssl s_client -connect smtp.mailersend.net:587 -starttls smtp -crlf -quiet`, then `EHLO`, `AUTH PLAIN <base64 of \0user\0pass>`, `MAIL FROM`, `RCPT TO`, `QUIT`. `235` means the credentials work. For a MailerSend trial domain, send from an address on that domain.

## 8. Conventions

- **Layout:** one module per feature under `Modules/<Name>/`, folders as in section 2. Namespaces match folders (`GooseWebsite.Api.Modules.<Name>.<Folder>`).
- **Registration:** every module exposes exactly one `Add<Name>Module(...)` extension and is wired by one line in `Program.cs`. Startup work goes in an `IModuleInitializer`; operator commands go in an `IModuleCommand`.
- **Persistence:** entities and their `IEntityTypeConfiguration<T>` live in the owning module. Query logic lives in an interface in the module's `Services/`, and every read or write goes through `DatabaseService`. Controllers and modules do not use the context or Identity managers directly. Add a generic method to `DatabaseService` rather than bypassing it, and declare the entity's `EntityAccessRule<T>` in the same module.
- **Contracts:** never return an entity from an endpoint. Use a request and response type in `Contracts/` with validation attributes. Never bind privilege-bearing fields (role, `EmailConfirmed`, ids of other users) from a request.
- **Authorization:** protect every write with a policy from `AuthorizationPolicies`, not bare `[Authorize]`, and add `[ValidateAntiForgeryToken]` to cookie-authenticated mutations.
- **Configuration:** bind settings to an options class (see `SmtpOptions`, `ContactOptions`). Do not read `IConfiguration` ad hoc in new code, and never hard-code addresses or credentials.
- **Mail:** modules send only through `IMailService` and read the `MailResult`; they never touch SMTP or MailerSend. Account emails are rendered in the module and sent with `SendAsync`. Subscription emails (F10) are rendered the same way. Contact emails are MailerSend templates sent with `SendTemplateAsync`: the module picks the template id and supplies every variable it reads (case-sensitive names, constants in `ContactEmailTemplateVariables`), and a test pins them. A variable the request omits renders as its own name in the inbox.
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
3. Declare `CommentAccessRule : EntityAccessRule<Comment>` next to the entity (who writes, which rows everyone may read, which properties are user text and their limits); an architecture test fails without it. Put the module's query logic behind `ICommentRepository` in `Services/`; it reads and writes only through `DatabaseService`.
4. Protect endpoints with `AuthorizationPolicies.VerifiedReader` (and CSRF on writes). Do not copy role checks.
5. Create `CommentsModule.cs` with `AddCommentsModule(...)` that registers the services (and an `IModuleInitializer` if needed).
6. Add one line to `Program.cs`: `builder.Services.AddCommentsModule();`.
7. Add tests in `tests/GooseWebsite.Api.Tests/`. Add a client feature under `app/features/comments/`.
8. Update this document (sections 3, 4, and the matrix) and the backlog evidence.
9. A new table or column needs a migration (see section 4); commit the generated migration files with the change.

### Change a feature

Read its module's section here and its stories in the backlog. Keep changes inside the module and its contracts. If another module must change, the dependency rules in section 3.1 are probably being bent; stop and reconsider.

### Remove a feature

Delete the module folder, its registration line in `Program.cs`, its tests, and its client folder. `DatabaseService` and the other modules do not need edits (Blog is the only one that depends on another module, Accounts). Removing Accounts also removes the policies Blog uses, so remove or rework Blog first.

## 10. Testing

| Level | Location | What it covers |
| --- | --- | --- |
| Integration (host in memory) | `tests/GooseWebsite.Api.Tests/AuthApiIntegrationTests.cs`, `AuthApiFactory.cs` | Login, CSRF, cookie flags, lockout, expiry, registration, verification, authorization, rate limit. The factory swaps in an in-memory SQLite connection and a test mail sink. |
| Unit and service | `AdminProvisioningTests.cs`, `EditorAccessTests.cs`, `AdminPasswordResetTests.cs`, `ContactFeatureTests.cs`, `ContactSubmissionModuleTests.cs`, `DatabaseInitializerTests.cs`, `SchemaCompatibilityTests.cs` | Provisioning rules; contact abuse guard (sequential, concurrent, bounded, expiry) and token store; the Contact submission module through its own interface (honeypot, per-device limit, discard on a rejected address, retry and restore after a failed delivery, log privacy); pinned table and index names so existing databases keep working |
| Client | `*.spec.ts` beside components | Header sign-in and Editor links; verify-email page; sign-in page (expired-session notice, safe return address); `editorGuard`; `sessionInterceptor`; `safeReturnUrl` |

Announcements (F10-US3): `AnnouncementsApiTests` (the real host, the test mail service, an adjustable clock, the hosted loop switched off and a round run by hand): first publish of a story and of a project mails each confirmed subscriber once and never a pending one, personal greeting, unsubscribe link and headers per recipient, edit, unpublish and republish never mail again and the marker is set once, a refused publish announces nothing, the migration marks already-published work as announced, draft saves queue nothing, the publish returns before any mail and survives a provider that fails everything, partial failure retried after the delay without mailing those already reached, giving up after `MaxAttempts`, the hosted loop delivering in the background, no names or addresses in the publish response, the logs or the queue tables, the queue holds only ids and counters, no subscribers, only pending subscribers, a subscriber who unsubscribed before the send gets nothing (F10-US2 acceptance test 4), a late confirmer is not told about earlier work, a hostile summary and title become plain, encoded, one-line text with an ellipsis, the dependency rule (Blog and Projects never reference Subscriptions) and the quiet fallback. Security O2 follow-ups in `UnsubscribeApiTests`: purpose isolation of the token, the one-click request and form limits, its own rate-limit policy, the query string never logged, and the key ring guard. Runtime: `70-blog.sh` and `75-projects.sh` check that a publish with no subscribers succeeds and returns no subscriber data.

Unsubscribe (F10-US2): `UnsubscribeApiTests` (a partial of `SubscriptionsApiTests`): delete of a confirmed or pending subscriber, one neutral answer for used, tampered, unknown, empty and over-long tokens, an old link never reaching a later subscription, the token not stored and every mailed link staying valid, another key ring cannot read or forge a token, GET and missing CSRF change nothing, the one-click endpoint, the link and the `List-Unsubscribe` headers in the mail, `SmtpMailTransport.CreateMessage` headers, the rate limit, log privacy, and the name cleaning (Zl, Zp, Cf). Browser: the `unsubscribe` screen and `unsubscribe:` tests. Runtime: the refusals and the 429 in `65-subscriptions.sh`.

Subscriptions (F10-US1): `SubscriptionsApiTests` (xUnit, the real host with the test mail service and an adjustable clock): stored pending record and one mail, identical answers for new, pending, confirmed, invalid and honeypot submissions, confirm once and never again, expiry, malformed tokens, GET never confirms, resend with a fresh link, cooldown, confirmed address gets no mail, case-insensitive uniqueness, abuse controls (honeypot, per-IP limit, CSRF), mail provider failure, the stored columns and hashed tokens, the access rule limits, the safeguards beneath the service, log privacy, no listing endpoint, the branded email, and the cleanup. Karma: `subscribe-form.spec.ts`, `subscribe-confirm-page.spec.ts`, `unsubscribe-page.spec.ts`, and the shell cases in `app.spec.ts`. Runtime: `verify/runtime/65-subscriptions.sh` (refusals only, no email). Browser: `verify/browser/tests/subscribe.spec.ts` (every call to the subscription endpoints is stubbed by the test, so nothing is sent), plus the `subscribe-confirmation` screen in all the generic checks and the home page checks, which now include the form.

Test doubles: `TestMailService` captures verification links; `StubContactEmailDeliveryService` replaces the contact mail transport; `AdjustableTimeProvider` and `CapturingLogger` support time and log assertions.

Continuous integration (`.github/workflows/ci.yml`) runs on every pull request and on pushes to `main`: the client build and tests, then the API build and tests against that built client, then a Docker image build. Make the three jobs required checks on `main` in the repository settings so a failing run blocks the merge.

### Container verification (TE10)

`scripts/verify.ps1` is the one command that verifies a change on the container stack (the only environment, see `Docs/project.md` section 3.6). It runs, in order: the CI gate (waits for the pull request's GitHub Actions checks; `-SkipCi` skips it), a backup and reset of the stack's data volume, a rebuild and restart of the containers (waiting for the health check), the seed (two test administrators created through `admin provision`, then `admin reset-password` and its refusals, with the answers piped on standard input), and finally the `verify` Compose service (image `Dockerfile.verify`: .NET SDK, Node, Chrome).

| Piece | Location | Notes |
| --- | --- | --- |
| Orchestrator | `scripts/verify.ps1` | Exit 0 only if everything passed; exit 2 means it stopped before verifying (CI red, no pull request, unhealthy stack) |
| Stages | `verify/stages/NN-*.sh` | Sourced in name order and always run to the end: `10-unit-dotnet` (xUnit), `20-unit-angular` (Karma in headless Chrome, `karma.conf.js`), `30-runtime`, `40-browser` (Playwright, below). A new stack (React) is one more script. `VERIFY_STAGES` (a prefix regular expression such as `40`) limits a run to some stages |
| Runtime tests | `verify/runtime/NN-*.sh` | One file per feature: health, HTTPS (redirect, HSTS, certificate), pages, contact (refusals only), mail provider authorization (read-only call, no email sent), authentication, blog, reader registration state, operator commands and startup migrations. A new feature adds a file here |
| Browser tests | `verify/browser/` | Playwright 1.64.1 and axe-core 4.14.0 on Chromium, Firefox and WebKit (TE5); see below |
| Helpers | `verify/lib.sh` | `check`, `check_contains`, `skip`, and HTTP helpers that go through Caddy over HTTPS. A result is `PASS`, `FAIL`, `SKIP` or `KNOWN` (a defect listed in `verify/browser/known-issues.json`, counted apart in the summary and the JSON) |
| Results | `verify-results/` (git-ignored) | `latest.log` (one line per test), `latest.json`, raw `unit-*.out`, `ops.log`, `web.log`, and the last three data backups |
| Test credentials | `.env.verify` (copied from `.env.verify.example`) | Test-only values; the stack's data is reset on every run |

The run never sends an email: the contact form is only given requests it refuses, and the mail provider check is a read-only authorization call that prints no key. The data left in place after a run is the final state of the tests. The first run of this suite found a real defect (sign-out did not end the session on the server), now fixed under F3-US1.

### Browser tests (TE5)

`Dockerfile.verify` installs the pinned Playwright browsers and system libraries (`PLAYWRIGHT_BROWSERS_PATH=/ms-playwright`, a cached layer before the sources). `verify/stages/40-browser.sh` starts `verify/browser/helpers/forward.cjs`, a TCP forwarder from `localhost:443` in the verification container to Caddy, so the browsers use the real address `https://localhost` with the stack's own certificate (accepted with `ignoreHTTPSErrors`), then runs Playwright and turns its JSON-lines report (`helpers/jsonl-reporter.ts`) into one `PASS`/`FAIL`/`KNOWN` line per test per engine. A global setup signs the seeded test administrator in through the API with the `VERIFY_ADMIN_*` settings and keeps the session for the editor screens only for the length of the run. The checks per screen are load without failed requests or console errors, axe WCAG 2.1 A/AA, 320 px reflow, keyboard order and focus indicators, and reduced motion on the home page. Defects that a later story will fix are listed in `verify/browser/known-issues.json` and reported `KNOWN`; an entry that stops reproducing fails the test until it is removed. How to run one engine, add a test and read a failure: `Docs/tests/README.md`.

Tightened checks (TE5 O3): the focus indicator must differ from the element's unfocused outline and box-shadow (an always-present decoration fails); the reachable-element list covers links, buttons, fields, `summary`, `contenteditable`, `iframe`, media with `controls` and anything with `tabindex` of 0 or more (the ARIA widgets button, link, checkbox, tab and menuitem when they get one); the keyboard check also runs on the home page at 320 px with the mobile menu open; axe known issues name rule and element (`axe:<rule>:<selector>`); the 320 px test asserts `innerWidth` is 320 first; `verify/browser/tests/selftest.spec.ts` keeps the negative controls. The stage fails on a known-issues entry whose screen, kind or engine matches no test ("orphan known issue"), counts its own failures, and fails when the number of results differs from the number of tests Playwright lists. Content checks (F9-US7): `profile.spec.ts` (home: the name, positioning line, bio and summary are visible text in About, not only screen-reader text; the portrait has descriptive alt text and loads; GitHub, LinkedIn and email links; no age or street address) and `contact.spec.ts` (F2-US1 tests 1 to 4 on the home page form: each invalid field shows a text error next to it in the shared alert style with `aria-invalid` and `aria-describedby`, the error goes away when corrected, Enter on an empty form shows every error and moves focus to the first invalid field, the submit button is not disabled by validity, and no request reaches `/api/contact`). The contact test never submits a valid form, because the stack may have a real mail provider configured; it stops at validation, and test 3 (the verification-pending message) is covered by the xUnit `ContactVerificationApiTests` and the Karma `contact-verify-page.spec.ts`. `verify/browser/known-issues.json` is empty since F9-US7 fixed the signed-out `GET /api/auth/me` 401 and the WebKit date-field focus indicator. Real Edge and Safari: `Docs/tests/browser-manual-check.md`. Known limits: a request that starts after `networkidle` is not seen by the load check; the content of iframes and positive `tabindex` ordering are not walked by the keyboard check.

Latest result (2026-10-29, after F10-US3 order O3, `./scripts/verify.ps1 -SkipCi`): 1011 checks pass, 0 fail, 0 known (.NET 268, Angular 234, runtime, and the browser stage: 321 tests on 3 engines). Earlier result (2026-10-29, after F10-US1 order O1): 922 checks pass, 0 fail, 0 known (the browser stage listed 303 tests on 3 engines). Earlier result (2026-10-22, after F9-US7): 567 checks pass, 0 fail, 0 known (.NET, Angular and runtime checks, plus the browser stage: 126 tests on 3 engines, all pass, and 2 stage self-checks). Not covered by anything yet: visual regression, Lighthouse, real Edge and Safari, previous browser versions.

## 11. Known gaps and technical debt

Each item is also tracked in [project.md](./project.md) or the backlog.

| Gap | Where | Consequence | Tracked as |
| --- | --- | --- | --- |
| Pending contact messages are in process memory | `ContactVerificationService` | Lost on restart; single instance only | Accepted for R1 |
| Existing local databases may still hold demo posts created before the sample content was removed (the client and the repository hold none since TE4). Operator note: back up `Data/goosewebsite.db` first, then either list the stories in the editor and delete the demo ones, or run `DELETE FROM BlogPosts WHERE Title IN ('<each demo title>');` against the file with the `sqlite3` tool (list them first with `SELECT Id, Title FROM BlogPosts;`). Never run it against data you cannot restore | `BlogPosts` table, editor | The Stories section shows whatever the API returns, so demo rows in a database would be shown publicly. The verification stack data volume is reset on every run, so it never holds them | TE4 (done 2026-10-26) |
| The public `GET /api/blog` has no upper page size (lifted at Gustavo's request, 2026-10-28) and returns the full `Content` of every published story, so its response grows with the number of stories; Caddy exposes it publicly, so it is not internal. Recommended when stories pile up: ceiling of about 500 and no `Content` in the list | `BlogController`, `PublicStoryApi` | Risk accepted for now; only Gustavo can add stories | F5 (Security S1, low) |
| No reader display-name update or reader password reset (administrator password recovery exists, F3-US3) | Accounts | Reader accounts are off in R1, so no one is affected yet | F3-US4 (R3) |
| `npm audit --omit=dev` is clean (0 findings) since the 2026-10-28 update to Angular 20.3.34 and `markdown-it` 15.0.3 (the R1 gate had found 1 high in `markdown-it` and 4 moderate in `@angular/*`, which do ship to the browser). The full `npm audit` (with dev dependencies) still reports 7 high, 3 moderate and 1 low, all in build and test tooling that never ships (`@angular/cli`, `vite`, `undici`, `body-parser`, `follow-redirects`, `zod`, `@modelcontextprotocol/sdk` and the Karma chain); the suggested fixes are breaking downgrades or tool major upgrades | `src/GooseWebsite.Client` | Development and CI only: those packages are not in the browser bundle or the image. Re-run `npm audit --omit=dev` at every release; production dependencies must show no high or critical finding (R1-G6) | Replace Karma and move to the next Angular tooling when Angular 21 is adopted |
| `GooseWebsite.Api.csproj` pins `SQLitePCLRaw.lib.e_sqlite3` 2.1.13 | `GooseWebsite.Api.csproj` | Overrides the vulnerable transitive 2.1.11 (CVE-2025-6965); `SqliteVersionTests` fails if the engine is older than SQLite 3.50.2 | Remove the pin when `Microsoft.EntityFrameworkCore.Sqlite` ships a patched engine |
| Real Edge and Safari have not been checked by a person: the automated browser tests cover Chromium, Firefox and WebKit only | Manual checklist `Docs/tests/browser-manual-check.md` (unticked; no agent can run it) | A defect specific to Edge or real Safari would not be caught | F9-US7; Gustavo runs the checklist before relying on those browsers |
| The emails have not been looked at in real mail clients (Gmail, Outlook, Apple Mail, mobile) | `Docs/email-templates/client-check.md` | Layout or dark-mode defects in a client are unknown; the templates are only covered by automated rendering tests | F9-US6 |
| The contact flow and the account emails have never been exercised end to end against the real SMTP or MailerSend provider: `45-mail.sh` only proves the API key is authorized (a read-only call), and no message has been delivered by an automated run | `Modules/Mail`, `Modules/Contact`, `Modules/Accounts` | A provider-side rejection, a sender-domain problem or a template error would only show on the first real delivery; the contact link goes to the sender, so one real message is needed | F2-US2, F9-US6 |
| The site is not deployed: it exists only in the local container stack. Caddy uses its local certificate authority, so no public certificate has been verified, and `HSTS_MAX_AGE` stays at its verification value of 300 s | `docker-compose.yml`, `Caddyfile`, `.env` | No production URL, public certificate, public `PublicBaseUrl` or production HSTS exists. Before deploying: set the real domain, `PublicBaseUrl` and a long `HSTS_MAX_AGE` (for example 31536000) in the deployment `.env` | Release gate R1-G8; deployment is not yet planned |
