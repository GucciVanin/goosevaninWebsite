# Worker report - O6 (Gustavo's review decisions)

## What is supported for interactive posts (verified by tests)
- GIF files: upload accepted (already covered by `ImageUploadApiTests`), usable as cover and as an inline `![alt](/uploads/<id>.gif)` in a story body or a project overview. Shown by the public pages as plain `<img>` (animated by the browser) with the alt text. API test `PostStructureTests` (story and project, published, public API, file served as `image/gif`) and Karma tests (cover and inline, alt text) pass. The editor already inserts the Markdown for an uploaded image and its file pickers accept GIF.
- Summary: the story `excerpt` shows on the index (it already did) and now on the story page under the title, before the body (gap closed; hidden when empty). The project `overview` shows on the index (plain, shortened) and in full on the project page (already).
- Emojis, including multi-code-point ones (family ZWJ sequence, flag, skin tone, keycap, heart with variation selector): API tests prove they survive create, edit, publish and the public API (single and list) in title, excerpt, content, category, tags, alt text, and the project's title, overview, contribution, technologies and alt. The sanitizer only strips `char.IsControl` characters and trims, so emojis are not touched (no change needed). Karma proves the story page h1, summary, body, document title and project page equally keep them whole. One gap closed: `plainSummary` (project index overview, panel) could cut an emoji in half; it now cuts on a grapheme boundary (`Intl.Segmenter`).
- Not added (no invention): cover thumbnails on the stories index; external image URLs in the body are whatever Markdown allows (unchanged).

## Changes
1. `BlogController`: removed the 50 cap; `pageSize` below 1 becomes 1; default stays 12; an offset beyond int range returns an empty list (no overflow). No other endpoint shares this code (no other clamp exists).
2. `PublicStoryApi.list()` defaults to every story (`pageSize=2147483647`); the home preview still passes 3.
3. `StoryPage`: summary line. `ProjectPage`: overview has the shared `gcv-reading` class (its own line-height rule removed so the shared one applies).
4. `plain-summary.ts`: grapheme-safe cut.
5. Docs: `architecture.md` (known-gaps row for the 50 cap removed, `GET /api/blog` row, stories and reading-class notes).

## Red-first tests (seen failing before the code change)
- `PostStructureTests.ThePublicStoryListIsNotCappedAtFifty` (55 published stories; got 50).
- Karma: story page shows summary under the title; story page emojis (failed on the summary); stories index asks for every story (`pageSize` > 50); project overview has `gcv-reading` with quote border and mono code; `plainSummary` never leaves half an emoji.
- Seen green on first run (confirming existing support, kept as guards): GIF cover/inline (API and Karma), emoji round trips (API), emojis on the project page. The ProjectsIndexPage emoji-cut test first failed only on a setup error (missing `await page`), then passed; its red state was not separately shown, but the same logic is covered by the red `plainSummary` test.

## Totals
- Karma (`cmd /c` to file): 195 of 195 pass.
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` (client rebuilt first): 180 passed, 0 failed.
- `./scripts/verify.ps1 -SkipCi`: 765 passed, 0 failed, 0 skipped, 0 known; `known-issues.json` still empty. No contact message submitted. Only this project's containers were touched (by verify.ps1).

## Doubts
- The look of `gcv-reading` on a project overview is checked by computed style in Karma (quote border, mono code), not seen in a browser with real content (the stack publishes nothing).
- Requesting `pageSize=2147483647` is a sentinel for "all"; fine while the API is internal, but a public-facing limit would need to come back if the API is ever exposed.
- A mid-session mishap: a script truncated `projects.spec.ts` and `plain-summary.ts`; both were restored from `HEAD` content (read-only `git show` into the files) and then re-edited. No git write command was used.
- Stories not marked Done; backlog evidence and project.md not edited (not in O6).
