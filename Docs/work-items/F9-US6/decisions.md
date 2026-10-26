# Decisions - F9-US6

| Date | Decision or question | Outcome |
| --- | --- | --- |
| 2026-10-22 | Work continues in a new pull request for the rest of F9 (the earlier one, #20, was merged with F9-US5 in it). Branch `f9-emails-and-verification`; one commit per story (F9-US6, then F9-US7). | Gustavo accepted F9-US5 and asked for a new PR for the rest. |
| 2026-10-22 | Spec check against the story in `Docs/product-backlog.md`. Head start already exists: the three contact emails are MailerSend templates with the logo at `{PublicBaseUrl}/email/gcv-logo.png` (an inline `cid:` image did not render, decided 2026-10-06). Remaining: the account verification email (today plain-text SMTP) and checking in real mail clients. | Scope of this item: build the account verification email as HTML plus plain text in code. The logo is hosted on the site's own address (no third-party hosting), the same decision as the contact emails. |
| 2026-10-22 | Limits the Captain records rather than hides: (a) real mail clients (Gmail, Outlook, Apple Mail, phones, dark mode) cannot be run by an agent, and the logo cannot load from `localhost`; (b) the plain-text alternative of the three contact emails lives in the MailerSend dashboard, which the code cannot change. | AC1 is verified here by rendering the HTML in headless Chrome and by tests on structure and text; the real-client check is a manual checklist for Gustavo (`Docs/email-templates/client-check.md`, written by order O2). The contact templates' plain-text alternative is a follow-up that needs a dashboard action by Gustavo. |

Acceptance criteria (from `Docs/product-backlog.md`, F9-US6):
- **F9-US6-AC1** The contact acknowledgement and the account verification email, opened in common email clients, show the logo and follow the approved design, with a readable plain-text alternative.
- **F9-US6-AC2** The email content has no tracking or third-party resources, and links, expiry guidance and privacy rules are unchanged.
- **F9-US6-AC3** In the automated tests with the mail sink, recipient, count and message-content assertions still pass.
