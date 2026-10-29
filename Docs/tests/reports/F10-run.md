# Testing report - F10 (F10-US1, F10-US2, F10-US3), three runs

Role: Testing. Date: 2026-10-29. Branch `f10-subscriptions`, HEAD `6491476` (all F10 work committed, working tree clean before the run). No code edited, no git write commands.

**Final verdict: PASS** (all layers green three times, per-test lists identical, no AC without a test; open gaps listed below are the known, unchecked-by-automation ones).

## Environment

- Windows 11 Pro 10.0.26200, Docker 29.8.2, images `goosewebsite-web:latest` (31ccca47864c) and `goosewebsite-verify:latest` (e1fa8a42d4b8), rebuilt by each `verify.ps1` run.
- .NET 10 SDK (net10.0 test host), Angular 20 Karma on Chrome Headless 156.0.0.0 (Windows 10), Playwright 1.64.1 (Chromium, Firefox, WebKit) inside the verification container.
- The only environment is the local container stack (CLAUDE.md); CI was not part of this run (`-SkipCi`, no pull request yet).
- The per-test comparison is on test names and outcomes (timings removed, sorted), taken from the TRX files (xUnit), the Karma spec reporter output, and `verify-results/latest.log` (copied after each run).

## Per-run totals

| Layer | Run 1 | Run 2 | Run 3 | Per-test lists |
| --- | --- | --- | --- | --- |
| `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | 279 passed, 0 failed, 0 skipped | 279 / 0 / 0 | 279 / 0 / 0 | identical (279 names, all Passed) |
| Karma, README command (spec reporter, `--karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox`, via `cmd /c ... > file 2>&1`) | 234 of 234 | 234 of 234 | 234 of 234 | identical (234 names, all passed) |
| Karma, default reporter (`npm test -- --watch=false --browsers=ChromeHeadless`, via `cmd /c`) | 234 of 234 | 234 of 234 | 234 of 234 | count only (the default progress reporter prints no names) |
| `./scripts/verify.ps1 -SkipCi` | 1022 passed, 0 failed, 0 skipped, 0 known (exit 0) | same | same | identical (1022 lines each: unit-dotnet 279, unit-angular 234, runtime 186, browser 323 incl. the "results complete" line, 321 of 321 listed browser tests) |

Determinism verdict: **deterministic**. The sorted per-test lists of all three runs are byte-identical in each layer; no test changed outcome.

Rate limits: `verify.ps1` backs up, resets and rebuilds the stack at the start of each run, so every run began with fresh 10-minute windows. No 429 appeared outside the checks that expect it (`the unsubscribe endpoint is rate limited per client` passed in all three). No restart of `goosewebsite-web` between runs was needed beyond what `verify.ps1` does itself. The stack only ever received refusals (never a valid subscription), so no email can have been sent; no real mail was sent.

## The Karma count difference (Worker: 201 direct vs 234 in verify.ps1)

Not reproducible. Both the direct run with the default `ChromeHeadless` command (as in the order) and the README spec-reporter command gave **234 of 234** in all three runs, the same number `verify.ps1` counts (unit-angular 234). The default progress reporter prints a running "Executed N of 234" line on one rewritten line (carriage returns), so a number like 201 read from a captured file or a partially viewed progress line is the most likely source, with the total (234) in the same line. The worker's "201 of 201" cannot be confirmed from anything observed here: the suite size is 234 in every configuration. Conclusion: no real difference between the direct run and verify.ps1; the 201 in `worker-O4.md` is a misreport (cause not provable after the fact). The Karma specs ran in randomised order in each run, and the sorted lists still match.

## Manual checks on the running stack (https://localhost, after run 3)

| Check | Result |
| --- | --- |
| `/subscribe/confirm` loads, title "Confirm subscription - Gustavo Couto Vanin", no site header/nav and no footer (only the page's own "Back to the site" link row) | pass; says "This confirmation link is no longer valid." with no token |
| `/unsubscribe` loads, title "Unsubscribe - Gustavo Couto Vanin", no site header/nav and no footer | pass; says "This unsubscribe link is no longer valid." with no token |
| Home at 375 px: `scrollWidth` 375 = `innerWidth` 375, no horizontal scroll | pass |
| Home at 375 px: the "Get new work by email" section form (288 px wide, right edge 331) and the footer form (338 px wide, right edge 356) are inside the viewport, each with name, email and a honeypot input | pass |

Measured by DOM inspection in the built-in browser (a screenshot of the footer was not obtained because the page scroll did not follow the programmatic scroll; the numbers above are from layout measurements). Viewport emulation was reset to desktop afterwards.

## AC-to-test map

xUnit names are `SubscriptionsApiTests.*` or `AnnouncementsApiTests.*` unless noted. All passed in all three runs.

### F10-US1

| AC | Tests |
| --- | --- |
| AC1 pending record and one confirmation mail | `Subscribe_StoresAPendingSubscriberAndSendsOneConfirmationEmail`; `TheConfirmationEmailIsBrandedEncodesTheNameAndLinksToTheClientPage` |
| AC2 identical response for every submission | `EveryKindOfSubmissionIsAnsweredIdentically`; `WhenTheMailProviderFails_TheAnswerIsTheSameAndTheRecordStaysPending`; `ASecondSubmissionOfAConfirmedAddress_SendsNoMailAndChangesNothing`; `AnInvalidEmailStoresNothingAndSendsNothing` (5 cases); `ResubmittingAPendingAddress_SendsAFreshLinkWithoutASecondRecord_AndTheOldLinkStopsWorking`; `ResubmittingAPendingAddressAgainAtOnce_DoesNotSendAnotherMail`; `TheEmailAddressIsUniqueIgnoringCase`; Karma `SubscribeForm` (answer in words) |
| AC3 confirm on the page; expired, malformed, reused change nothing | `Confirm_MakesTheSubscriberConfirmed_AndTheLinkWorksOnlyOnce`; `Confirm_RefusesAnExpiredLinkAndChangesNothing`; `Confirm_RefusesMalformedOrUnknownTokensWithTheSameAnswerAndChangesNothing` (4 cases); `OpeningTheConfirmationEndpointWithGet_NeverConfirms`; Karma `SubscribeConfirmPage` (5 tests); browser `subscribe-confirm:` (2 tests x 3 engines); runtime `route /subscribe/confirm serves the app` |
| AC4 pending record deleted after 7 days | `CleanupDeletesPendingRecordsWhoseLinkExpiredAndKeepsEverythingElse`; `TheCleanupJobIsRegisteredAsAHostedService` (the real 7-day timer was not run; the test drives the cleanup with an advanced clock) |
| AC5 honeypot and per-IP limit | `AFilledHoneypotStoresNothingAndSendsNothing`; `ExceedingThePerIpLimitStoresNothingMoreAndSendsNothingMore`; `ASubmissionWithoutTheCsrfTokenIsRefused`; runtime `a subscription without a CSRF token is rejected`; Karma honeypot tests (2) |
| AC6 stored columns | `TheStoredRecordHoldsOnlyTheDocumentedColumns_AndTokensAreOnlyHashed`; `NoEndpointListsSubscribers` (+ runtime `no endpoint lists subscribers`) |
| AC7 safeguards, never logged | `TheAccessRuleLimitsTheNameTo100AndTheEmailTo254AndTreatsThemAsUserText`; `TheSafeguardsRefuseAnOverLongNameBeneathTheService`; `AnEmailOver254CharactersOrANameOver100IsNotStored`; `NeitherTheNameNorTheEmailNorTheTokenIsEverLogged`; Karma `limits the name to 100 characters` |
| UI (tasks) | Karma `SubscribeForm` (12), browser `subscribe-section`, `subscribe-footer`, `subscribe-home`, `subscribe-confirmation` (load, axe, 320 px, keyboard) on Chromium, Firefox, WebKit |

### F10-US2

| AC | Tests |
| --- | --- |
| AC1 valid link deletes the record and the page says so | `Unsubscribe_DeletesTheConfirmedSubscriberAndOnlyThatOne`; `Unsubscribe_AlsoRemovesAPendingSubscriber`; `TheUnsubscribeLinkHasNoExpiryOfItsOwn_ItOnlyStopsWorkingWhenTheSubscriberIsDeleted`; `TheUnsubscribeLinkIsNotStored_AndEveryLinkEverMailedStaysValid`; Karma `UnsubscribePage` (9); browser `unsubscribe:` (6 x 3 engines) |
| AC2 invalid or used link: nothing changes, neutral | `Unsubscribe_RefusesUsedTamperedAndUnknownLinksWithOneNeutralAnswerAndChangesNothing`; `AnOldUnsubscribeLinkNeverRemovesALaterSubscriptionOfTheSameAddress`; `TheSharedLinkServiceReadsItsOwnTokens_AndAnotherKeyRingCannotForgeOrReadThem`; `UnsubscribingNeedsAPostWithTheCsrfToken_AndFetchingTheLinksChangesNothing`; runtime `an unknown unsubscribe token is refused with 400`, `an empty unsubscribe token is refused with the same status`, `an unsubscribe link opened with GET only serves the app, it never unsubscribes`, `a GET of the one-click address does not unsubscribe`, `an unsubscribe without a CSRF token is rejected`, `route /unsubscribe serves the app` |
| AC3 unsubscribe link and `List-Unsubscribe` in every mail | `EverySubscriptionMailCarriesTheUnsubscribeLinkAndTheListUnsubscribeHeaders`; `TheSmtpTransportWritesTheCustomHeadersIntoTheMessage`; `AMailWithoutCustomHeadersIsStillValid`; `TheSharedLinkServiceGivesLaterMailsTheSameLinkAndHeaders`; `TheOneClickEndpointForMailClientsNeedsThePostBodyAndNeedsNoCsrfToken`; runtime `the one-click endpoint refuses a POST without the List-Unsubscribe=One-Click body`; `AnnouncementsApiTests.EveryMailIsPersonal_ItsOwnGreetingUnsubscribeLinkAndHeaders` |
| AC4 nothing sent to a deleted subscriber | `AnnouncementsApiTests.ASubscriberWhoUnsubscribedBeforeTheMailWasSentGetsNothing`; `ASubscriberWhoConfirmsAfterThePublishIsNotToldAboutEarlierWork`; `AMissingSubscriberRecordOrAnAnnouncementOfDeletedContentNeverBreaksARound` |
| Abuse and privacy | `TheUnsubscribeEndpointIsRateLimitedPerClient`; runtime `the unsubscribe endpoint is rate limited per client`; `UnsubscribeTokensAndAddressesAreNeverLogged` |

### F10-US3

| AC | Tests |
| --- | --- |
| AC1 first publish, each confirmed subscriber once, no pending one | `TheFirstPublishOfAStoryMailsEachConfirmedSubscriberOnce_AndNoPendingOne`; `TheFirstPublishOfAProjectMailsEachConfirmedSubscriberOnceWithTheOverviewAsSummary`; `EveryMailIsPersonal_ItsOwnGreetingUnsubscribeLinkAndHeaders`; `TheSummaryIsPlainEscapedAndShort_AndTheSubjectIsOneCleanLine`; `TwoSimultaneousFirstPublishesOfAStory...` and `...OfAProject...` (NeitherFailNorMailAnyoneTwice) |
| AC2 edit, unpublish, republish: no second mail | `EditingRepublishingAndPublishingAgainNeverMailAgain_AndTheMarkerIsSetOnce`; `AProjectIsAnnouncedOnlyOnItsFirstPublishToo`; `ContentPublishedBeforeSubscriptionsExistedIsMarkedAsAnnounced_SoRepublishingItStaysSilent` (real migration on an old-shaped database); `APublishThatIsRefusedAnnouncesNothing_AndThePublishThatFollowsDoes`; runtime `publishing the story again succeeds (it is announced only on the first publish)`, `the republished story can be returned to draft again`, `publishing again keeps the same publication date` (x2) |
| AC3 draft sends nothing | `SavingDraftsAndEditingThemSendsNothingAndQueuesNothing`; runtime `saving does not publish`, `saving edits does not publish` |
| AC4 provider failure never fails the publish, retry without duplicates | `PublishingReturnsBeforeAnyMailIsSent_AndAProviderThatFailsEverythingNeverFailsThePublish`; `AMailProviderThatFailsAfterThePublishNeverUndoesItNorTheMarker`; `APartialFailureIsRetriedLaterWithoutMailingTheRecipientsAlreadyReached`; `ARecipientThatKeepsFailingIsGivenUpAfterTheConfiguredAttempts_AndTheAnnouncementCloses`; `TheHostedLoopDeliversInTheBackgroundWithoutTheEditorRequestWaiting` |
| AC5 no subscriber data in response or logs | `NeitherThePublishResponseNorTheLogsNorTheQueueTablesHoldSubscriberNamesOrAddresses`; `TheQueueKeepsOnlyIdsAndCounters`; runtime `the publish response holds no subscriber or announcement data` (x2), `the publish response does not expose the announced marker` (x2) |
| AC6 no subscribers or only pending | `APublishWithNoSubscribersSendsNothingAndFailsNothing`; `OnlyPendingSubscribersMeansNothingIsSent`; runtime `an administrator can publish a complete draft` / `...project` (stack has no subscribers) |
| Dependency rule | `BlogAndProjectsDoNotReferenceTheSubscriptionsModule_TheyOnlyCallTheSharedAnnouncer`; `WithoutTheSubscriptionsModuleThePublishStillWorks_BecauseSharedProvidesAQuietAnnouncer` |

### Security and Reviewer follow-ups

| Item | Tests |
| --- | --- |
| Security O1 S4 (Zl, Zp, Cf stripped from the name) | `TheNameLosesLineSeparatorsParagraphSeparatorsAndFormatCharacters` |
| Security O1 S2 (rate limit keyed on the real client behind Caddy) | covered by hand in `Docs/work-items/F10/worker-O2.md` (S2 evidence); automated only as "the limit exists" (`TheUnsubscribeEndpointIsRateLimitedPerClient`, runtime 429 check). Not re-proven by this run (see gaps) |
| Security O1 PublicBaseUrl documented | docs only, no test; the links in the mail tests use the configured base |
| Security O2 S1 (key ring guard) | `StartingWithAnEmptyKeyRingWhileSubscribersExistLogsAWarningWithoutPersonalData`; `AnEmptyKeyRingWithNoSubscribersIsNotWorthAWarning_AndAnIntactRingNeverWarns` |
| Security O2 S2 (one-click size limit) | `TheOneClickActionHasATinyRequestAndFormLimit`; `AnOversizedOneClickFormIsRefusedAndChangesNothing` |
| Security O2 S3 (own higher rate limit) | `TheOneClickAddressHasItsOwnHigherRateLimitThanConfirm`; `TheOneClickLimitDefaultsToMoreThanTheConfirmLimit` |
| Security O2 S4 (query string never logged) | `TheOneClickQueryStringIsNeverLogged_AndNothingInTheDeploymentWritesAnAccessLog` |
| Security O2 S6 (retry keeps the token on 429/5xx/network) | Karma `UnsubscribePage`: `keeps the token and the button for a retry after a rate limit (429)`, `... server error (500)`, `... network error`, `treats only an invalid token (400) as final and drops the token` |
| Reviewer O2 advisories (purpose isolation, renamed no-expiry test) | `ATokenProtectedUnderAnotherPurposeIsRejected`; `TheUnsubscribeLinkHasNoExpiryOfItsOwn_ItOnlyStopsWorkingWhenTheSubscriberIsDeleted` |
| Security O3 S1 and Reviewer O3 advisory 1 (atomicity) | `AStoryWhoseAnnouncementFailsStaysADraftUnmarkedAndUnqueued_AndPublishingAgainWorks` and the project variant (modes `BeforeQueue`, `AfterQueueSaved`) |
| Security O3 S2 (cancellation) | `TheCancellationTokenOfARoundReachesTheMailService`; `ARoundCancelledDuringASendEndsPromptly_AndTheRecipientIsNotCountedAsFailed` |
| Security O3 S3 (regex timeouts) | `AVeryLongRunOfBracketsInASummaryIsHandledQuickly` (opener `[` and `!`); the Worker notes it is a regression guard only |
| Worker O4 (c) concurrent first publish | `TwoSimultaneousFirstPublishesOfAStory...` / `...OfAProject...` |

**ACs with no test: none.** Partial or indirect coverage to note:

- F10-US1 AC4: the 7-day timer itself has not run for 7 days; the cleanup logic and registration are tested with an advanced clock.
- F10-US2 AC3 and F10-US3 AC1/AC4: checked against a test mail service and the MIME message the SMTP transport builds; never against a real provider.
- F10-US3 AC1 at runtime: the stack has no subscribers (by design), so no end-to-end mail on the container; delivery is proven only by xUnit.
- Reviewer O4 advisories (untested branch where the mail service returns a failed result after cancellation; no test for text near or over the 4000 cap or the timeout fallback; the concurrency test may pass by plain SQLite write-lock serialisation) have no test; they are logged as a follow-up, not blocking.

## Gaps not covered by this run

1. Real SMTP and MailerSend delivery of the confirmation and announcement mails (open gaps 2 and 3 in CLAUDE.md); the stack never sends a valid subscription.
2. The mails in real mail clients, including how clients honour `List-Unsubscribe` and `List-Unsubscribe-Post` (`Docs/email-templates/client-check.md`).
3. Real Edge and Safari by a person (`Docs/tests/browser-manual-check.md`); Chromium, Firefox and WebKit engines were run.
4. Security O1 S2 (real client address behind Caddy) was not re-measured from a second address in this run; relies on the earlier hand evidence in `worker-O2.md`.
5. CI (`gh pr checks`) was not part of this run (`-SkipCi`, no pull request yet).
6. Delivery is at least once (a crash between the provider accepting a mail and the row being saved mails that recipient again); not testable here.

## Final

**PASS.** dotnet 279/279, Karma 234/234, verify.ps1 1022 passed / 0 failed / 0 skipped / 0 known, each three times with identical per-test lists; manual page checks pass; all F10-US1, US2, US3 ACs and follow-ups map to passing tests.

Containers: only `goosewebsite-caddy` and `goosewebsite-web` were running from this project at the end; both are stopped by name after this report was written (the `goosewebsite-verify` run container is removed by the script with `--rm`).
