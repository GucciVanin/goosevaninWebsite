# Security review - R1-GATE O1..O3 (uncommitted changes on r1-signoff)

Verdict: PASS. No blocking findings. Two low/informational notes.

| # | Area | Result | Severity |
| --- | --- | --- | --- |
| 1 | Public response types | PASS | - |
| 2 | Other endpoints returning stories/projects (Location, ETag, lists) | PASS | - |
| 3 | Dependency changes | PASS | - |
| 4 | .env.example and docs: secrets | PASS | - |
| 5 | Removed ClientApp/.angular cache, .gitignore | PASS | Info |
| 6 | New tests and scripts: credentials | PASS | Low (note) |
| 7 | Residual: public list returns full content (S1) | Known, unchanged | Info |

## 1. Public response types - PASS
`PublicStoryResponse` (title, slug, excerpt, content, category, tags, coverImage, coverImageAlt, createdAtUtc, publishedAtUtc) and `PublicProjectResponse` (title, slug, overview, contribution, technologies, demoUrl, sourceUrl, coverImage, coverImageAlt, publishedAtUtc) are explicit allow-list records mapped field by field. Compared with the editor responses (`BlogPostResponse`, `ProjectResponse`), the public ones omit `id`, `authorId`, `authorName`, `isPublished`, `updatedAtUtc` (and project `createdAtUtc`/`id`). Nothing sensitive or internal remains. `createdAtUtc` on stories is the only non-publication timestamp and is already shown by the public page; it could reveal draft age, which is low value. The public controllers still read only through `GetPublished*`, so unpublished items are not exposed.

## 2. Other endpoints - PASS
Grep of `src/GooseWebsite.Api`: `BlogPostMapping.ToResponse` / `ProjectMapping.ToResponse` are used only in `StoryEditorController` and `ProjectEditorController` (editor policy, cookie + anti-forgery). The `CreatedAtAction` Location headers are in editor controllers only and carry the numeric id to an authenticated administrator. No ETag, sitemap, RSS/feed or other code emits stories or projects. `AuthController` responses are unrelated.

## 3. Dependency updates - PASS
package.json: only `@angular/*` ^20.3.33 -> ^20.3.34 (common, compiler, core, forms, platform-browser, router, compiler-cli) and `markdown-it` ^15.0.2 -> ^15.0.3. Lockfile: the diff touches only those packages plus transitive `argparse` 3.0.2 -> 3.0.4 and markdown-it's ranges (`entities` ^8.1.0, `linkify-it` ^6.1.0). All `resolved` URLs are `https://registry.npmjs.org/`; no new packages, no git/tarball/file URLs, no `hasInstallScript` additions; integrity hashes changed together with each version (expected). Installed tree shows `@angular/core@20.3.34` deduped across the tree. The removal of the stray exact `"@rollup/rollup-linux-x64-gnu": "4.59.0"` from the lock root `dependencies` is correct: package.json keeps it in `optionalDependencies` (^4.59.0), so this is a consistency fix, not a loss of integrity. `npm audit --omit=dev` (run now, network available): found 0 vulnerabilities.

## 4. .env.example and docs - PASS
`.env.example` adds only a commented `HSTS_MAX_AGE` example (a number, no secret). Added lines in CLAUDE.md, Docs/architecture.md, Docs/product-backlog.md and the work-item docs were searched for password/secret/token/key/PEM/email patterns: only prose mentions (test names, "reader password reset" story text). No secrets. The long HSTS value is deliberately kept out of the default (300 s), which is the safe choice for a testing environment.

## 5. Removed .angular cache and .gitignore - PASS (Info)
The staged deletions are `ClientApp/.angular/cache/20.3.37/client/angular-compiler.db` and its lock: build cache from a stale `ClientApp` directory (the client lives in `src/GooseWebsite.Client`). Removing them is correct (generated binary, not source). `.gitignore` adds `ClientApp/.angular/`, preventing re-adding. Info: `src/GooseWebsite.Client/.angular` is not covered by this new line; confirm an existing rule ignores it (not part of this change, no regression).

## 6. New tests and scripts - PASS (Low note)
`PublicContractTests.cs` uses `contract-admin@example.test` and `Strong-password-123`, which are throwaway values for an in-memory factory (same pattern as existing auth tests), not real credentials, and nothing is printed. `shell-motion.spec.ts` touches no credentials. `70-blog.sh` / `75-projects.sh` fetch the author id with the admin cookie jar and only compare it; the id is passed to `check_not_contains`. Low note: if that check fails, the harness may echo the GUID in the log line (an account id, not a secret, and a testing environment). Keep an eye on it only if logs are ever published. The Karma "Keep it" test is UI-only.

## 7. Residual (not part of this change)
The public story list still returns full `content` per item (backlog row S1, unchanged by design); no security impact beyond bandwidth.
