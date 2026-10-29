# R1 release gate report (R1-G1 to R1-G8)

Role: Testing. Checklist: `Docs/work-items/R1-GATE/decisions.md` (approved by Gustavo 2026-10-26). R1-G9 (Gustavo's sign-off) is not evaluated here.
Date: 2026-10-28. Branch `r1-signoff`, HEAD `f8edbe0`, tree clean when the gate started. No application code was changed and no git write command was used.

## Verdict

**R1 cannot be signed off yet: 3 rows fail (G2, G6, G8), 1 row is pass with an open action (G5), 1 row is pending Gustavo's acceptance plus a documentation fix (G1), 3 rows pass (G3, G4, G7).** None of the failures needs new feature work; each is a small fix (work items W1 to W6 below). Re-run the gate rows G2, G6 and G8 after the fixes.

| Row | Verdict | One-line reason |
| --- | --- | --- |
| G1 Stories Done with evidence | **Pending acceptance** (plus doc defects, W5) | F3-US1 is the only R1 story not Done: its acceptance tests pass and it waits for Gustavo. Feature headers and some F9 task boxes are stale. |
| G2 No sensitive fields exposed | **FAIL** | Public `GET /api/blog` and `GET /api/blog/{slug}` return `authorId` (the author's account GUID). Everything else clean. W1. |
| G3 Only administrators can edit | **Pass** | EditorAccessTests 19/19, architecture write-action test, 14 anonymous/CSRF runtime checks, all 3 times. |
| G4 No sample content | **Pass** | No sample content in the tree; fresh stack serves `[]` for stories and projects. |
| G5 HTTPS and cookies | **Pass, action open** (W4) | All HTTPS and cookie runtime checks pass. The production HSTS value is not recorded anywhere: the default is 300 s and `.env.example` does not mention it. |
| G6 Quality gates | **FAIL** | Verification: 3 identical runs, 790/790. But `npm audit --omit=dev` reports 1 high (markdown-it) and 4 moderate (Angular) in production dependencies. CI: main is green; no release pull request exists yet. W2. |
| G7 Secrets and privacy | **Pass** (one low hygiene finding, W6) | No secret in tree or history; log-content tests pass; container logs clean. |
| G8 Known gaps honest | **FAIL** | `Docs/architecture.md` section 11 and `CLAUDE.md` do not list the four gaps R1-G8 names, and section 11 lists two gaps that are fixed. W3. |

## G1 Stories Done in the backlog

Read from `Docs/product-backlog.md` (status lines of every R1 story, evidence paragraphs counted per story).

| Feature | Stories | Status | Evidence paragraph |
| --- | --- | --- | --- |
| F1 | US1, US2 | Done | yes (1 each) |
| F2 | US1, US2, US3 | Done | yes (1, 2, 1) |
| F3 | US2 (R3 scope), US3 | Done | yes |
| F3 | **US1** | **In Progress: "status stays In Progress until sign-off"** | yes (3 paragraphs, acceptance evidence 2026-10-28) |
| F3 | US4 | Proposed, release R3, not part of R1 | n/a |
| F4 | US1 to US5 | Done (approved, PRs merged) | US2 to US5 yes; **US1 none** (its tasks are checked and the status says approved) |
| F7 | US1 | Done | yes |
| F9 | US1, US5, US6, US7 | Done | yes |
| F9 | **US2, US3, US4** | Done ("approved by the merge") | **no Implementation evidence paragraph** (only Progress notes) |

Pending acceptance (not failures): **F3-US1**, which waits for Gustavo's acceptance; it is the single R1 story still In Progress. Its report is `Docs/tests/reports/F3-US1-evidence.md`; the full suites passed in this gate (see G6), including the tests it added (`SignInTimingEqualisationTests`, `80-reader.sh`, `auth.spec.ts`, `sign-in-page.spec.ts`).

Documentation defects found (W5), none is an open acceptance test of a Done story except the first:
1. F9-US2 acceptance test 2 (shell transitions with a reduced-motion path) was not built; the status text records "not required for R1 unless Gustavo asks". The three F9-US2 tasks and F9-US3 tasks and F9-US4 tasks are still unchecked `[ ]` although the stories are Done. F9-US6 task "Test rendering in representative clients" is unchecked (real clients are a manual step, see G8). F9-US5 still says "Gustavo's approval is still to be recorded" in a task while the status says approved by merge.
2. Feature-level status lines are stale: F3 "In Progress" (correct until F3-US1 is accepted), but **F4, F7 and F9 show "Proposed"** though all their R1 stories are Done.
3. F4-US1 has no evidence paragraph; F9-US2/3/4 have none.

Verdict: pending acceptance for F3-US1; documentation fixes required before sign-off (W5). No R1 acceptance test is known to be failing.

## G2 No sensitive fields exposed (live run)

Method: real run against the stack (`https://localhost`, Caddy to web, production environment) with the seeded administrator from `.env.verify` (credentials not printed). A script created one story and one project through the editor API, published both, fetched every public page and API anonymously, then deleted both (204 each) and confirmed `/api/blog` and `/api/projects` returned `[]` again. Scanned for emails, GUIDs, ages and birth words, street and phone patterns, token-shaped values, `authorId`/user-id markers.

| Surface | Result |
| --- | --- |
| Pages `/`, `/projects`, `/stories`, `/stories/{slug}`, `/projects/{slug}`, `/contact`, `/sign-in`, `/access-denied`, `/verify-email`, unknown route | All return the same 12,574-byte Angular shell; clean |
| Client bundle (main, 24 chunks, CSS) | Emails found: Gustavo's public address (once, `profile.ts`), the `you@company.com` form placeholder, one library string. "Southern California" present once (broad location, allowed by the backlog). No age, birth, street, phone, secret or key. Embedded portrait and logo images have no EXIF/text chunks (GPS check negative) |
| `GET /api/projects`, `GET /api/projects/{slug}` | Keys: contribution, coverImage, coverImageAlt, createdAtUtc, demoUrl, id, isPublished, overview, publishedAtUtc, slug, sourceUrl, technologies, title, updatedAtUtc. No account data. Clean |
| `GET /api/blog`, `GET /api/blog/{slug}` | **Includes `authorId` (a GUID of the author's account)** and `authorName` ("Site administrator"). The list also returns full `content` of every story (already a known accepted risk, S1 in section 11) |
| `GET /api/auth/me` anonymous | 401, empty body. As administrator: only `displayName`, `isAdmin` |
| `GET /api/auth/csrf` | Returns an anonymous antiforgery request token by design (not a credential) |
| Contact flow: invalid body, bad verification token | 400 validation text, 409 generic text; no echo of address or message. A valid message was not submitted (the stack has real mail settings) |
| 404 responses | ProblemDetails with a traceId only |
| Response headers | `Server: Kestrel`, no `X-Powered-By` |

Observation (not failing): public responses expose the numeric `id` of stories/projects and `isPublished`/timestamps; these are not account identifiers. The code is `BlogPostResponse.AuthorId` in `src/GooseWebsite.Api/Modules/Blog/Contracts/BlogPostResponse.cs` mapped in `BlogPostMapping.cs`; the same record serves the editor and the public endpoints. The R1-G2 text predicted this ("check and fix if not").

Verdict: **FAIL**. Work item W1.

## G3 Only authenticated administrators can edit

- `EditorAccessTests`: 19 test cases passed in each of the 3 runs (anonymous, reader and expired-session cannot create/update/delete/publish and the stored content is unchanged; every provisioned administrator can write; missing CSRF token refused for writes and public-state changes).
- Architecture test `DataAccessArchitectureTests.EveryWriteActionDeclaresWhoMayCallIt_AndProtectedWritesRequireTheAntiForgeryToken` and its self-test `TheWriteActionCheckSeesInheritedActionsAndAcceptVerbsAndTheAntiForgeryRule`: passed.
- Runtime (`70-blog.sh`, `75-projects.sh`, `78-images.sh`, `60-auth.sh`): anonymous create, update, delete, publish and editor-list read refused with 401 for stories and projects; anonymous and no-CSRF image upload refused; no-CSRF administrator writes refused with 400; protected profile endpoint refuses anonymous. All passed.
- Browser: `auth.spec.ts` (session cookie invisible to script, expired-session redirect) passed on Chromium, Firefox, WebKit.
- Limit: a reader session cannot be exercised at runtime because reader registration is off (`80-reader.sh` proves a role/isAdmin registration body is answered 404 and creates no account); the reader-cannot-write case is covered in xUnit only.

Verdict: **Pass**.

## G4 No sample content

- Tree grep (excluding Docs, lockfiles, node_modules): no sample/lorem/demo/seed post or project content; no `HasData` seed; the only hits are comments stating there is no sample content (`selected-work.ts`, its spec). `BlogPost`/`Project` are constructed only in the authoring services. No built output is tracked (`wwwroot` untracked).
- Runtime: on the freshly reset stack `GET /api/blog` and `GET /api/projects` return `[]` (checked before creating the test content in G2 and after deleting it).
- The "demo posts in old local databases" note in section 11 is an operator note for existing databases; the verification volume is reset on every run.
- Not run: a separate TE4 acceptance re-run (reports `TE4-run.md`, `TE4-run-2.md` exist from 2026-10-26/28).

Verdict: **Pass**.

## G5 HTTPS and cookies

Runtime checks passed in all three runs (`20-https.sh`, `60-auth.sh`, plus browser `auth.spec.ts`):
- HTTP redirects with 308 to https; HSTS header sent; certificate issued by the Caddy local authority (so a public certificate cannot be verified here).
- CSRF cookie and session cookie: Secure, HttpOnly, SameSite=Lax; session cookie has no fixed expiry; sign-in marker is Secure, SameSite=Lax, readable by the page by design, session cookie; a signed-out session cookie replayed later is rejected.
- Also checked directly: `Strict-Transport-Security: max-age=300`, `Referrer-Policy: no-referrer`, `ASPNETCORE_ENVIRONMENT=Production` in the web container.

HSTS production value: the header is set in `Caddyfile` as `max-age={$HSTS_MAX_AGE:300}` and `docker-compose.yml` defaults `HSTS_MAX_AGE` to 300. The comment in `Caddyfile` says to raise it "once the site has a real domain"; `.env.example` does not list `HSTS_MAX_AGE`, and no document records the value to use in production (comment implies one year, 31536000). No production configuration exists because the site is not deployed.

Verdict: **Pass** for the checks; **action open** W4 (record the production value and where it is set, add `HSTS_MAX_AGE` to `.env.example`).

## G6 Quality gates

Three full runs of `./scripts/verify.ps1 -SkipCi` (each resets the data volume and rebuilds the images), consecutive on 2026-10-28 UTC:

| Run | Started to finished (UTC) | Passed | Failed | Skipped | Known |
| --- | --- | --- | --- | --- | --- |
| 1 | 00:55:40 to 01:00:10 | 790 | 0 | 0 | 0 |
| 2 | 01:02:44 to 01:07:05 | 790 | 0 | 0 | 0 |
| 3 | 01:07:35 to 01:11:30 | 790 | 0 | 0 | 0 |

Stage split (identical): unit-dotnet 189, unit-angular 200, runtime 150, browser 251 (Chromium, Firefox, WebKit). The per-test lists of the three `latest.log` files (timings stripped, sorted) are byte-identical. No test was skipped or marked known.

Dependency audit:
- `dotnet list GooseWebsite.slnx package --vulnerable --include-transitive`: no vulnerable packages in either project.
- `npm audit --omit=dev` in `verify/browser`: 0 vulnerabilities.
- `npm audit --omit=dev` in `src/GooseWebsite.Client`: **5 vulnerabilities (1 high, 4 moderate)** in production dependencies: `markdown-it` 15.0.2 (high; GHSA-vfgh-jcmw-g37q quadratic-time parsing with html enabled, GHSA-gfr2-rv44-r23c quadratic-time link reference; fix available) used by `src/app/features/editor/markdown.ts` (raw HTML disabled there; exposure is a browser slow-down on content the administrator authors), and `@angular/common` 20.0.0 to 20.3.33 (moderate, GHSA-jfgx-762w-99vr XSRF token leakage through URL parsing) with `@angular/forms`, `@angular/platform-browser`, `@angular/router` flagged as dependents. `npm audit fix` is reported as available. The checklist requires no high or critical finding in production dependencies, so this fails. Note `Docs/architecture.md` section 11 says the audit's high findings are all in the Karma dev chain; that statement is now wrong for `--omit=dev`.
- CI: `gh` works (authenticated as the repository owner). The latest `CI` run on `main` (push of PR #23 merge, run 40785255594, 2026-10-28) is **success**, as are the preceding runs. Branch `r1-signoff` is local only and has **no pull request**, so "CI green on the release pull request" cannot be satisfied yet (open the release PR and run `gh pr checks`).

Verdict: **FAIL** (W2). Container verification part: pass. CI part: pending the release PR.

## G7 Secrets and privacy

- Tracked sensitive file names: only `.env.example` and `.env.verify.example`; real `.env` and `.env.verify` are untracked. No `.pem`, `.pfx`, `.key`, `secrets.json` or `id_rsa` in tree or any commit (`git log --all --name-only`).
- Private-key headers: none in tree. Provider-token patterns (AWS, GitHub, Slack, MailerSend `mlsn.`, `sk-`, JWT-like) in tree, Docs and the whole history (55 commits, all refs): none.
- Assignments of key/secret/password/token with a 12+ character literal: found only in test and runtime-test files (test passwords, test tokens, a test mail-provider key literal in `ContactEmailTemplateTests.cs`) and as a field name `token` in `categories.ts` (a UI category identifier). No production secret. `.env.verify.example` holds the test-only administrator values by design.
- Direct check of real values: the 13 values of 8+ characters in the local `.env` and `.env.verify` were searched in the tree and, with `git log -S`, in the history without being printed. Matches are only non-secret ones (site domain, SMTP host, sender display name, Gustavo's public contact address that is in `profile.ts`, and the test-only administrator values in `.env.verify.example`). The MailerSend key and the SMTP username/password do not occur anywhere in the repository or its history.
- Low finding W6: `ClientApp/.angular/cache/20.3.37/client/angular-compiler.db` and `...db-lock` are tracked since the first commit (`ea3f53c`, an Angular build cache; no secret pattern found in it, but it is generated noise).
- Log content: xUnit tests passed in all runs: `ContactSubmissionModuleTests.Submit_DoesNotLogSenderEmailOrMessageBody`, `MailServiceTests.TemplateSend_Succeeds_AndLogsNoAddressLinkBodyOrKey`, `...WhenProviderRejects_ReturnsFailureAndLogsOnlyTheStatus`, `...RenderedSend_WhenNotConfigured_ReportsNotConfiguredAndLogsNoSecrets`, `...RenderedSend_WhenTheConnectionFails_...LogsNoSecrets`, `...ATransportThatThrows_NeverThrowsToTheCaller_AndTheExceptionTextIsNotLogged`, `...WhenATransportAccepts_BothKindsSucceedAndNothingSecretIsLogged`. Container logs after a full run (19,012 lines): zero email-address-shaped strings; EF SQL logs show `Parameters=[]` (no parameter values); matches for the words "password" and "token" are only SQL column names.
- No valid contact message was sent during the gate, so no real mail left the machine.

Verdict: **Pass**.

## G8 Known gaps are listed honestly

Checked `Docs/architecture.md` section 11 and `CLAUDE.md` ("Known gaps to keep in mind"):

| R1-G8 required gap | In section 11 | In CLAUDE.md | Evidence it is real |
| --- | --- | --- | --- |
| Real Edge and Safari manual check | No (only in the F9-US7 status and `Docs/tests/browser-manual-check.md`, which says nothing was run by an agent) | No | Checklist unticked; engines only automated |
| Real-client email check | No | No | `Docs/email-templates/client-check.md` exists; F9-US6 limits say not covered |
| Contact verification flow against the real SMTP provider | No | Yes (but worded as the older "production SMTP" gap) | `45-mail.sh` passes (the API key is authorized, read-only call) but no email was delivered end to end in this gate |
| Deployment itself (site only in the local container) | No | The "only environment" paragraph states it, but not as a gap | Caddy uses a local certificate authority; HSTS stays 300 s |

Stale entries in section 11: "No ... administrator recovery" (F3-US3 is Done), "Blog HTTP behavior ... lack tests" (blog runtime and xUnit tests now exist), and the npm audit row claims high findings are dev-only (not true, see G6). The `GET /api/blog` row (full content, unbounded) is current.

Verdict: **FAIL** (W3).

## Work items proposed

| Id | Row | Severity | Proposal |
| --- | --- | --- | --- |
| W1 | G2 | High for R1 exit measure | Remove `authorId` from the public story responses (list and detail); keep `authorName`. Prefer a public response record separate from the editor's. Add an xUnit test that the public JSON has no `authorId` and a runtime check in `70-blog.sh`. Optionally drop `content` from the list at the same time (section 11 S1). Update `Docs/architecture.md` contract notes. |
| W2 | G6 | High (checklist rule) | Run the dependency update for `src/GooseWebsite.Client`: markdown-it to a fixed version (15.0.3 or later) and Angular packages to the patched 20.3.x; rebuild, re-run Karma, the container verification (3 runs) and `npm audit --omit=dev` until it shows none high or critical. Correct the audit row in section 11. Then open the release pull request and confirm `gh pr checks` is green. |
| W3 | G8 | Medium | Rewrite section 11 and the CLAUDE.md "Known gaps" so they list: real Edge/Safari check, real-client email check, contact flow not exercised end to end with the real SMTP provider (key authorization only), deployment not done (local certificate, HSTS 300 s). Remove the stale administrator-recovery and blog-test rows. |
| W4 | G5 | Low | Add `HSTS_MAX_AGE` to `.env.example`, record the production value (proposal 31536000) and where it is set (the deployment `.env`) in `Docs/architecture.md` section 7.1, and list "production value not yet applied" as a deployment gap. |
| W5 | G1 | Low | Backlog hygiene: set feature-level status for F4, F7, F9 (Done) and F3 after F3-US1 acceptance; tick or annotate the stale task boxes in F9-US2, US3, US4, US5, US6; add short evidence paragraphs for F4-US1 and F9-US2/3/4 (point to the Playwright and Karma tests that now cover them); state the F9-US2 transitions decision explicitly. Gustavo to accept F3-US1. |
| W6 | G7 | Low | Remove `ClientApp/.angular/cache` from git (tracked build cache) and ignore it; no history rewrite needed as no secret was found. |

## Environment

- Windows 11 Pro 10.0.26200, Docker (compose project `goosewebsite`, containers `goosewebsite-web` and `goosewebsite-caddy` only; other containers on the machine were not touched), .NET 10, Node via the project lockfiles, Python 3.12 for the live probe.
- Data volume `goosewebsite_web-data` reset three times by `verify.ps1` (backups kept under `verify-results/`, untracked).
- Network was reachable: NuGet and npm audits ran, `gh` authenticated.
- Not verified here: a public certificate and production HSTS, real Edge and Safari, real email clients, real SMTP delivery end to end, CI on the (not yet existing) release pull request, R1-G9 sign-off.
- Test content created for G2 (one story, one project) was deleted; the stack data was left as the last verification run seeded it, and the stack was stopped at the end of the gate (`goosewebsite-caddy`, `goosewebsite-web`).

## Post-fix run

Run after the Worker's fixes O1 to O3 (uncommitted on `r1-signoff`). No code was edited by Testing. Only the `goosewebsite-*` containers were touched (via `./scripts/verify.ps1 -SkipCi`).

### Totals per run (clean build, three runs each)

Clean build: `tests/GooseWebsite.Api.Tests/bin` and `obj` deleted, client rebuilt once (`npm ci && npm run build`), then:

| Suite | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- |
| `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | 191 passed, 0 failed | 191 passed, 0 failed | 191 passed, 0 failed |
| Karma (`ChromeHeadlessNoSandbox`, output via `cmd /c`) | 201 of 201 SUCCESS | 201 of 201 SUCCESS | 201 of 201 SUCCESS |
| `./scripts/verify.ps1 -SkipCi` (exit code) | 806 passed, 0 failed, 0 skipped, 0 known (0) | 806 / 0 / 0 / 0 (0) | 806 / 0 / 0 / 0 (0) |

Each verify run: 191 unit-dotnet, 201 unit-angular, 157 runtime, 257 browser; "browser results complete - 255 of 255 listed tests reported". Previous gate total was 790; the increase comes from the new xUnit/Karma/runtime checks and the two `shell-motion` Playwright tests on three engines.

### Determinism

- Verify: the sorted per-test lines (result, stage, name; timings removed) of the three runs are identical (806 lines, no difference). The three xUnit TRX files list the same 191 test names, all Passed.
- Karma: the progress reporter prints no per-test names, so only the totals (201 of 201 each run) could be compared; they are identical, and the same 201 tests appear inside each verify run's list.
- `shell-motion.spec.ts` (`home: the header slides away and back with a real transition`, `home: the header change is immediate and nothing transitions`) passed on chromium, firefox and webkit in all three runs, six of six each time. Slowest was webkit's transition test (2343 ms, 1593 ms, 2095 ms), which is the engine with the most timing variance; no failure or retry seen in these three runs. Three runs is a small sample, so a residual webkit timing risk cannot be excluded.

### Dependency audit

- `npm audit --omit=dev` in `src/GooseWebsite.Client`: found 0 vulnerabilities (was 1 high, 4 moderate).
- `npm audit --omit=dev` in `verify/browser`: found 0 vulnerabilities.

### G2 public-response inspection (re-run)

Same method as before: seeded administrator from `.env.verify` (credentials not printed) created one story and one project via the editor API and published both; the public endpoints were fetched anonymously; both were deleted (204 each) and the public lists returned `[]` again. No contact message was submitted (only an invalid body and a bad token, as before).

| Endpoint | Keys | Scan (email, GUID, age, authorId/userId, street, phone) |
| --- | --- | --- |
| `GET /api/blog` | category, content, coverImage, coverImageAlt, createdAtUtc, excerpt, publishedAtUtc, slug, tags, title | clean |
| `GET /api/blog/{slug}` | same keys | clean |
| `GET /api/projects` | contribution, coverImage, coverImageAlt, demoUrl, overview, publishedAtUtc, slug, sourceUrl, technologies, title | clean |
| `GET /api/projects/{slug}` | same keys | clean |

No `authorId`, `authorName`, numeric `id`, `isPublished`, `createdAt`/`updatedAt` of projects, account GUID or email in any of the four. The editor response still carries `authorId` and `id` (correct: authenticated only). The 404s carry only ProblemDetails fields; `/api/auth/me` anonymous is 401 empty; as administrator it returns only `displayName` and `isAdmin`; the HTML shell is clean on all routes; `Server: Kestrel`, no `X-Powered-By`. The only "tokenish" hit is the anonymous antiforgery request token from `/api/auth/csrf`, by design. Note: the story list still returns full `content` (section 11 S1, accepted).

### Updated verdict per gate row

| Row | Verdict | Basis |
| --- | --- | --- |
| G1 Stories Done with evidence | **Pending acceptance** | F3-US1 and F5 wait for Gustavo's acceptance; tests behind them pass in all three runs. Doc hygiene (W5) not re-checked by Testing here. |
| G2 No sensitive fields exposed | **Pass** (W1 fixed) | Live re-run above; xUnit `PublicContractTests` and the `70-blog.sh`/`75-projects.sh` checks pass in all runs. |
| G3 Only administrators can edit | **Pass** | Anonymous/CSRF runtime checks and EditorAccessTests pass 3 of 3. |
| G4 No sample content | **Pass** | Fresh stack served `[]` for stories and projects after cleanup and after each reset. |
| G5 HTTPS and cookies | **Pass** | All HTTPS and cookie runtime checks pass 3 of 3. Whether W4 (recording the production HSTS value) was done is a documentation item not re-verified by Testing. |
| G6 Quality gates | **Pass for the local part**: 3 identical runs 806/806, dotnet 191, Karma 201, audits 0 and 0. | CI on the release pull request is not yet run (no release PR; `-SkipCi` used), so the CI half of G6 remains open. |
| G7 Secrets and privacy | **Pass** | Log-content xUnit tests pass 3 of 3; the secrets/history scan of the first gate was not repeated. W6 (tracked Angular cache) shows as staged deletion in git status; not committed. |
| G8 Known gaps honest | **Not re-assessed** | Documentation rows (`Docs/architecture.md` section 11, `CLAUDE.md`) are modified in the working tree; this is a review item, not a runtime test, and was outside this post-fix run. Verdict stays as at the first gate until the Reviewer confirms. |
| G9 | Not Testing's | |

The stack (`goosewebsite-caddy`, `goosewebsite-web`) is stopped at the end of this run; its data is as the last verification run seeded it.
