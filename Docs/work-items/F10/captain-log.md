# Captain log - F10

| # | Report | Source | Verdict | Action |
| --- | --- | --- | --- | --- |
| 0 | Gustavo approved F10 US1-US3 as drafted (2026-10-29) | Human | approved | orders O1-O3 written; Worker dispatched for O1 |
| 1 | Worker report O1: complete; Subscriptions module, `AddSubscribers` migration, subscribe and confirm endpoints, footer and home form, `/subscribe/confirm` page; Karma 225, dotnet 222, container 922/0/0 (browser 303 x3 engines). Deviations: fragment link `#token=`, confirm by button not on open (AC3 wording), 2-minute resend cooldown, expiry-based cleanup, no valid subscription in runtime/browser checks, ASCII-only email regex. Doubts: no read filter on access rule, unsubscribe token only hashed (O2/O3 need a derivable token), timing difference between outcomes, hourly cleanup not run for real | Worker | complete | route to Reviewer and Security in parallel; deviations 1-4 and 6 accepted, deviation 2 to Gustavo at review (AC3 wording to be amended); unsubscribe-token design decided in decisions.md for O2 |
