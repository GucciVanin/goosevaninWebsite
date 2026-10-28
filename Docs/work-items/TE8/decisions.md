# Decisions - TE8 (one Mail module)

Source: GitHub issue #5 (architecture review 2026-10-06, candidate 4). Its premise: after F9-US6 moves the account verification email onto branded **MailerSend templates**, replace the two mail seams (Accounts: plain-text SMTP; Contact: MailerSend templates) with one Mail module, and delete SMTP if unused.

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-26 | The premise changed: F9-US6 built the account verification email in code as HTML plus plain text and still sends it over SMTP, because a MailerSend template for it would have to be created in the MailerSend dashboard, which the code and the agents cannot do. | Open question for Gustavo (see below). TE8 stays at `spec` until answered. |

Options (the Captain recommends B):
- **A. Move the account email to a MailerSend template** (as the issue says): needs Gustavo to create the template in the dashboard and give its id; then SMTP can be deleted. Cannot be verified here.
- **B. One Mail module with one interface and two transports** (recommended): callers say what to send (recipient, subject, HTML, plain text, or a template id and variables) and get one failure model, one place that enforces the privacy logging rule, one configuration section and one test double; MailerSend (templates) and SMTP (rendered content) stay as transports behind it. Contact emails keep working unchanged; nothing needs the dashboard.
- **C. Render the contact emails in code as well and send everything over one transport**: removes MailerSend templates; changes the contact flow and the templates Gustavo already created.
- **D. Defer TE8 past R1**: no R1 gate depends on it; the duplication is a maintenance cost only.

Draft acceptance criteria for B:
- **TE8-AC1** One `Shared/Email` mail interface used by both Accounts and Contact; no module talks to SMTP or MailerSend directly.
- **TE8-AC2** One failure model (returns success or failure, never throws to callers, no addresses, bodies, tokens or provider response text in logs) enforced in one place, with a test that captures log output for success, failure and unconfigured cases and finds no address, link or body.
- **TE8-AC3** Contact emails behave exactly as before (the ContactEmailTemplateTests that pin the variables and the contact API tests pass unchanged); the account verification email is unchanged in content (the F9-US6 tests pass unchanged).
- **TE8-AC4** One test double for mail in tests replaces the per-module doubles.
- **TE8-AC5** One set of mail settings documented in `Docs/architecture.md` section 7 and `.env.example`, and the architecture and tech-stack lines updated.

| 2026-10-26 | Gustavo chose approach B (one module, two transports) and approved the draft acceptance criteria TE8-AC1 to AC5. | Runs after TE4. |
| 2026-10-28 | Gustavo: move to the next tasks (R1 sign-off: TE8, then F3-US1 evidence, then the R1 gate). Branch r1-signoff, stacked on f5-public-browsing because PR #23 could not be checked (GitHub unreachable from this machine). | Status plan -> execute. |
| 2026-10-28 | Worker O1 implemented approach B: `IMailService` with `MailService` (one logging rule), `SmtpMailTransport` and `MailerSendTransport`, `Email:MailerSend:*` settings with the older `MailerSend:*` as fallback, `TestMailService` as the one mail double. Verification pending; TE8 not Done. | Awaiting review. |
| 2026-10-28 | AC4 read as "one test double for the mail seam": `TestMailService` replaces the per-module mail doubles; `StubContactEmailDeliveryService` stays because it doubles the Contact-level service, not a mail seam. In Docker only the `.env` variable names are supported: compose always sets `Email__MailerSend__*`, so a raw `MailerSend__*` container variable would be shadowed by empty values. | Recorded (Reviewer advisories 2 and 3). |
