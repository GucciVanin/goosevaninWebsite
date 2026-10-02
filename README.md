# VaninWebsite

The personal website of Gustavo Couto Vanin: a public profile, a verified contact form, and (in progress) a private editor for projects and stories.

- **Why it exists:** [Docs/goal.md](Docs/goal.md)
- **Roadmap and Scrum plan:** [Docs/project.md](Docs/project.md)
- **How it is built:** [Docs/architecture.md](Docs/architecture.md)
- **Stories and acceptance tests:** [Docs/product-backlog.md](Docs/product-backlog.md)

## Prerequisites

.NET 10 SDK, Node.js 22, and (optionally) Docker.

## Run locally

```powershell
# 1. Build the Angular client into the API's wwwroot
Push-Location src/VaninWebsite.Client
npm ci
npm run build
Pop-Location

# 2. Trust the ASP.NET Core development certificate (once)
dotnet dev-certs https --trust

# 3. Run the API, which also serves the client
dotnet run --project src/VaninWebsite.Api
```

The site is at `https://localhost:8889` (HTTP on 8888 redirects). For fast client iteration, run `npm start` in `src/VaninWebsite.Client`; it proxies `/api` to the running API.

### Configuration and secrets

Email settings are not in the repository. For `dotnet run`, store them as user-secrets:

```powershell
cd src/VaninWebsite.Api
dotnet user-secrets set "Email:Smtp:Host" "smtp.example.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Username" "<username>"
dotnet user-secrets set "Email:Smtp:Password" "<password>"
dotnet user-secrets set "Email:Smtp:FromEmail" "<sender address>"
dotnet user-secrets set "Contact:RecipientEmail" "<inbox that receives contact messages>"
```

Without them the app still runs; email sending is skipped with a warning. All settings are listed in [Docs/architecture.md](Docs/architecture.md#71-configuration-reference).

### Create an administrator

There is no public administrator sign-up. On the host, run:

```powershell
dotnet run --project src/VaninWebsite.Api -- admin provision
```

It prompts for an email and a masked password.

## Test

```powershell
dotnet test VaninWebsite.slnx -p:SkipClientBuild=true
Push-Location src/VaninWebsite.Client; npm test -- --watch=false --browsers=ChromeHeadless; Pop-Location
```

## Run with Docker

```powershell
Copy-Item .env.example .env   # then fill in the values
docker compose up --build -d
```

The site is served over HTTPS at `https://localhost` through Caddy. Operational notes, including trusting the local certificate and the volume layout, are in [Docs/architecture.md](Docs/architecture.md#73-operations).

## Layout

```text
src/VaninWebsite.Api/      ASP.NET Core API (Modules/Accounts, Blog, Contact; Shared/)
src/VaninWebsite.Client/   Angular client
tests/                     API tests
Docs/                      goal, project, architecture, backlog
```
