# Worker O1 - W1 (R1-G2): public responses carry no account id

## Files
- New: `tests/GooseWebsite.Api.Tests/PublicContractTests.cs` (two tests: public story detail+list, public project detail+list; assert exact key sets, the author's account GUID absent from the text, and that the editor responses keep `id`, `authorId`, `isPublished`).
- `src/GooseWebsite.Api/Modules/Blog/Contracts/BlogPostResponse.cs` (+`PublicStoryResponse`), `BlogPostMapping.cs` (+`ToPublic`), `Controllers/BlogController.cs` (public reads use them).
- `src/GooseWebsite.Api/Modules/Projects/Contracts/ProjectResponse.cs` (+`PublicProjectResponse`), `ProjectMapping.cs` (+`ToPublic`), `Controllers/ProjectsController.cs`.
- `verify/runtime/70-blog.sh` (5 new checks on the story the script creates and later deletes), `verify/runtime/75-projects.sh` (2 new checks).
- `Docs/architecture.md` sections 3.6 and 3.6a (contract rows).

## Red first
Both new tests failed before the change (story: `authorId` present; project: `id`, `createdAtUtc`, etc. present). After: whole xUnit suite 191/191.

## Decisions
- Public story: `title, slug, excerpt, content, category, tags, coverImage, coverImageAlt, createdAtUtc, publishedAtUtc` (exactly what `PublicStoryApi` declares). Dropped `id`, `authorId`, `authorName`, `isPublished`, `updatedAtUtc`.
- Public project: exactly what `PublicProjectApi` declares. Dropped `id`, `isPublished`, `createdAtUtc`, `updatedAtUtc`.
- Editor responses and the editor client model (`editor/models/story.ts` still has `authorId`) unchanged. The public client models never declared `authorId`, so no client change.

## Doubts
- `authorName` ("Site administrator") was dropped from the public story because no public page uses it; if a byline is wanted later, add it deliberately.
- The runtime script changes were syntax-checked (`bash -n`) only; they run in the final container verification (O3).
- The S1 row (list returns full content) is unchanged; not in the order.
