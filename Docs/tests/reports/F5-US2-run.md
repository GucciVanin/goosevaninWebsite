# F5-US2 test run (order O4)

Role: Testing. Branch `f5-public-browsing`, HEAD `0d5c2e5` plus the uncommitted F5-US2 working tree. Date 2026-10-28. Windows 11, Docker stack `goosewebsite` rebuilt by `./scripts/verify.ps1 -SkipCi` (no pull request for this work, so the CI gate was not run). Chrome 156 for Karma; Playwright 1.64.1 engines (chromium, firefox, webkit) for the browser stage. Client built once (`npm run build`) before the .NET runs.

## Result: FAIL (nondeterministic)

One Angular test is nondeterministic under the fixed Karma command: `App routing configuration scrolls to the top when a visitor navigates from a scrolled page to another page` (`src/app/app.spec.ts:201`, "Expected 800 to be 0"). Everything else passed in every run. Per `Docs/tests/README.md`, a test whose result differs between runs is `fail` with reason `nondeterministic`.

## Totals per run

| Layer | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- |
| .NET (`dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`) | 175 passed, 0 failed | 175 passed, 0 failed | 175 passed, 0 failed |
| Karma (`ChromeHeadlessNoSandbox`, local) | 182 SUCCESS | 182 SUCCESS | 181 SUCCESS, 1 FAILED (the test above) |
| `./scripts/verify.ps1 -SkipCi` | 717 passed, 0 failed, 0 skipped, 0 known, exit 0 | same | same |

Container breakdown (each run): unit-dotnet 175, unit-angular 182, runtime 148, browser 212 (210 Playwright tests plus the stage's own lines).

Karma history in this session: the very first local run (before the three recorded ones) also failed on the same test, so the local Karma command failed 2 of 5 runs. The same 182 Angular tests passed in all 3 container runs.

## Determinism

- .NET: three runs, same count and all passing.
- Container: sorted `latest.log` result lines (durations stripped), 717 each, are identical across the three runs (0 differences 1 vs 2 and 1 vs 3). No test is nondeterministic there.
- Local Karma: nondeterministic as described. Failure mode: after `router.navigateByUrl('/projects')` the test waits up to 2 s for `window.scrollY` to reach 0 (CSS `scroll-behavior: smooth` is set in the styles); on the failing runs it was still 800. The test is timing dependent (it reads the real window scroll position and a smooth scroll animation); in headless Chrome the smooth scroll may not progress at all, or only on some runs. Suggested fix for the Worker: make the test independent of animation (set `scroll-behavior: auto` on the test's `document.documentElement` for the duration, or assert on the router's scroll call instead of the real scroll position).
- Side effect: when Karma finishes with a failure it does not exit by itself; the process and its headless Chrome stay alive. I killed my own leftovers.

## Real-content pass (stack up after run 3, seeded test administrator)

Created through the editor API with the seeded administrator (credentials read from `.env.verify`, not printed): two PNG covers uploaded (640x360 gradients); project "RC Draft Project" (draft) and "RC Published Project" (published, cover with alt text, overview, contribution, three technologies, demo and source links); story "RC Draft Story" (draft), "RC Competition Story" (published, tags competition and Chess, Markdown body with heading, bold, italic, link, list, quote and code block, cover with alt) and "RC Plain Story" (published, tag notes, no cover). No contact message was sent.

Checked in the built-in browser, all as expected unless noted:

- `/projects`: lists only the published project (cover thumbnail, title, overview as plain text, technology chips); header Projects item highlighted; title "Projects - Gustavo Couto Vanin".
- `/projects/rc-published-project`: order is back link, title, overview, "Gustavo's contribution", Technologies, cover image (alt text present, loaded), Demo and Source code links (`target=_blank`, `rel="noopener noreferrer"`, external-link text and arrow). Title "RC Published Project - Gustavo Couto Vanin". `/projects/rc-draft-project`: not-found page.
- `/stories`: lists the two published stories (newest first; category, date, excerpt, tags), not the draft; search box and category chips present. `/stories?tag=competition`: only the competition story, "Showing stories tagged #competition." and "Show all stories". `/stories?tag=zzz`: "No stories tagged #zzz yet." `/stories/rc-draft-story`: not-found page.
- `/stories/rc-competition-story`: title, cover, rendered Markdown (heading, bold, italic, link, list, quote, code), then date, category and tags below; title "RC Competition Story - Gustavo Couto Vanin"; header Stories item highlighted.
- Home: Projects section previews the published project; "VIEW CASE" opens the in-place case with "Open the full project post" (`/projects/rc-published-project`), Demo and Source links, and "All projects" (`/projects`). Stories section shows both stories as previews linking to `/stories/<slug>`, with "All stories" (`/stories`). Cards go to `/stories` (Life stories) and `/stories?tag=competition` (Competitions).
- 375 px: `/projects`, project page, `/stories`, `/stories?tag=competition`, story page and home all have `scrollWidth` 375 (no sideways scroll) and read well. Desktop width (1024): no overflow.

Odd or minor things noticed (none fails an acceptance test; for the Captain to triage):

1. On the new pages the first line of content ("← All projects", "← All stories", the "STORIES" kicker on the index) sits directly under, and on the index slightly under, the fixed header's logo at both desktop and 375 px; it looks cramped, and the kicker is partly covered. Likely missing top padding for the header on standalone pages.
2. A Markdown `# heading` in a story renders as a second `<h1>` (page title is also h1). A story body's top-level heading should probably map to h2.
3. Story heading at 375 px has a very large line height (the "A heading inside the story" lines are widely spaced); the quote has an indent but no visible quote marker; the code block is plain small monospace without a background. Readable, but unstyled.
4. The search box placeholder on the story index reads "try systems or tools" (copied from the earlier section; odd for stories).
5. The page heading text appears twice in the page text of `/projects` and `/stories` ("PROJECTS" then "PROJECTS"); probably a decorative or hidden duplicate, worth a check with a screen reader.
6. The header is transparent over scrolled content, so text scrolls behind the nav (seen on a story page); not new in this story.
7. Not checked: whether the header still highlights Projects on an unknown `/projects/<x>` page (the O4 "if cheap" item); the earlier report noted it.

Cleanup: all created content deleted through the editor API (2 projects, 3 stories, 2 images; all 204). Afterwards the public and editor lists for projects and stories are empty and the uploaded cover URL returns 404.

## New F5-US2 tests seen passing in every container run

Runtime: story routes serve the app (in `30-pages.sh`). Angular: stories index (two stories, none, failure, tag filter ignoring case, "Show all stories"), story detail and not-found, editor pre-publish reminder, header and card targets, page titles. Browser (all three engines): screens `stories` and `story-not-found` (load, axe, 320 px, keyboard, direct load with header and footer, no sideways scroll at 1280 and 375 px). The browser stage lists 210 tests, all reported.

## Not covered

- Only the default empty/populated browser states above; the story index search box and category chips were not exercised with typing or clicks.
- The editor pre-publish reminder was not looked at in a browser (covered by Karma and the editor browser screens only); publishing through the editor UI was done through the API instead.
- Real Edge and Safari, `Docs/tests/browser-manual-check.md`; CI on GitHub; screen readers; widths other than 1024, 375, 320 and the Playwright widths; a story with a very long body or a very long title; a story with images in the Markdown; a project without links or cover.
- One `node` process (PID 17464, started 12:09, before my first command) was left running; it is not one this role started.

---

# Second run (after O5): Testing role

Same tree (HEAD `0d5c2e5` plus uncommitted F5-US2 work), 2026-10-28, Chrome 156 for Karma, Playwright 1.64.1 in the container. No pull request, so the CI gate was not run.

## Result: PASS

All three layers passed three times with identical per-test lists. The flaky scroll test from the first run did not fail in any of the three Karma runs.

## Totals per run

| Layer | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- |
| .NET (`dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`) | 175 passed | 175 passed | 175 passed |
| Karma (`ChromeHeadlessNoSandbox`) | 185 SUCCESS | 185 SUCCESS | 185 SUCCESS |
| `./scripts/verify.ps1 -SkipCi` | 750 passed, 0 failed, 0 skipped, 0 known, exit 0 | same | same |

Container breakdown (each run): unit-dotnet 175, unit-angular 185, runtime 148, browser 242.

## Determinism

- Karma: sorted per-spec lists (timings stripped), 185 lines each; 0 differences run 1 vs 2 and 1 vs 3; no failed spec in any run.
- Container: sorted result lines of `latest.log` (timings stripped), 750 each; 0 differences 1 vs 2 and 1 vs 3.
- .NET: same count and all passing each run.
- Note on the Karma command: one execution launched through a PowerShell `*>` redirect printed the progress reporter and did not exit by itself even after `TOTAL: 185 SUCCESS`; I stopped it and re-ran all three runs via `cmd /c ... > file 2>&1`, which printed the per-spec lines and exited by itself. The recorded three runs are the latter. Worth a line in the README (redirect with `> file 2>&1`, not `*>`).

## Real-content pass (visual items only)

Content created through the editor API with the seeded administrator (credentials not printed): one cover image upload, published project "RC Published Project" (cover, overview Markdown with heading, quote and code), a draft project, published story with a 130-character title (Markdown body with `#` and `##` headings, quote, code block, list, cover), a plain published story and a draft story. No contact message was sent.

Looked at in the built-in browser at 1280 and 375 px:

- Header overlap: gone on `/projects`, `/projects/<slug>`, `/stories`, `/stories/<slug>` at both widths. The first content line (kicker or back link) now starts about 128 px from the top, below the header; the kicker is fully visible.
- Single h1: one h1 on every page. A story body `#` renders as h2 (32 px, 24 px at 375) and `##` as h3.
- Story reading column: width 766 px at 1280; paragraphs 16 px, line height 1.8; headings tight at line height 1.1 (looks fine, not widely spaced as before); the blockquote has a 2 px cyan left rule, indent and muted text; the code block has a dark panel, border and padding. The 130-character title wraps over 7 lines at 1280 and 9 at 375, no clipping, no sideways scroll (`scrollWidth` 375 and 1265 at a 1280 viewport, that is the scrollbar).
- Project page: single h1; no sideways scroll at 375. The Markdown in the project overview renders its heading as a small bold line, the quote with indent but no rule, and the code as plain small monospace without a panel; the contribution and cover come after. Story styling is not applied to project overviews (see findings).
- Index pages: `/projects` shows only the published project, the overview as plain text ("Overview heading A realistic overview ... A..."). `/stories` shows the two published stories, not the draft. Search box placeholder now reads "Search by title or tag".
- Search and chips (375 px): typing "plain" left only "RC Plain Story"; typing "zzzz" showed "No story found. Try another search."; clearing and clicking the "Life" chip kept both stories and set Life pressed, All not pressed. Only one category (Life) existed in this data set, so a chip that narrows the list was not exercised.

## Findings (for the Captain to triage; none fails an acceptance test)

1. A project overview written in Markdown renders with weaker styling than a story body: the quote has no rule and the code has no panel. Only matters if overviews are meant to take Markdown (the project index shows the Markdown as plain text with the heading text inline, "Overview heading A realistic ...").
2. A story with Markdown `#` and `##` yields h2 and h3 with no h1 gap, which is fine; a `#` and an `##` directly after each other look similar in size at 1280 (32 px vs about 24 px). Cosmetic.
3. The header is still transparent over scrolled content (text scrolls behind the nav), as before and not part of this story.
4. The visible text "STORIES / STORIES" and "PROJECTS / PROJECTS" in the page text is the kicker plus the h1, not a duplicate heading; not a defect.

## Not covered

- The editor pre-publish reminder was not looked at in a browser this time: signing in through the UI would have required showing the test administrator's password in a tool call, which I avoided. Covered by Karma in all three runs: "shows, above the Publish button, a visible reminder to review the story for personal or sensitive details" and "does not block: Publish stays enabled, the reminder is not an alert, and saving the draft never publishes". Showing for a draft and not for a published story is therefore Karma evidence only.
- A category chip that narrows the list (needs two categories); real Edge and Safari; CI on GitHub; screen readers; story with images inside the Markdown; a project without links or cover.

## Cleanup

All created content deleted through the editor API (2 projects, 3 stories, 2 images, all 204). Afterwards the public and editor lists for projects and stories are empty. Viewport emulation reset. My Karma processes were stopped; one `node` process (PID 2384, started 13:33, during the browser pass) is alive; it is not a Karma process I launched (those were killed) and I did not stop it.

