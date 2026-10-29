# TE12 - Remove the MailerSend API path
Issue: https://github.com/GucciVanin/goosevaninWebsite/issues/28

After TE11 nothing sends template mail. Remove the MailerSend API transport, template types, options, secrets, settings and tests, and replace the `45-mail.sh` API-key check with a read-only SMTP login check that sends no email, so the runtime tests prove the credential that is actually used. Update the compose file, `.env.example` and the architecture and email-template docs in the same change. Depends on TE11. Spec: row TE12 in `Docs/project.md`.
