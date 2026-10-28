# F5-US2 test run 3 (order O6)

Role: Testing. Branch `f5-public-browsing`, HEAD `2d9d347` plus the uncommitted O6 working tree. Date 2026-10-28. Windows 11, Docker stack `goosewebsite` rebuilt by `./scripts/verify.ps1 -SkipCi` (no CI gate: uncommitted work). Chrome 156 for Karma. Client built before the .NET runs.

## Result: PASS

## Totals per run

| Layer | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- |
| .NET (`dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`) | 180 passed, 0 failed | 180 passed, 0 failed | 180 passed, 0 failed |
| Karma (`ChromeHeadlessNoSandbox`, `cmd /c` redirect, exited by itself each time) | 195 SUCCESS | 195 SUCCESS | 195 SUCCESS |
| `./scripts/verify.ps1 -SkipCi` | 765 passed, 0 failed, 0 skipped, 0 known, exit 0 | same | same |

Container breakdown per run: unit-dotnet 180, unit-angular 195, runtime 148, browser 242.

## Determinism

- .NET: per-test lists (testName and outcome from the three .trx files) identical, 180 tests all Passed.
- Container: sorted result lines of `latest.log` (durations stripped), 765 each, identical across the three runs (0 diff lines 1 vs 2 and 1 vs 3), none non-PASS.
- Karma: the reporter prints only the count in the fixed command, so the per-test list was not diffed; count and result (195 of 195 SUCCESS) are identical in all three runs, and the same 195 tests also ran inside the container three times. The scroll-to-top test that was nondeterministic in the previous report passed in all three runs here.
- No test is nondeterministic.

## Real-content pass (stack up after run 3, seeded test administrator, via the editor API)

Created: published project "RC3 Project" (Markdown overview with heading, bold, quote, code block and emoji; contribution with an emoji); published story "RC3 GIF Story" (two-frame GIF89a cover with alt text, inline GIF in the body with alt, summary with an emoji, body with heading, quote and code and emojis including a ZWJ family and a flag); 55 published stories "RC3 Bulk 00..54". GIF uploads were accepted (201) and served byte-identical (36681 bytes, decoded as 2 animated frames).

- `/stories` at 1280 and 375: all 56 published stories listed (55 bulk plus the GIF story; one page, no pagination); summary with its emoji shown once per card; no overflow (`scrollWidth` 1265 / 375).
- `/stories/rc3-gif-story` at 1280 and 375: title, cover GIF (alt "Animated red and blue cover", loaded, 240x135) and the inline GIF (alt "inline animated gif") both display and animate (red then blue observed in screenshots); summary text appears exactly once; heading, quote (left bar) and code block styled; emojis whole (rocket, grin, family, sparkles, party); `scrollWidth` 375 at phone width.
- `/projects/rc3-project` at 1280 and 375: overview rendered like story reading (h2 "Overview heading", paragraph with bold, quote bar, code block, emoji whole); overview text appears once; contribution, technologies, Demo and Source links follow; single h1; no overflow.
- `/projects` at 1280 and 375: card shows the overview as plain text (markers removed, emoji intact); no overflow.
- Cleanup: all 1 project, 56 stories and 2 images deleted (204 each); public and editor lists for projects and stories then returned `[]`. No contact message was submitted.

Minor observations (none fails a criterion):

1. The flag emoji (Brazil) shows as the letters "BR" in this Windows browser: the platform has no flag glyphs. It is the font, not the site.
2. Story index cards show no cover image (only the story page does); by design as far as seen, noted in case it is unintended.
3. The page title text appears twice in the page text of `/projects` and `/stories` (kicker "PROJECTS" and heading), as in the previous report.
4. A cover GIF is shown at its natural size (240 px), not stretched; a large cover is not tested here.

## Not covered

- Real Edge and Safari (manual checklist), flag emoji on a platform with a flag font.
- GIF over 5 MB or very large, a project with a cover image in this pass (covered earlier in the F5-US2-run reports).
- CI for this work (not committed or pushed).
- Karma per-test names (the fixed command prints counts only).

## State left

Containers `goosewebsite-caddy` and `goosewebsite-web` are running (web healthy). No stray processes of mine remain.
