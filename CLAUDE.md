# GooseWebsite - Claude Code Project

Gustavo Couto Vanin's personal website: a public profile, a verified contact form, a private editor for projects and stories, and later public browsing and reader comments. It is a single ASP.NET Core application that serves an Angular client.

**Status:** Active development. See `Docs/project.md` for the roadmap and the current sprint.

## Read these first

| Document | Read it when |
| --- | --- |
| `Docs/goal.md` | You need to know why a feature exists. It overrides every other document. |
| `Docs/project.md` | You are planning work, or need the Scrum roadmap, Definition of Done, and risks. |
| `Docs/architecture.md` | **Before changing any code.** Module layout, dependency rules, flows, conventions, and the recipe for adding a feature. |
| `Docs/product-backlog.md` | You are implementing or verifying a story. It holds the acceptance tests and decisions. |
| `Docs/f3-authentication-architecture.md` | You are touching accounts or authentication. |

## Tech stack

- **Backend:** ASP.NET Core on .NET 10, modular monolith (`src/GooseWebsite.Api/Modules/{Accounts,Blog,Contact}` over `Shared/`)
- **Database:** SQLite through EF Core, with ASP.NET Core Identity (file `Data/goosewebsite.db` in development)
- **Frontend:** Angular 20, standalone components and signals, SCSS (`src/GooseWebsite.Client`). It builds into `src/GooseWebsite.Api/wwwroot`.
- **Email:** SMTP through `ISmtpMailSender` (MailerSend today)
- **Deployment:** Docker; Caddy reverse proxy in `docker-compose.yml`
- **Tests:** xUnit (`tests/GooseWebsite.Api.Tests`), Jasmine/Karma (client)

## Commands

```bash
dotnet build GooseWebsite.slnx                       # also builds the client; add -p:SkipClientBuild=true to skip
dotnet test GooseWebsite.slnx -p:SkipClientBuild=true
dotnet run --project src/GooseWebsite.Api            # https://localhost:8889 (needs `dotnet dev-certs https --trust`)

cd src/GooseWebsite.Client
npm ci && npm run build                              # build the client into the API's wwwroot
npm test -- --watch=false --browsers=ChromeHeadless
npm start                                            # dev server, proxies /api to https://localhost:8889

dotnet run --project src/GooseWebsite.Api -- admin provision   # create an administrator (interactive)
docker compose up --build -d                         # https://localhost, needs .env (see .env.example)
```

Build the client at least once before running the .NET tests; some tests expect the Angular fallback page.

## Rules for working in this repository

1. **Follow the module conventions** in `Docs/architecture.md` section 8. Keep a feature's controllers, contracts, domain, and services inside its module. Do not add a dependency that points the wrong way (section 3.1).
2. **All database reads and writes go through `DatabaseService`** (`Shared/Persistence`). Modules keep their query logic but never inject `ApplicationDbContext`, `UserManager`, or `RoleManager`. Sanitization and access checks are planned there (story F7-US1).
3. **Add features as modules.** One `Add<Name>Module` extension, one line in `Program.cs`. Startup work goes in an `IModuleInitializer`, operator commands in an `IModuleCommand`.
4. **Never put secrets in the repository.** This includes `appsettings*.json`, this file, docs, tests, and logs. SMTP settings (`Email:Smtp:*`) and `Contact:RecipientEmail` come from `dotnet user-secrets` (id `goosewebsite-api`) in development and from environment variables or `.env` in Docker.
5. **Authorization is enforced in the API.** Use the policies in `AuthorizationPolicies` and `[ValidateAntiForgeryToken]` on cookie-authenticated writes. Never accept roles or user ids from a request.
6. **Privacy:** do not store contact messages durably, and never log message bodies, email addresses, tokens, or passwords.
7. **Do not mark a story Done from reading code.** Run its acceptance tests, record the evidence in `Docs/product-backlog.md`, and follow the Definition of Done in `Docs/project.md`.
8. **Keep documents in step with code.** Structural or contract changes update `Docs/architecture.md` in the same change. Scope or status changes update the backlog and the decision log.
9. Use "Gustavo" or "Gustavo Couto Vanin" in public-facing text, not "the owner". Do not publish his age, exact location, or invented credentials.

## Known gaps to keep in mind

The contact verification flow emails the link to the sender (story F2-US2) but has not yet been exercised against the production SMTP provider. The full list is in `Docs/architecture.md` section 11.
