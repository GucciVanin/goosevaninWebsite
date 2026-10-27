# Decisions - R1 release gate

Intent: decide with evidence whether Release R1 (credible home base) can ship. R1 exit measure (`Docs/product-backlog.md`, Release Map): all R1 feature gates pass; no sensitive profile fields are exposed; only authenticated, provisioned administrators can access editing; no public project/story content or comments are required.

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-26 | The gate has no written checklist; the Captain drafted it below. It is documentation plus evidence (no new feature code); any failed row becomes a new work item. It runs last, after TE4 and the F3-US1 close-out. | Pending Gustavo's approval of the draft. |

Draft gate checklist (each row: pass with evidence, or fail with a work item):
- **R1-G1** Features F1, F2, F3 (stories F3-US1 and F3-US3 plus the R1 parts), F4, F7 and F9 are Done in the backlog, each with its evidence paragraph and no open acceptance test.
- **R1-G2** No sensitive profile fields exposed: a test or scan of every public page and public API response shows no age, exact location, email addresses other than Gustavo's public one, tokens or internal ids (the public blog and project APIs no longer include the author's account id: check and fix if not).
- **R1-G3** Only authenticated administrators can edit: the editor access matrix (EditorAccessTests, the architecture test on write actions) and the runtime anonymous/reader checks pass.
- **R1-G4** No sample content: TE4 done.
- **R1-G5** HTTPS and cookies: the runtime HTTPS and cookie-flag checks pass in the container (`20-https.sh`, `60-auth.sh`); HSTS max age is the production value in the production configuration (the verification stack uses 300 seconds: record the production value and where it is set).
- **R1-G6** Quality gates: CI green on the release pull request; the full container verification passes with 0 failures and 0 known issues, three runs identical; the dependency audit shows no high or critical finding in production dependencies (`dotnet list package --vulnerable`, `npm audit --omit=dev`).
- **R1-G7** Secrets and privacy: no secrets in the repository (a secret scan of the history and tree), logs contain no message bodies, addresses or tokens (the existing log-content tests pass).
- **R1-G8** Known gaps are listed honestly: real Edge and Safari manual check (`Docs/tests/browser-manual-check.md`), real-client email check (`Docs/email-templates/client-check.md`), the contact verification flow against the real SMTP provider, and the deployment itself (not done: the site is only in the local container).
- **R1-G9** Gustavo's sign-off recorded in `Docs/product-backlog.md` and `Docs/project.md`.

| 2026-10-26 | Gustavo approved the draft checklist R1-G1 to R1-G9 as drafted. | Runs last. |
