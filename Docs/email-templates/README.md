# Contact email templates

Three self-contained HTML emails in the cyberpunk look of the redesign (near-black navy, neon magenta and cyan, monospace labels, corner-accent button). They use tables and inline styles so they render in Gmail, Outlook, and Apple Mail, and they work without web fonts.

| File | Sent to | When |
| --- | --- | --- |
| `client-confirmation.html` | The sender | Right after they submit the form, with the confirm link |
| `client-receipt.html` | The sender | After they confirm: acknowledgement with a copy of their message |
| `owner-notification.html` | Gustavo | After the sender confirms: the message itself, with a Reply button |

## Placeholders

Replace these `{{...}}` tokens before sending (MailerSend personalization uses the same double-brace syntax).

| Placeholder | Used in | Value |
| --- | --- | --- |
| `{{name}}` | all | Sender's name |
| `{{sender_email}}` | owner | Sender's email address |
| `{{reason}}` | receipt, owner | `Work or collaboration` or `Personal note` |
| `{{message}}` | receipt, owner | The message text. Line breaks are kept (`white-space: pre-wrap`) |
| `{{verification_url}}` | confirmation | The one-time confirm link (`https://<domain>/contact/verify#token=...`) |
| `{{site_url}}` | all | Public site address, for example `https://goosewebsite.example` |
| `{{logo_url}}` | all | Public address of the logo: `{{site_url}}/email/gcv-logo.png` |

Suggested subjects: `Confirm your email to send your message to Gustavo`, `Your message has been received`, and `New contact message from {{name}}`. Do not put the message text or the sender's email address in the subject of the sender's emails.

## Using them

- **They are the source for the MailerSend templates.** The contact emails are sent through the MailerSend HTTP API with a template id (`Contact:Templates`), by `ContactEmailDeliveryService`. The variable names above are exactly what the code sends (`Models/ContactEmailTemplateVariables.cs`); each template must use only these, spelled the same way, or the inbox shows the variable name instead of the value. `ContactEmailTemplateTests` pins them. Account verification emails still use plain-text SMTP.
- **Variables per template:** confirmation uses `name`, `verification_url`, `site_url`, `logo_url`; receipt and owner notification use `name`, `sender_email`, `reason`, `message`, `site_url`, `logo_url`. The confirmation request deliberately never includes `message`.
- **Escaping:** the values come from a public form. Confirm in MailerSend that `{{message}}` and `{{name}}` are HTML-escaped (a test message containing `<b>bold</b>` must show the tags as text). If a template uses triple braces or a raw-output option, switch it to the escaped form.
- **Logo:** `logo_url` is `{site_url}/email/gcv-logo.png` (file `src/GooseWebsite.Client/public/email/gcv-logo.png`). Mail clients fetch images from a public address, so the logo does not display in real inboxes while `PublicBaseUrl` is `localhost`; it will once the site is public. `Contact:LogoUrl` overrides the address, for example with an image hosted elsewhere. An inline (`cid:`) image was tried on 2026-10-06 and did not render in the MailerSend templates, so it was removed.
- **Privacy:** the receipt contains a copy of the sender's message, so it goes only to the verified sender address. Never log the rendered emails.

## What was checked

The three files were rendered in headless Chrome with sample values and reviewed visually at desktop width. They have not been tested in Gmail, Outlook, or Apple Mail, on a phone, or with dark-mode inversion. Test with a real send (for example through a MailerSend test message) before relying on them. Outlook for Windows ignores some CSS (the layout is tables, so it should degrade to square, flat colors).
