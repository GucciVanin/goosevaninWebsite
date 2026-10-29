# Worker O3 - W3, W4, W5 (R1-G8, G5, G1): documents and honesty

## Files
- `Docs/architecture.md`: section 11 rewritten (four honest gaps: real Edge/Safari, real mail clients, contact flow and account emails never exercised end to end against the real SMTP/MailerSend provider, deployment not done; stale rows removed: administrator recovery, blog tests/browser contact tests; reader display-name/password reset kept as F3-US4); section 7.1 `HSTS_MAX_AGE` row now says it is a deployment setting set in the deployment `.env` and why the verification value stays 300.
- `CLAUDE.md`: "Known gaps" rewritten with the same four gaps.
- `.env.example`: commented `HSTS_MAX_AGE` entry (default 300; long value such as 31536000 only once the real domain serves HTTPS with a public certificate). Verification value unchanged.
- `Docs/product-backlog.md`: feature status of F4, F7, F9 set to Done (all their stories are Done; F9 line names the open manual checks); evidence paragraphs added to F4-US1 and F9-US2/3/4 naming real tests; F9-US2/3/4 task boxes ticked with explanations; F9-US5 task note fixed; F9-US6 task split (automated mail-sink tests ticked, real-client rendering left unticked on purpose); F9-US2 acceptance test 2 reworded (see below); decision-log row added. F3 and F3-US1 untouched (In Progress).
- New tests: `verify/browser/tests/shell-motion.spec.ts` (Chromium, Firefox, WebKit; 2 tests each = 6) and a Karma test in `editor-page.spec.ts` for dismissing the delete confirmation with "Keep it" (F4-US1 AC4 had no test for the dismissal).

## Red-first
- F9-US2 "transitions": the shell has no route transition; its only motion is the header slide and colour fades. I wrote the browser test first as an assertion about the real behaviour; its first run failed (3 failures: my selector matched the logo link, whose transition is 0 s, then in the reduced-motion test a 0.01 ms transition was still "running" for one frame in Chromium). Both were test defects, fixed (nav-link selector; poll until nothing runs). The test now passes in all three engines in the full container run.
- Karma "Keep it" test: written against existing behaviour (the dismissal code existed untested), so it was green on first run; I did not mutate the code to see it red.

## Doubts
- F9-US2 AC2 criterion was reworded, since no route or page transition was ever designed or built. This is a story-text change for Gustavo to confirm (logged in the decision log). If he wants a real page transition, that is new feature work.
- Backlog statements of approval "by the merge" are as the backlog already said; I could not find separate approval notes. Tasks "record approval" were ticked with that explanation.
- F9-US6 real-client rendering stays unticked (manual, a gap in section 11).
- The F3-US1 status and F3 feature status were not changed.

## Final verification (after O1 + O2 + O3)
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 191 passed, 0 failed.
- Karma (`cmd /c ... > file`): 201 of 201 SUCCESS.
- `./scripts/verify.ps1 -SkipCi`: **806 passed, 0 failed, 0 skipped, 0 known** (unit-dotnet 191, unit-angular 201, runtime 157, browser 257). Compared with the gate's 790: +1 xUnit... precisely +2 xUnit (PublicContractTests), +1 Karma, +7 runtime checks, +6 browser tests. Two earlier full runs in this session had failures only in my new shell-motion test (803/3 failed, then 805/1 failed); they are explained under Red-first.
- `npm audit --omit=dev`: client 0 vulnerabilities; `verify/browser` 0 vulnerabilities.
- Not done: three identical runs (G6 asks for three; this session made one clean run after fixes); a re-run of the gate rows is for the Testing role. No CI / PR work (no git writes).
