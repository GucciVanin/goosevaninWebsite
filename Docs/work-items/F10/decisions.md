# Decisions - F10

| Date | Decision | Outcome |
| --- | --- | --- |
| 2026-10-29 | Spec F10 (US1-US3) approved by Gustavo as drafted; merge of the pull request = final acceptance. | Approved |
| 2026-10-29 | Captain defaults, unless the Worker finds a conflict with the architecture: tokens are 32 random bytes, URL-safe, stored only as SHA-256 hashes; confirmation link valid 7 days (same as pending deletion); unsubscribe tokens do not expire; each subscriber has one confirmation token and one unsubscribe token; links point at client pages (`/subscribe/confirm?token=`, `/unsubscribe?token=`) that POST the token. Mail goes through `IMailService` (rendered SMTP path like account mail). The subscribe form lives in the footer and in a short home section; no other page. | Recorded; Gustavo may amend at review |
