# Worker record - TE4 order O6 (2026-10-27)

## Changes
- `features/blog/blog-page.html`: (1) when the list is empty, posts exist and a tag is set, the message is "No stories tagged #<tag> yet." (the "No story found. Try another search." message stays for a search or category with no match and no tag); the clear button stays (the status line is unchanged). (3) the `.tag-filter` status line is rendered only while no story is open (`!selected()`); it returns when the visitor goes back to the list.
- `layout/site-header/site-header.ts`: injects `StoryFilter`; `onNavigate('blog')` clears the tag. `onNavigate` serves both the primary navigation and the mobile menu drawer, so one change covers both. The Competitions card still sets the tag and the other two cards still clear it (unchanged, in `categories.ts`); the card's navigate event goes straight to `App.scrollTo`, not through the header, so the header clear does not undo it.
- Not changed: `app.ts`, `story-filter.ts`, the browser tests (no browser expectation touched the three behaviours). The footer navigation (`app-site-footer`) also emits `navigate`; I did not change it (not in the order). If it has a Stories link it would keep a tag; flagged for the Reviewer.

## Tests first, red observed (Karma, before the implementation)
4 new specs, 3 failed for the right reasons (139 run, 3 failed, 136 passed):
- `BlogPage ... says no stories carry the tag when none matches`: the text was "No story found. Try another search." instead of "No stories tagged #nothing-here yet."
- `BlogPage ... does not show the tag status line in the reading view`: `.tag-filter` was present in the detail view.
- `SiteHeader mobile menu clears the story tag from the Stories link in the header and in the menu`: tag stayed `competition` after clicking Stories in the nav and in the drawer (the check that Projects does not clear it passed).
- The fourth new spec (search with no match and no tag still says "No story found") is a guard and was green before and after, by design.
After the change: 139 of 139 pass.

## Final run
- Karma (`ChromeHeadlessNoSandbox`): 139 of 139 success (was 135; +4). Note: in this session the Karma process did not exit by itself after printing the TOTAL line (both the red and the green run, output redirected to a file); I ended the background task. Not investigated (known follow-up from the Captain log, item 4).
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 175 passed, 0 failed.
- `./scripts/verify.ps1 -SkipCi` (once): 585 passed, 0 failed, 0 skipped, 0 known (was 581; +4 Karma). Browser stage 126 of 126. `known-issues.json` untouched.
- No git write commands run.
