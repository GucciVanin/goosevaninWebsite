# Product Goals and Agile Backlog

## Purpose of This Document

This is the living product specification and delivery backlog for Gustavo Couto Vanin's personal website (GooseWebsite). It records **why** the product exists, **what outcome** each feature must deliver, and **how completion can be verified**. It is intended for Gustavo, developers, testers, and AI agents working on the project. Read this document before changing product behavior.

It is the detailed layer of a three-level documentation set. [goal.md](./goal.md) states the ultimate purpose in plain language, [project.md](./project.md) turns that purpose into a Scrum roadmap, and this file holds the stories and acceptance tests. [architecture.md](./architecture.md) describes how the code is built. If this file and `goal.md` ever disagree, `goal.md` wins.

This backlog describes the agreed target, not a claim about what the application already does. Existing code, architecture notes, and this backlog can differ. Do not mark work complete based only on finding related code: run the listed checks, update the status, and record any changed decision here.

### How to Maintain the Backlog

- Keep feature goals, user stories, acceptance tests, and measurable outcomes together. Update this file when a goal, decision, scope, estimate, or acceptance result changes.
- Use these statuses: `Proposed`, `Ready`, `In Progress`, `Blocked`, and `Done`. New work starts as `Proposed`. Move it to `Ready` once dependencies and implementation decisions are clear.
- A feature is `Done` only when its completion criteria are met, its included stories pass their acceptance tests, and the evidence (test name, report, or manual check) is noted here.
- A user story is `Done` only when every acceptance test passes, its tasks are complete, and the change is reviewed. A task is a concrete implementation or verification step, not another product goal.
- When scope changes, revise the affected acceptance tests and effort estimate; preserve the reason for a significant change in the decision log.
- Treat measurable targets below as release gates. If a target proves unsuitable, change it explicitly rather than silently weakening the check.

### Effort Unit

`1 US = 12 hours of effort`. In this document, US is an effort unit requested for planning; it is not a relative Fibonacci-style Agile story-point scale. Estimates cover implementation, tests, and integration for the story. They are planning estimates, not deadlines. Feature totals are the sum of their user-story estimates.

### Product Purpose

Build a credible public home for friends, family, and professional visitors. Give projects and selected personal stories equal care: visitors should be able to explore Gustavo's work, get to know him through public stories, and contact him easily. The project is not a private diary or a social network.

### Gustavo's Persona

Gustavo Couto Vanin is the site's author and a site administrator. More than one administrator account may exist, and each must be provisioned through the authorized private setup path. In product and public-facing language, use **Gustavo** or **Gustavo Couto Vanin**, not the generic phrase “the owner.” In technical authorization language, use **site administrator**. Do not infer or publish any additional identity or account details.

- Positioning line: **Engineer interested in people and technology.**
- Professional background: software engineering and past product security advisory work, including engineering contributions. Do not retitle the advisory role as “product security engineer.”
- Professional directions of interest: software engineering, product security, security engineering, and exploration of new domains. Let case studies demonstrate capabilities; do not invent credentials, outcomes, or expertise.
- Personal voice: curious and thoughtful. Philosophy and interest in human behavior belong mainly in personal writing rather than being forced into the professional pitch.
- Stories may cover learning, games, observations about people, and selected life milestones. Select specific topics and details story by story.
- Keep his age private. Do not list specific games, books, or current studies in the profile by default; Gustavo may choose to name them in an individual story.

### Visual and Interaction Direction

**Status:** Superseded in part by Feature F9 (2026-10-02): Gustavo wants a full redesign from his own logo and an inspiration image. Until F9 is Ready and its design analysis replaces this section, treat the typography, color, and composition details below as the current implementation, not the target. The accessibility, content, and privacy rules in this section still apply. Originally: proposed from Gustavo's confirmed persona; palette and visual metaphor were not separately approved. Treat this as the working design brief and record any later decision in the decision log.

The site should feel like a **humanist engineering journal**: precise enough to make Gustavo's engineering work easy to inspect, but editorial and personal enough to make his writing feel authored. Avoid a generic cybersecurity dashboard, hacker/terminal stereotype, gaming-themed portfolio, or marketing landing page. Carry forward the current editorial serif, sans-serif, and monospace typography and existing dark ink, mint, and coral tokens, while adding restrained light reading surfaces so long-form text is comfortable and the page does not become a single-color field.

- **First viewport:** show Gustavo's name, the positioning line “Engineer interested in people and technology,” a concise introduction, his portrait, and a clear contact action. Do not use age, oversized marketing claims, or a decorative hero card.
- **Typography:** use the existing display serif for the name and major editorial headings, the sans-serif for readable body copy and controls, and monospace only for compact dates, section indices, tags, and technical metadata. Keep body copy at least 16 CSS px, use a readable line length of roughly 60–75 characters for long text, and preserve a clear heading hierarchy.
- **Color:** use the current tokens as the dark surface: `--paper #071013`, `--ink #d8fff2`, `--muted #7ca69d`, `--line #183a3b`, `--accent #ff4f8b`, and `--accent-soft #80ffcf`. Retain dark ink for the masthead/hero and footer, and introduce a light neutral reading surface (working proposal: `#eef1e8` with dark text `#172622`) for long-form content. Reserve mint for links or focus/secondary highlights, and coral for primary actions or a small number of emphasis marks. Keep text/background contrast at WCAG AA or better; never use color alone to convey state.
- **Composition:** favor full-width, unframed editorial sections with a consistent content column. Use compact repeated project/story entries only where they help comparison or scanning; avoid nested cards and oversized rounded panels. Use subtle rules, numbering, or grid details as quiet engineering-journal cues, not as decoration that competes with content.
- **Projects:** prioritize the problem, Gustavo's specific contribution, decisions, tools, and verifiable outcomes. Give demo/source links clear labels and visible focus states.
- **Stories:** use a calmer reading layout with publication date and tags as secondary metadata. Let Gustavo's curious, thoughtful voice carry the personality; do not force hobbies or technical branding into each story.
- **Contact:** keep the direct email route and form findable, with concise labels, visible validation, and a clear verification-pending state. Do not present the contact form as an in-site message inbox.
- **Editor:** make Gustavo's private editor compact and task-focused, visually distinct from public storytelling without introducing a second design language. Draft/published state, preview, save, and publish actions must be unambiguous.
- **Responsive behavior:** preserve the same content priority on narrow screens; stack columns, keep controls within the viewport, and do not hide essential profile, project, story, or contact content behind hover-only behavior.
- **Motion and imagery:** use only restrained transitions that clarify navigation or state; respect reduced-motion preferences. Use real, relevant portraits/project/story imagery when supplied, never fabricated project evidence or decorative cybersecurity stock imagery.

The visual direction is accepted when the implemented public pages and editor pass their feature-level checks and a desktop/mobile review confirms the hierarchy, readability, section distinction, and contrast described above. Aesthetic preference changes require Gustavo's explicit confirmation; update this section rather than silently drifting from it.

### Confirmed Product Decisions

- Personal stories are public by default, selected by Gustavo, and may use a different voice from the professional project section. Do not publish sensitive details by default.
- The first public release includes Gustavo's profile, a direct email route and contact form, and a private editor accessible only to authenticated, provisioned administrator accounts.
- Gustavo and any additional administrator accounts are provisioned through the authorized private setup path; public reader registration must never create or grant administrator access. Every provisioned administrator may use the private editor. Reader accounts are introduced in R3 for verified commenting.
- Release one does not require public project/story pages, sample project/story content, or public comments. The editor still supports draft, preview, and publish states; public browsing is a later release goal.
- The contact form asks for name, email, reason (`Work or collaboration` or `Personal note`), and message. The sender verifies their email before the message is delivered to Gustavo's configured contact address. Do not keep a durable site inbox or archive. Retain pending message data only as long as strictly needed for verification and delivery, then remove it; avoid logging message contents or email addresses.
- Gustavo's public profile includes his name, short bio, broad location/background, professional summary, photo, and selected links. Do not expose his age, exact location, or other sensitive personal details.
- Only authenticated accounts provisioned with the administrator role can create or manage project and story content. Story fields: title, body, publication date, cover image, and tags. Project fields: title, overview, Gustavo's role/contribution, tools/technologies, images, and demo/source links. Images are uploaded from an administrator's device.
- Later comments use open reader registration with verified email. Comments appear immediately on projects and stories, display a reader-selected name, keep email private, allow replies, and let readers edit or delete their own comments.
- Member profiles (R3, feature F8): signed-in users can have a profile that other users can view, with hobbies, profession, location, and a short about-me. Everything beyond the display name is optional and skippable, and email is never shown.

### Release Map and Measures

| Release | Included outcomes | Exit measure |
| --- | --- | --- |
| R1: Credible home base | Gustavo's public profile, contact form/email delivery, provisioned administrator account(s) and sign-in, private editor for projects and stories, and the new brand design (F9) | All R1 feature gates pass; no sensitive profile fields are exposed; only authenticated, provisioned administrators can access editing; no public project/story content or comments are required |
| R2: Explore the work and stories | Public project and story browsing, with published content only | Every published item is reachable from its section; drafts are never visible publicly; project and story details expose the agreed fields |
| R3: Reader conversation | Verified reader accounts, public comments on projects and stories, and optional member profiles | Readers can register and verify email; only verified readers can comment; comment privacy and author-ownership rules pass; accepted comments display without Gustavo's pre-approval |

Overall success is observable when a new visitor can identify Gustavo and his professional focus, reach his contact route, and (after R2) explore both his work and selected stories without encountering drafts or private information. Do not use traffic or posting frequency as a release gate; neither was selected as a success requirement.

## Feature F1: Public Profile

**Release:** R1  
**Status:** Done
**Goal:** Present a concise, credible introduction that works for personal and professional visitors while protecting personal privacy.

**Feature is complete when:** Gustavo's agreed profile content is present; the photo and links work; the page follows the visual direction above; the page is usable on supported mobile and desktop viewports and with keyboard navigation; no age, exact location, or other sensitive personal details are exposed; and all F1 acceptance tests pass.

### User Story F1-US1: Understand Who Gustavo Is

**Status:** Done  
**Estimate:** 1 US (12 hours)  
**Goal:** As a visitor, I can understand who Gustavo is, what he has worked on, and what he is interested in from the public profile.

**Acceptance tests:**

1. Given a visitor opens the home page, when the profile renders, then Gustavo Couto Vanin's name, the positioning line “Engineer interested in people and technology,” short bio, broad location/background, and professional summary are visible.
2. Given profile content is reviewed, when the page is inspected, then the photo and selected links are present and usable, and neither age, exact location, nor other sensitive detail is exposed.
3. Given a visitor activates a profile link, when navigation completes, then the intended destination opens successfully.
4. Given the first viewport is reviewed at desktop and mobile widths, when the profile is rendered, then name and positioning are the strongest text hierarchy, the portrait is visible, and a contact action is available without scrolling past a promotional hero.

**Tasks:**

- [x] Identify the single source of editable profile content and add Gustavo's confirmed profile fields and positioning line.
- [x] Render Gustavo's profile and photo with appropriate alternative text; label links by their destination.
- [x] Review desktop and mobile first-viewport hierarchy against the visual direction; test links and sensitive-field omission and record results.

**Implementation evidence (2026-09-26):** Profile content comes from the shared typed source in `src/GooseWebsite.Client/src/app/shared/models/profile.ts` and is rendered through the hero, header, footer, and contact surfaces. `src/GooseWebsite.Client/public/Headshot.svg` is rendered in the first viewport with descriptive alternative text. The production Angular build and Docker deployment pass. Browser review at 1440x900 and 320x800 confirmed the profile hierarchy, portrait visibility, no horizontal overflow, and no overlap after the responsive hero fix. The hero contact button scrolls to `#contact`; the header contact action is visible. GitHub returned HTTP 200; LinkedIn's configured URL is present and opens in a new tab, but automated requests are blocked by LinkedIn's anti-bot response. Page review found no age, exact-location, or other sensitive profile fields.

### User Story F1-US2: Use the Profile Accessibly

**Status:** Done  
**Estimate:** 1 US (12 hours)  
**Goal:** As a visitor, I can read and navigate the profile on common screen sizes and without a mouse.

**Implementation evidence (2026-09-28):** The public profile was reviewed and adjusted for responsive behavior and keyboard usability. The profile keeps readable layout and no horizontal overflow at 320, 375, 480, 800, and 1024 CSS-pixel widths, and the site now includes explicit focus styling for interactive elements alongside reduced-motion handling. The Angular build passes after the updates. The content remains within the approved humanist engineering journal direction, with the existing typography and restrained color system retained.

**Acceptance tests:**

1. Given a 320 CSS-pixel-wide viewport, when the page is loaded, then profile text and controls remain visible without horizontal page scrolling or clipped text.
2. Given keyboard-only input, when the visitor tabs through the page, then every link is reachable in a logical order and has a visible focus state.
3. Given an automated accessibility scan of the profile, when violations are reviewed, then there are zero critical or serious violations attributable to the profile feature.
4. Given the profile and first viewport are inspected, when typography and colors are checked, then the display serif, sans-serif body, restrained monospace metadata, and stated color roles are used consistently.
5. Given text and controls are tested against their backgrounds, when contrast is measured, then text meets WCAG AA contrast and focus/state information is not conveyed by color alone.

**Tasks:**

- [x] Implement the profile using the humanist engineering journal direction, existing type families, balanced reading surfaces, and responsive content column.
- [x] Check keyboard order, focus visibility, image alternative text, heading structure, text contrast, and reduced-motion behavior.
- [x] Run the project's available accessibility check and browser checks at 320px and desktop width; capture review evidence and record results.

**Feature estimate:** 2 US (24 hours)

## Feature F2: Contact and Message Delivery

**Release:** R1  
**Status:** Done  
**Goal:** Let personal and professional visitors send a message reliably while minimizing retained personal data and blocking obvious abuse.

**Feature is complete when:** The form contains the agreed fields and reason choices, invalid input is rejected, a verified sender triggers exactly two delivery emails (one to Gustavo and one acknowledgment to the sender), pending content is removed after delivery or expiry, and abuse controls and delivery tests pass. There is no durable in-site inbox.

### User Story F2-US1: Submit a Complete Contact Request

**Status:** Done
**Estimate:** 1 US (12 hours)  
**Goal:** As a visitor, I can send a clearly categorized message with the information needed for a reply.

**Implementation evidence (2026-09-28):** The contact form now includes the required `Reason` field with the approved choices `Work or collaboration` and `Personal note`, and the server-side API enforces the same required fields and valid-email checks. The success state now tells the user that email verification is required before delivery, matching the R1 requirement and avoiding the false claim that the message has already reached Gustavo. The Angular app is compiling successfully with the updated contract.

**Acceptance tests:**

1. Given the contact form is loaded, when its fields are inspected, then it has name, email, reason, and message fields, with reason choices `Work or collaboration` and `Personal note`.
2. Given any required field is missing or the email is malformed, when submit is attempted, then submission is blocked and the invalid field is identified without sending a message.
3. Given all fields are valid, when the visitor submits, then the UI confirms that verification is required and does not claim the message has already been delivered.
4. Given the contact form is viewed on mobile or by keyboard, when a visitor completes it, then labels remain associated with fields, errors are adjacent to the affected fields, and the verification-pending status is announced and visually distinct.

**Tasks:**

- [x] Implement the agreed form fields, required validation, and reason choices in the defined editorial visual system; use a clear stacked mobile form and concise field labels.
- [x] Implement server-side validation equivalent to the client-side checks.
- [x] Test missing fields, malformed email, each reason choice, and successful transition to the verification-pending state; assert response and UI output.

### User Story F2-US2: Verify Email Before Delivery

**Status:** Done (2026-10-06; reopened 2026-10-01 after review)  
**Estimate:** 2 US (24 hours)  
**Goal:** As Gustavo, I receive contact messages only after the sender proves control of the submitted email address.

**Implementation evidence (2026-09-29):** The verification flow is implemented with an expiring, single-use token stored in memory and a `GET /api/contact/verify` endpoint. The form submission returns `verificationRequired: true` and exposes the verification URL instead of claiming delivery. After verification, the backend sends two SMTP emails: the message to Gustavo's configured contact address and a professional acknowledgement to the sender. The workflow was validated successfully with the provider and accepted the confirmed real inbox target. The backend build passes after the final delivery update.

**Reopened after code review (2026-10-01):** Reading `ContactController.Send` and `contact-page.ts` shows two gaps. (1) No verification email is sent to the submitted address. The verification token and URL are returned in the `POST /api/contact` response body, so anyone can verify any address without controlling it, and acceptance test 1 and the goal of this story are not met. (2) The Angular form ignores the returned URL, so in the browser the message is never delivered at all. The earlier "validated with the provider" result was obtained by calling the verify URL directly. Neither gap is covered by an automated test. The token store, expiry, single-use, delivery and cleanup logic are sound and should be kept. The fix is to email the link to the sender, stop returning the token and URL in the response, and make the verification page a deliberate confirm action (like F3's) so mail link scanners cannot trigger delivery.

**Acceptance tests:**

1. Given a valid contact request, when it is submitted, then a verification message is sent to the submitted address and Gustavo's inbox receives no contact message yet.
2. Given a valid, unused verification link, when the sender confirms it, then exactly two contact emails are sent: one to Gustavo's configured address and one professional acknowledgement to the sender.
3. Given an expired, malformed, or previously used verification link, when it is opened, then no contact message is delivered and the sender receives a clear failure result.
4. Given delivery succeeds or the verification request expires, when pending data is inspected, then the message content is no longer retained by the application.

**Tasks:**

- [x] Select and configure a mail delivery provider without committing secrets to source control. (Secrets were moved out of `appsettings.json` into user-secrets and environment variables on 2026-10-01; the credentials that were committed earlier must be rotated.)
- [x] Email the verification link to the submitted address and remove the token and URL from the `POST /api/contact` response.
- [x] Add a verification page in the client that confirms with an explicit action, and show a "check your email" state after submission.
- [x] Implement expiring, single-use verification and message delivery; ensure retry behavior cannot deliver duplicates.
- [x] Define and implement transient pending-data storage and cleanup; do not persist a durable message archive.
- [x] Test valid, expired, replayed, and failed-delivery paths with a test mail sink; assert recipient, delivery count, and cleanup.
- [x] Add an end-to-end test that fails if the verification token or URL is ever returned to the submitter.

**Implementation evidence (2026-10-02):** `POST /api/contact` now emails the link to the submitted address through `IContactEmailDeliveryService.SendVerificationRequestAsync` (link only, no message body) and returns `verificationRequired` without a token or URL. If the verification email cannot be sent, the pending message is discarded and the API returns 502. The link is `/contact/verify#token=...`; the token is in the fragment, and the client page `ContactVerifyPage` confirms only after an explicit button press with `POST /api/contact/verify {token}`. `GET /api/contact/verify` no longer exists, so a link scanner cannot trigger delivery. Automated evidence: `ContactVerificationApiTests` (4 tests: link emailed to the sender and absent from the response body, nothing delivered before confirmation, a GET with the token delivers nothing, delivery happens once on POST and a replay returns 409, unknown token rejected, 502 and nothing sent when the verification email fails); the full .NET suite passes (24 tests) and the Angular suite passes (5 tests, 3 new for the confirmation page: no automatic post, expired-link message, incomplete link). Expired-link behavior is covered by the unchanged token store logic, not by a new test. **Review follow-up (2026-10-02):** a code review and a runtime run (SMTP blanked, so no email was sent) confirmed the 502 path, the absence of the token in responses, the unknown-token 409, the 429 limit, log privacy, and the confirmation page rendering in headless Chrome. The review found three defects, now fixed and tested, and recorded in `architecture.md` section 8.1: (1) a failed delivery after confirmation consumed the link, so the promised retry was rejected; `Restore` puts the message back until the original 30-minute expiry. If the notice to Gustavo is sent and only the acknowledgement fails, the restored message is marked `OwnerNotified` so a retry sends only the receipt (fixed 2026-10-06 after a second review). (2) The form could be used to email a third party repeatedly; a per-address limit (`Contact:RateLimit:MaxRequestsPerRecipient`, default 2 per window, address stored only as a hash) now applies. (3) A malformed address that made the mail library throw skipped cleanup; it is now treated as a failed send and the pending message is discarded. The .NET suite is now 28 tests (4 new). Failed sends still count against the per-IP limit, which is accepted. **Manual end-to-end check (2026-10-06, Docker stack behind Caddy with a trusted local CA, MailerSend SMTP):** Gustavo submitted the form, received the verification email, confirmed with the explicit button, and both the message to Gustavo and the acknowledgement to the sender arrived. The Definition of Done checks pass (28 .NET and 5 Angular tests, production build, no secrets or message content in logs). Replaying the used link and the 30-minute expiry were checked only by automated tests, not by hand. The MailerSend domain is a trial, so delivery to other recipients is unverified.

### User Story F2-US3: Reach Gustavo Reliably and Safely

**Status:** Done  
**Estimate:** 1 US (12 hours)  
**Goal:** As a visitor, I can find a direct email route or use the form; as Gustavo, I am protected from simple automated abuse.

**Implementation evidence (2026-09-29):** The rendered contact page exposes a `mailto:` link to Gustavo's configured email alongside the form. The API rejects honeypot submissions and enforces an atomic per-socket-IP sliding-window limit; caller-supplied `X-Forwarded-For` is ignored unless trusted-proxy processing is configured, and the tracked client partition count is bounded. Operational logs omit sender addresses and message bodies, including SMTP failure details. The focused contact test suite passes 5 tests covering sequential and concurrent thresholds, spoofed forwarding headers, partition bounds, honeypot rejection, and log privacy. Runtime verification against the rebuilt Docker container returned HTTP 200 for the page, HTTP 400 for a filled honeypot, and HTTP 200 for the first five contact submissions followed by HTTP 429 for the sixth even when each request supplied a different `X-Forwarded-For`. Container logs contained no synthetic sender address or message body. Verification URLs were intentionally not followed, so this abuse-control check did not send email. The direct email link was also confirmed in the rendered page.

**Acceptance tests:**

1. Given the public profile is loaded, when the contact options are inspected, then a working email link and contact form are both available.
2. Given repeated submissions exceed the configured abuse-control threshold, when another request is sent, then the request is rejected or delayed and no email is sent to Gustavo.
3. Given application logs are inspected after a form submission, when log entries are reviewed, then message bodies and sender email addresses are absent.

**Tasks:**

- [x] Add a direct `mailto` route using Gustavo's configured public contact address.
- [x] Configure rate limiting and an abuse signal (for example, a honeypot) for contact submission.
- [x] Add structured operational logging that excludes message bodies and sender email addresses.
- [x] Verify the rendered direct email link and form; test sequential/concurrent threshold behavior, spoofed forwarding headers, bounded partitions, honeypot rejection, and captured log privacy.

**Feature estimate:** 4 US (48 hours)

## Feature F3: User Accounts and Authentication

**Release:** R1 (Gustavo administrator account); R3 (reader accounts)  
**Status:** In Progress  
**Architecture design:** [F3 User Accounts and Authentication Architecture](./f3-authentication-architecture.md) (design record; implemented behavior is noted in its status section)  
**Goal:** Establish distinct, secure identities for Gustavo and readers, with access rules appropriate to each role and verified reader email before commenting.

**Feature is complete when:** For R1, Gustavo can sign in and out through a provisioned administrator account, and no public registration path can create or elevate an administrator. For R3, readers can register, verify their email, sign in and out, and maintain a public display name; unverified readers cannot access comment actions. Sessions and account data are handled securely, private email is never exposed publicly, and all F3 acceptance tests pass.

### User Story F3-US1: Authenticate Site Administrators

**Status:** In Progress  
**Estimate:** 2 US (24 hours)  
**Goal:** As a provisioned administrator, I can securely sign in to an administrator account used for private site management.

**Acceptance tests:**

1. Given an administrator account has been provisioned, when that administrator submits valid credentials, then an authenticated administrator session is established and the session is not exposed to client-side script.
2. Given invalid credentials or an unauthenticated request, when a protected administrator operation is attempted, then access is rejected and no privileged action occurs.
3. Given an administrator signs out or the session expires, when the same session is used again, then protected operations are rejected.
4. Given public registration is available to readers, when a registrant submits any role or privilege value, then the request cannot create or grant an administrator account.
5. Given an administrator needs to sign in or is signed in, when they view the account interface, then the sign-in form and sign-out action are clearly available, with validation and session-expiry feedback that does not reveal whether an arbitrary email is registered.

**Tasks:**

- [x] Define a secure operator-authorized provisioning path for administrator accounts; do not ship a default password or public administrator registration.
- [x] Implement administrator sign-in, sign-out, session expiration, and role-based authorization using Identity cookie authentication.
- [x] Implement the private sign-in interface with labeled fields, validation, non-enumerating error messages, and signed-in/signed-out feedback.
- [x] Configure production cookies as HttpOnly, Secure, and SameSite=Lax; require antiforgery validation for cookie-authenticated mutations.
- [x] Test valid/invalid sign-in, sign-out, expired sessions, protected operations, privilege escalation attempts, CSRF rejection, and cookie flags through API integration tests.

**Implementation evidence (2026-09-30):** Added operator-only interactive administrator provisioning (`dotnet run --project src/GooseWebsite.Api -- admin provision`), allowing multiple distinct Admin accounts while rejecting duplicate account creation and never promoting an existing user. Removed the public registration endpoint and demo identity/content seed. All account types use the single `/sign-in` route and API; login and `/me` return a server-derived `isAdmin` flag, while protected APIs independently enforce roles. Login is non-persistent, uses Identity lockout, generic errors, and an HttpOnly same-origin cookie; logout/login mutations require antiforgery tokens. Unknown-user and locked-account paths perform dummy password-hash verification to reduce login timing differences. Added the shared sign-in screen and applied `AdminOnly` plus antiforgery validation to blog mutation endpoints. Integration coverage verifies valid admin and standard-account login, backend-derived access levels, generic unknown/wrong-password/locked responses, session and antiforgery cookie flags, missing-CSRF rejection, protected writes, logout, expiry, and disabled public registration. The header component test verifies the visible shared sign-in link and `/sign-in` target. The full .NET and Angular test suites and the Angular production build pass. HTTPS Docker runtime checks are documented in the architecture; this story remains In Progress pending the remaining account and deployment acceptance work.

### User Story F3-US2: Register and Verify Reader Accounts

**Status:** Done  
**Estimate:** 2 US (24 hours)  
**Goal:** As a reader, I can create an account, verify my email, and use a public display name before participating in comments.

**Acceptance tests:**

1. Given a reader submits a valid email, credential, and display name, when registration succeeds, then a verification message is sent and the account remains unverified.
2. Given a valid unused verification link, when the reader confirms it, then the account becomes verified and can sign in; expired, invalid, or reused links do not verify it.
3. Given an unverified reader attempts to sign in for commenting or perform a comment action, when the request is processed, then it is rejected and no comment is stored.
4. Given a verified reader signs in and later signs out, when account state is checked, then the session is established and subsequently invalidated as expected.
5. Given public account data or comments are returned, when response fields are inspected, then the selected display name may be public but email and credentials are absent.
6. Given a reader opens registration, verification, or sign-in on a mobile viewport or by keyboard, when they complete the flow, then each step has labeled controls, understandable status/error feedback, and no horizontal overflow.

**Tasks:**

- [x] Implement reader registration, email verification, sign-in/sign-out, and display-name storage; prevent reader-selected roles or identifiers from granting administrator privileges.
- [x] Implement registration, verification-result, sign-in, and sign-out screens using the public site's visual system and accessible form patterns.
- [x] Add the verified-account requirement to authorization policies consumed by comment endpoints.
- [x] Test registration, valid/invalid/expired/replayed verification, verified and unverified sign-in/access, sign-out, and public response privacy.
- [x] Record the selected credential policy, verification-link lifetime, anti-abuse controls, and account recovery approach before marking this story Ready.

**Implementation evidence (2026-10-01):** Reader registration creates only an unconfirmed `Reader`, sends a 24-hour Identity confirmation link through the separate account-email sender, and always returns the same generic response for new and duplicate addresses. The token and user ID are placed in the URL fragment so they are not sent in the initial HTTP request; the verification page requires an explicit confirmation action and submits the token through a CSRF-protected POST. A resend endpoint uses the same generic response for known and unknown addresses. Identity refuses sign-in until email confirmation. The `VerifiedReader` policy requires the `Reader` role and checks current persisted `EmailConfirmed` state, so a cookie/principal alone is insufficient. Registration, resend, and verification share a fixed-window limit of five requests per client IP per ten minutes. The credential policy recorded at the time was the existing Identity minimum of eight characters (raised to twelve on 2026-10-02, see the decision log) with no composition requirement and lockout after five failed attempts for fifteen minutes. Reader password recovery is selected as a generic-response email reset flow, but its endpoints and UI remain a separate R3 follow-up; exact recovery email copy and production mail-provider delivery still require deployment verification.

Automated runtime coverage in `tests/GooseWebsite.Api.Tests/AuthApiIntegrationTests.cs` exercises registration through a test mail sink, generic duplicate/resend responses, no account-existence disclosure, no sign-in before confirmation, invalid/expired/replayed token rejection, GET-link non-mutation, confirmed sign-in/sign-out, live `VerifiedReader` authorization, delivery-failure resend recovery, the per-IP registration limit, and rejection when registration is disabled. The `Auth:ReaderRegistration:Enabled` setting defaults to false; Development opts in, and Production requires an explicit true setting when R3 launches. Angular headless tests verify that opening the confirmation link does not post automatically and that explicit confirmation triggers the CSRF flow. Browser review confirmed registration/sign-in and verification fit at 320px without horizontal overflow, keyboard focus reaches the confirmation action, and auth pages omit the public navigation shell. A local HTTP smoke check returned 200 for `/verify-email` and confirmed `Referrer-Policy: no-referrer`. SMTP was replaced by a test sender for automated tests; production delivery credentials were not exercised by these checks.

### User Story F3-US3: Recover Administrator Access

**Status:** In Progress (approach approved 2026-10-02: operator-only reset command; implemented 2026-10-09, awaiting review)  
**Release:** R1  
**Estimate:** 1 US (12 hours), proposed 2026-10-01  
**Goal:** As a site administrator who has lost a password, I can regain access through an operator-authorized path without any public recovery endpoint.

**Acceptance tests:**

1. Given an existing administrator account, when the operator runs the interactive reset command and enters a new password, then only that account's password changes and its existing sessions are invalidated.
2. Given an email that has no account or belongs to a Reader, when the command runs, then nothing is created, promoted, or changed, and the operator sees a clear failure.
3. Given the command is inspected, when arguments, logs, and output are reviewed, then no password is accepted as an argument or printed.

**Tasks:**

- [x] Add an operator-only module command (same `IModuleCommand` mechanism as `admin provision`) that resets one existing administrator's password.
- [x] Update the security stamp on reset so existing cookies stop working. The cookie's security stamp is now checked on every request (it was every 30 minutes), so a reset ends open sessions at once.
- [x] Test the success, unknown-account, and Reader-account paths, and record the approach in the F3 design note.

**Implementation evidence (2026-10-09):** `AdminPasswordResetTests` (5 tests) pass with the full suite (49 tests): reset changes only that administrator's password and ends its open session while another administrator is unaffected (test 1); an unknown email and a Reader account are refused and nothing changes (test 2); a password that breaks the 12-character policy is rejected and the old one keeps working; a locked-out administrator can sign in after a reset; the command rejects any extra argument. Acceptance test 3 is met by construction: the command reads the password interactively without echo, takes no password argument, and prints only a fixed success or failure message. The reset also clears any lockout so a locked-out administrator can sign in with the new password.

### User Story F3-US4: Recover Reader Passwords

**Status:** Proposed  
**Release:** R3  
**Estimate:** 1 US (12 hours), proposed 2026-10-01  
**Goal:** As a reader, I can reset a forgotten password through an emailed link without revealing whether an address has an account.

**Acceptance tests:**

1. Given any email address, when a reset is requested, then the response is identical for known, unknown, and unconfirmed addresses.
2. Given a valid unused reset link, when the reader submits a new password, then it changes and existing sessions are invalidated; expired, malformed, or reused links change nothing.
3. Given reset endpoints, when abuse is simulated, then the existing per-IP limit applies.

**Tasks:**

- [ ] Implement `POST /api/auth/password/forgot` and `POST /api/auth/password/reset` with generic responses and CSRF protection.
- [ ] Add the reset screens and email copy; test generic responses, token expiry and replay, and session invalidation.

**Feature estimate:** 6 US (72 hours): F3-US1 (2), F3-US2 (2), F3-US3 (1), F3-US4 (1)

## Feature F4: Gustavo's Private Content Editor

**Release:** R1  
**Status:** Proposed  
**Goal:** Let Gustavo manage project and story content without editing source code, while ensuring drafts remain private until he explicitly publishes them.  
**Starting point in the code:** The `Blog` module already provides published-post reads and administrator-only create, update, and delete with draft/publish state, tags, and slugs, and the client has an in-page blog section that falls back to hard-coded sample posts. These are the seed for stories. F4 and F5 should evolve this module (adding projects, images, and the editor UI) rather than start a parallel one. The sample posts must be removed before R1 because R1 requires no sample content.

**Feature is complete when:** Unauthenticated visitors and non-administrator readers cannot access editor operations; only authenticated accounts explicitly provisioned with the administrator role can create, update, preview, publish, and delete content; the agreed fields persist; uploaded images can be associated with content; and all F4 tests pass. R1 does not require public project/story browsing pages or published sample content. Content state and API behavior must be ready for the later public browsing release. The editor follows the compact, task-focused visual direction above and is not exposed in public navigation.

### User Story F4-US1: Navigate and Manage Content in the Editor

**Status:** Proposed  
**Estimate:** 3 US (36 hours)  
**Goal:** As Gustavo, I can use a private editor workspace to find and manage my projects and stories without editing source files.

**Acceptance tests:**

1. Given Gustavo opens the authenticated editor, when the workspace loads, then he can switch between project and story management and can see each item's title, status, and last-updated date.
2. Given the selected content type has draft and published items, when Gustavo filters by status or searches by title, then only matching items are shown and the empty state is clear when nothing matches.
3. Given Gustavo selects Create, when the action completes, then the correct project or story form opens for a new item; given he selects an existing item, then its corresponding edit form opens with its saved values.
4. Given Gustavo chooses to delete an item, when the confirmation is dismissed, then the item remains unchanged; when deletion is confirmed, then the item is removed from the editor list and cannot be retrieved as a published item.
5. Given the editor is used at 320 CSS-pixel width or by keyboard only, when Gustavo navigates the workspace, then content actions remain reachable, controls do not overlap, and focus order and visible focus are clear.

**Tasks:**

- [ ] Implement an authenticated editor entry point and navigation between project and story lists; keep it out of public navigation.
- [ ] Implement content lists showing title, draft/published status, and last-updated date, with status filtering, title search, and empty states.
- [ ] Wire create and select-item actions to the corresponding project/story forms; implement deletion with an explicit confirmation step.
- [ ] Apply the compact, task-focused visual direction with responsive layout, keyboard operation, visible focus, and non-color-only state labels.
- [ ] Test list contents, search/filter results, create/edit routing, cancel/confirm deletion, anonymous access, keyboard flow, and narrow viewport behavior; assert UI and persisted state.

### User Story F4-US2: Secure the Administrator Editor

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As a provisioned administrator, I can access content management; visitors and reader accounts cannot use administrator-only actions.

**Acceptance tests:**

1. Given no authenticated administrator session, when an editor page or write endpoint is requested, then the application returns an unauthenticated response and no content is changed.
2. Given any explicitly provisioned administrator's authenticated session, when the editor is opened, then the administrator can reach project and story management.
3. Given an authenticated reader account, when an editor page or write endpoint is requested, then access is denied and no content is changed.
4. Given a signed-out or expired session, when a write is attempted, then it is rejected and the stored content remains unchanged.

**Tasks:**

- [ ] Define the authorized administrator provisioning path and authenticated administrator policy for editor routes and APIs.
- [ ] Gate editor UI and every write endpoint on the same administrator policy.
- [ ] Keep editor controls compact and task-focused; distinguish draft, preview, save, and publish states with labels and status text, not color alone.
- [ ] Test anonymous, provisioned-administrator, reader, and expired-session paths; assert HTTP status and database state.

### User Story F4-US3: Manage Stories as Drafts

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As Gustavo, I can create and edit a story with the agreed fields, preview it, and publish it explicitly.

**Acceptance tests:**

1. Given Gustavo saves a story with title, body, publication date, optional cover image, and tags, when the story is reopened, then all supplied fields match the saved values.
2. Given a story is a draft, when Gustavo opens its preview, then he sees the rendered content and a clear draft state; an unauthenticated public request cannot retrieve it.
3. Given a draft passes required validation, when Gustavo selects Publish, then its state changes to published exactly once; saving edits without selecting Publish does not publish it.
4. Given a required story field is missing or invalid, when save is attempted, then validation identifies the error and the invalid story is not published.

**Tasks:**

- [ ] Implement story data fields and server-side validation for required content and publication state.
- [ ] Implement Gustavo's editor forms for create/edit, draft save, preview, and explicit publish.
- [ ] Test field persistence, invalid input, preview rendering, draft privacy, and publish state transitions through API and UI checks.

### User Story F4-US4: Manage Projects as Drafts

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As Gustavo, I can create and edit a project with its overview, my contribution, tools, images, and demo/source links.

**Acceptance tests:**

1. Given Gustavo saves a project with title, overview, role/contribution, technologies, images, and demo/source links, when it is reopened, then all supplied fields match the saved values.
2. Given a project is a draft, when Gustavo opens its preview, then he sees its rendered content and draft state; an unauthenticated public request cannot retrieve it.
3. Given a draft passes required validation, when Gustavo selects Publish, then its state changes to published; ordinary save does not publish it.
4. Given a required project field is invalid, when save is attempted, then validation is shown and the invalid project is not published.

**Tasks:**

- [ ] Implement project data fields and server-side validation, including URL validation for optional external links.
- [ ] Implement Gustavo's editor forms for create/edit, draft save, preview, and explicit publish.
- [ ] Test project field persistence, invalid URLs, preview rendering, draft privacy, and publish transitions through API and UI checks.

### User Story F4-US5: Upload Content Images

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As Gustavo, I can upload and associate images with a story or project from my device.

**Acceptance tests:**

1. Given Gustavo uploads an allowed image file, when upload completes, then the response contains a usable image reference and the image can be associated with its content item.
2. Given a non-image or disallowed file is uploaded, when the request is processed, then it is rejected and no file is made publicly addressable.
3. Given an unauthenticated visitor attempts an upload, when the request is processed, then it is rejected and no file is stored.
4. Given a saved item references an uploaded image, when its preview is rendered, then the image loads and has suitable alternative text support in the editor.

**Tasks:**

- [x] Choose allowed formats and maximum size: JPEG, PNG, and WebP, 5 MB, generated file names (decided 2026-10-02).
- [ ] Choose the storage location and the deletion behavior for unreferenced images (see "Decisions to Record").
- [ ] Implement administrator-only upload handling, content-type/size validation, and safe image references.
- [ ] Test allowed, oversized, disallowed, and unauthenticated uploads; assert response, storage state, and preview rendering.

**Feature estimate:** 11 US (132 hours)

## Feature F5: Public Project and Story Browsing

**Release:** R2  
**Status:** Proposed  
**Goal:** Let visitors explore Gustavo's published work and selected public stories, while keeping drafts and private data inaccessible.

**Feature is complete when:** Project and story indexes show published content, each item has a usable detail view, direct links work, drafts remain private, and the agreed profile/contact paths remain available. All F5 acceptance tests pass on mobile and desktop.

### User Story F5-US1: Explore Published Projects

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As a visitor, I can browse Gustavo's projects and inspect his contribution and supporting links.

**Acceptance tests:**

1. Given at least one project is published, when a visitor opens the work index, then every published project appears and no draft appears.
2. Given a visitor opens a project, when its detail view renders, then title, overview, Gustavo's role/contribution, technologies, images, and available demo/source links are shown in that order of importance, with links clearly distinguishable from metadata.
3. Given a visitor opens a valid project URL directly, when the page loads, then the matching project is displayed; an unknown project returns the site's not-found state.
4. Given the project index is inspected at desktop and mobile sizes, when projects are scanned, then each repeated entry is compact and unframed, while the project detail provides a readable editorial column without horizontal scrolling.

**Tasks:**

- [ ] Implement a public query/API that returns published projects only.
- [ ] Implement project index and detail views using the agreed project fields.
- [ ] Seed test data with both draft and published projects; test index filtering, details, direct navigation, and missing IDs.

### User Story F5-US2: Read Published Personal Stories

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As a visitor, I can read Gustavo's selected personal stories and understand when they were published.

**Acceptance tests:**

1. Given at least one story is published, when a visitor opens the story index, then every published story appears with its title, date, and tags, and no draft appears.
2. Given a visitor opens a story, when its detail view renders, then title, body, publication date, tags, and optional cover image are displayed in a quiet reading layout with metadata secondary to the story.
3. Given a visitor opens a valid story URL directly, when the page loads, then the matching story is displayed; an unknown story returns the site's not-found state.
4. Given a story contains sensitive personal details, when Gustavo reviews it before publication, then publishing remains an explicit action by Gustavo and no draft is exposed before that action.
5. Given a story is displayed, when its tone and layout are reviewed, then the design allows Gustavo's curious, thoughtful writing to lead and does not require technical jargon, game imagery, or a hobby list.

**Tasks:**

- [ ] Implement a public query/API that returns published stories only.
- [ ] Implement story index and detail views with date, tags, and optional cover image.
- [ ] Seed test data with draft and published stories; test filtering, rendering, direct navigation, and not-found behavior.
- [ ] Add a pre-publish privacy/content review reminder for Gustavo without automatically exposing drafts.

**Feature estimate:** 4 US (48 hours)

## Feature F6: Verified Reader Comments

**Release:** R3  
**Status:** Proposed  
**Goal:** Let readers participate in discussion on Gustavo's projects and stories with verified accounts, public display names, private email addresses, replies, and control over their own comments.

**Feature is complete when:** Only verified registered readers can comment; accepted comments appear without Gustavo's pre-approval; comments can attach to projects and stories; only their author can edit or delete them; replies work; email addresses are never public; and all F6 authorization and privacy tests pass. Comment controls remain visually secondary to the project or story itself.

### User Story F6-US1: Comment on a Project or Story

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As a verified reader, I can add a comment to a project or story and see it appear immediately under my display name.

**Acceptance tests:**

1. Given a verified reader submits a valid comment on a published project or story, when submission succeeds, then the comment appears on that item without Gustavo's approval.
2. Given a comment is displayed publicly, when its fields are inspected, then the display name and comment are visible and the reader's email is not.
3. Given an anonymous or unverified reader submits a comment, when the request is processed, then it is rejected and no comment appears.
4. Given a comment targets a missing or unpublished content item, when submission is processed, then it is rejected and no comment is stored.

**Tasks:**

- [ ] Implement comment creation for published project and story identifiers.
- [ ] Render comments with display name and safe text handling; never serialize reader email publicly.
- [ ] Link the commenter's display name to their member profile (F8), subject to the profile viewer rule.
- [ ] Test immediate visibility, both content types, anonymous/unverified access, and missing/unpublished targets.

### User Story F6-US2: Reply to a Comment

**Status:** Proposed  
**Estimate:** 1 US (12 hours)  
**Goal:** As a verified reader, I can reply to an existing comment in its project or story discussion.

**Acceptance tests:**

1. Given a verified reader replies to a comment, when the reply is saved, then it appears under the correct parent comment and on the same content item.
2. Given a reply refers to a missing parent or a parent on another content item, when it is submitted, then it is rejected and no reply is stored.
3. Given a reply is displayed, when its fields are inspected, then no email address is included.

**Tasks:**

- [ ] Define and implement the parent-child relationship and validation for replies.
- [ ] Render replies with an understandable relationship to their parent.
- [ ] Test correct nesting, invalid parent references, and public response privacy.

### User Story F6-US3: Manage My Own Comments

**Status:** Proposed  
**Estimate:** 2 US (24 hours)  
**Goal:** As a reader, I can edit or delete my own comments but cannot change another reader's comment.

**Acceptance tests:**

1. Given the author edits their comment, when the update succeeds, then the new text is displayed and the original author remains unchanged.
2. Given a different authenticated reader attempts to edit or delete that comment, when the request is processed, then it is rejected and the comment is unchanged.
3. Given the author deletes their comment, when the discussion is reloaded, then the comment is absent or clearly marked deleted according to the chosen deletion policy.
4. Given an unauthenticated reader attempts to edit or delete a comment, when the request is processed, then it is rejected.

**Tasks:**

- [x] Deletion policy decided 2026-10-02: replace the comment with a "[deleted]" placeholder (text and author removed, replies stay attached).
- [ ] Implement author-scoped edit/delete authorization on the server.
- [ ] Test author, other-reader, and anonymous operations; assert response and persisted/public state.

**Feature estimate:** 5 US (60 hours)

## Feature F7: Centralized Data Access Safeguards

**Release:** R1  
**Status:** Proposed  
**Goal:** Give the whole application one place where every database read and write passes, so protection such as input sanitization and access checks is written once and applies to every feature as the product grows.

**Feature is complete when:** `DatabaseService` is the only route to the database for all modules (this is already true in the code as of 2026-10-02), it applies the agreed safeguards to every operation, and the F7 acceptance tests pass. Modules keep their own query logic but must not bypass the gateway.

**Why this is planned before F4:** The editor adds many administrator writes and, later, public reads of user-supplied content. Adding the safeguards before those arrive avoids repeating checks in each controller.

### User Story F7-US1: Apply Safeguards in the Database Gateway

**Status:** Proposed  
**Release:** R1  
**Estimate:** 2 US (24 hours), proposed 2026-10-02 and confirmed by Gustavo; scheduled for Sprint 2  
**Goal:** As Gustavo, I want every database operation to be checked in one place, so a feature cannot store unsanitized input or read or change data the caller is not allowed to.

**Current state (2026-10-02):** `Shared/Persistence/DatabaseService.cs` is the single gateway and all modules use it. It performs **no** sanitization and **no** access checks yet, so each controller still validates for itself.

**Decided 2026-10-02:** user-written text is normalized on save (trimmed, control characters stripped), input over the field limits is rejected with a clear error, and text is always rendered as plain text.

**Decisions to record before this story is Ready:** whether access rules are declared per entity (for example an attribute or a per-entity policy) or per operation; and how a rejected operation is reported to callers.

**Acceptance tests:**

1. Given a write that carries input outside the agreed rules (for example over-long, containing control characters, or untrimmed text), when it is saved through the gateway, then it is normalized or rejected exactly as the recorded rules say, and the stored value is never the raw input.
2. Given an operation on an entity that is restricted (for example administrator-only writes), when the caller lacks the required access, then the gateway refuses it, nothing is read or changed, and a clear, non-revealing failure is returned.
3. Given an authorized caller and valid input, when the same operation is performed, then it succeeds and existing behavior (for example the blog endpoints and account flows) is unchanged.
4. Given the code base is searched, when modules are inspected, then no module class other than Identity registration uses `ApplicationDbContext`, `UserManager`, or `RoleManager` directly. An automated architecture test enforces this.
5. Given a safeguard rejects an operation, when logs are inspected, then they record the category and entity type but never the rejected values.

**Tasks:**

- [ ] First, decide what the persistence interface promises: named operations per intent, no composable queries returned to callers, and the Blog repository seam removed unless a second implementation appears. Spec: [#3](https://github.com/GucciVanin/goosevaninWebsite/issues/3) (architecture review 2026-10-06). Revisit the 2 US estimate once decided.
- [ ] Record the sanitization and access-rule decisions above in this backlog.
- [ ] Implement a single filter step inside `DatabaseService` that every read and write passes through, with sanitization and access checks as separate, individually testable parts.
- [ ] Provide the caller context (the current user and role) to the gateway without modules passing it by hand.
- [ ] Add the architecture test that fails when a module bypasses the gateway.
- [ ] Test rejected, normalized, and allowed operations for the blog and account paths; assert stored data and logs.
- [ ] Update `architecture.md` section 3.2 and the conventions.

**Feature estimate:** 2 US (24 hours)

## Feature F8: Member Profiles

**Release:** R3  
**Status:** Proposed  
**Goal:** Let signed-in users have a profile that other users can view, so members can get to know each other through basic, voluntarily shared details such as hobbies, profession, and location.

**Feature is complete when:** A verified user can create a profile in which every detail beyond the display name is optional and can be skipped; other signed-in users can view that profile; users can later change or remove any detail; private data (email, credentials, and anything the user left blank) is never exposed; and all F8 acceptance tests pass. F8 uses the accounts built in F3-US2 and is distinct from Gustavo's public profile in F1, which is site content, not an account profile.

**Product rules for this feature (confirmed by Gustavo 2026-10-02):**

- Every profile detail other than the display name is optional. Nothing is required to finish creating an account or profile, and each optional step offers a clear **Skip** action.
- Profile fields in scope: hobbies/interests (200 characters), profession (100), location (100), and a short "about me" (500), all plain text. Additional fields are added only by a recorded decision.
- Only signed-in verified users may view a member profile. Administrators have no member profile (their public identity is F1). A commenter's display name links to their profile (F6).
- Location is a broad, free-text place (for example a region or country) and the interface discourages exact addresses. The same privacy stance applies as for Gustavo's own profile: do not ask for age or other sensitive personal details.
- Email address, credentials, and role are never part of any profile response.
- Content is user-written, so it is stored and displayed as safe text and passes through the F7 database safeguards once they exist.

### User Story F8-US1: Create My Profile with Optional Details

**Status:** Proposed  
**Release:** R3  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As a verified user, I can create my profile and choose which details to share, skipping any I prefer not to give.

**Acceptance tests:**

1. Given a user has just verified their email, when they reach profile creation, then only the display name is required and every other field is clearly marked optional.
2. Given the user selects **Skip** on the profile step (or on any single optional field), when they continue, then the account is fully usable and the skipped details are stored as empty, not as placeholder text.
3. Given the user fills some fields and leaves others blank, when the profile is saved and reopened, then the filled values match and the blank ones remain empty.
4. Given input that is over-long or contains markup, when it is saved, then it is validated against the field limits and stored and displayed as plain text only.
5. Given the profile form is used at 320 CSS-pixel width or by keyboard only, when the user completes or skips it, then labels are associated with fields, **Skip** is reachable, errors are adjacent to the affected field, and there is no horizontal scrolling.

**Tasks:**

- [x] Profile fields, length limits, and visibility recorded (see "Decided on 2026-10-02").
- [ ] Add the profile data in a new `Profiles` module with its own table (one row per user; proposed default, record if changed) with server-side validation of length and content.
- [ ] Implement the create/skip screens in the client using the existing form, validation, and accessibility patterns.
- [ ] Test skipping the whole step, skipping individual fields, partial and full profiles, over-long and markup input, and the 320 px and keyboard flows.

### User Story F8-US2: View Another User's Profile

**Status:** Proposed  
**Release:** R3  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As a signed-in user, I can open another user's profile and learn the basic things they chose to share.

**Acceptance tests:**

1. Given a signed-in user opens another user's profile, when it loads, then the display name and every detail that user filled in are shown, and details left blank are omitted rather than shown as empty labels.
2. Given a profile response is inspected, when its fields are reviewed, then email, credentials, role, and account identifiers that reveal private data are absent.
3. Given a user who skipped every optional detail, when their profile is viewed, then a respectful minimal page is shown (display name and an indication that they have not shared more) rather than an error.
4. Given an unknown or unverified user, when their profile URL is requested, then the site's not-found state is returned without revealing whether the account exists.
5. Given a visitor who is not signed in, when they request a profile, then access follows the visibility decision recorded below (proposed default: signed-in verified users only).
6. Given a profile is viewed at desktop and mobile widths, when its layout is reviewed, then it follows the calm reading layout of the visual direction and is usable without horizontal scrolling.

**Tasks:**

- [x] Viewer rule decided: signed-in verified users only.
- [ ] Record the profile URL scheme before implementation.
- [ ] Implement a profile read endpoint that returns a dedicated public profile contract (never the account entity) and authorizes with the `VerifiedReader` policy unless the decision says otherwise.
- [ ] Implement the profile page with an empty-profile state and a not-found state.
- [ ] Test shown versus omitted fields, response privacy, empty profile, unknown and unverified targets, and anonymous access.

### User Story F8-US3: Edit or Remove My Profile Details

**Status:** Proposed  
**Release:** R3  
**Estimate:** 1 US (12 hours), proposed 2026-10-02  
**Goal:** As a user, I can change or clear any profile detail at any time and control what others see.

**Acceptance tests:**

1. Given a signed-in user edits their profile, when the change is saved, then others see the new value and the account's owner and identity are unchanged.
2. Given a user clears a field, when others view the profile, then that detail is no longer shown.
3. Given a different user attempts to change this profile, or an anonymous request is made, when the request is processed, then it is rejected and nothing changes. The target account is always derived from the signed-in session, never from a submitted identifier.
4. Given a request includes role, email, or account-id fields, when it is processed, then those fields are ignored or rejected and never change.

**Tasks:**

- [ ] Implement an owner-scoped update endpoint with antiforgery validation and server-side validation; derive the account from the authenticated principal.
- [ ] Add the edit screen with the same optional-field and **Skip** patterns as F8-US1.
- [ ] Test owner edits, field clearing, other-user and anonymous attempts, and rejected privileged fields.

**Feature estimate:** 5 US (60 hours)

## Feature F9: Brand and Visual Redesign

**Release:** R1 (after the R1 content features, before public launch)  
**Status:** Proposed  
**Goal:** Give the whole site Gustavo's own identity: his logo and a visual design he chose, applied consistently to every page, email, and screen, replacing the earlier working design.

**Confirmed scope (Gustavo, 2026-10-02):**

- A **full visual re-skin of every page**: public site, sign-in and verification screens, and the private editor.
- Follow the **inspiration image closely** for layout and look. The image, and Gustavo's **logo as an SVG**, are supplied by Gustavo; neither is in the repository yet.
- A **single look** as shown in the inspiration (no light/dark switch).
- Included beyond styling: a **new typography and design-token system**, a **rewrite of the home page layout and section order**, **page transitions and animation**, and the **editor screens** in the new design.
- The logo appears in the **header, the footer, and the email templates**. A favicon or social preview image is not in scope unless added later.
- Timing: after the R1 content features (F2, F3, F4, F7), before public launch. The editor is first built in the current look and re-skinned by F9-US5.
- **Approval after each story.** Gustavo reviews and approves tokens, then shell, then home, then the other pages before the next story starts.
- Design is done first and **accessibility is verified afterward** in F9-US7. The accessibility requirements are not relaxed: WCAG AA contrast, keyboard operation with visible focus, 320 CSS-pixel layout, and reduced-motion support remain release gates, and a failing check blocks R1.

**Feature is complete when:** every public, account, and editor screen and both email templates use the new design and logo; the design tokens are documented and used instead of hard-coded values; Gustavo has approved each story; the F9-US7 checks pass on the supported browsers (latest two versions of Chrome, Edge, Firefox, and Safari); and the earlier F1 and F2-US1 acceptance tests are re-run against the new design and pass.

**Design analysis (received 2026-10-02, decisions by Gustavo):**

- **Look:** a single dark neon-cyberpunk look. Near-black navy background; neon magenta/violet and cyan/blue as accents; green (Security) and orange (Systems & Tools) appear only as per-card accents. Wide-tracked uppercase monospace-style labels; a large geometric sans headline with a cyan-to-white gradient on the second word. Corner-bracket buttons and thin glowing rules. Exact token values are taken from the image in F9-US1.
- **Home regions (top to bottom):** header (logo, Home / Projects / Skills / About, GitHub, LinkedIn, and email icons); hero with eyebrow "Build · Break · Improve", headline "Software Engineer", a three-line intro, and an "Explore my work" button over a full-bleed city illustration; a row of four category cards (Applications, Cloud & DevOps, Security, Systems & Tools), each with an icon, title, arrow, and image strip; footer strip with a closing line and a "Let's connect" button.
- **Logo:** the supplied `Logo.svg` reads "GCV" and is a raster PNG (1254x1254, black background, about 1.4 MB) wrapped in an SVG, not a vector. The mockup header shows a different script "GV" mark. **Decision:** use the GCV logo as supplied in the header, footer, and emails. Because it is raster, add an optimized, resized copy for header and footer sizes, blend its black background into the page, and give it descriptive alt text. A true vector remains an optional later upgrade.
- **Hero imagery:** **Decision:** the illustration is the hero background and Gustavo's portrait appears in the About section. F9-US3 acceptance test 2 is amended: the first viewport shows his name, positioning line, introduction, and contact action; the portrait is shown in About, not in the first viewport.
- **Copy kept:** the eyebrow "Build · Break · Improve", the three-line intro ("Curious mind. Practical solutions. Real impact."), the four category cards, and the decorative Japanese text. Gustavo confirmed all four cards (Applications, Cloud & DevOps, Security, Systems & Tools) as reflecting his real work. **Dropped:** the footer line "Solve complex problems"; the footer shows the logo and the "Let's connect" button.
- **Navigation:** **Decision:** match the mockup (Home, Projects, Skills, About). Skills and About are sections of the home page; Projects links to projects in the Blog module. No new Skills page is added to the F9 scope.

**Still open before F9-US1 is Ready:** Gustavo supplies the hero illustration as a file (the mockup is a flattened image) and his GitHub and LinkedIn URLs, both promised before Sprint 4. Until then, build with placeholders. The text under each card (what it says about his work) is written with him in the F9-US3 copy review. The existing "Visual and Interaction Direction" section is revised or replaced with this analysis, as soon as the tokens are extracted, so the backlog has a single design brief.

### User Story F9-US1: Establish Brand and Design Tokens

**Status:** Proposed  
**Release:** R1  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As Gustavo, I get a documented brand foundation (logo, colors, typography, spacing, and base components) taken from my inspiration image, so every screen is built from the same pieces.

**Acceptance tests:**

1. Given the supplied logo SVG, when it is added to the client, then it renders crisply at header and footer sizes, has descriptive alternative text, and keeps clear space and a minimum size.
2. Given the inspiration image, when the tokens are defined, then colors, font families and sizes, spacing, radii, and motion timings are named tokens in one place and the values are traceable to the image.
3. Given the token documentation is read, when a developer builds a new screen, then every color, font, and spacing value comes from a token rather than a literal.
4. Given the tokens are applied to a sample page, when Gustavo reviews it, then he approves or requests changes before the next story starts.

**Tasks:**

- [ ] Receive the logo SVG and the inspiration image; record the design analysis (regions, palette, typography, imagery, animation) in this feature.
- [ ] Define the token system (client styles) and document it in `architecture.md`.
- [ ] Add the logo asset with accessible markup; build base components (buttons, links, form fields, headings).
- [ ] Review with Gustavo and record approval.

### User Story F9-US2: Redesign the Site Shell

**Status:** Proposed  
**Release:** R1  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As a visitor, I see the new header, navigation, footer, and page transitions on every page.

**Acceptance tests:**

1. Given any page, when it loads, then the new header (with the logo) and footer (with the logo) match the approved design at desktop and mobile widths.
2. Given a visitor navigates between sections or routes, when the transition runs, then it matches the approved motion design, and with reduced-motion enabled the transition is removed or minimal.
3. Given the shell is used by keyboard, when the visitor tabs through it, then focus order is logical and focus is visible.
4. Given Gustavo reviews the shell, when he approves it, then the approval is recorded before F9-US3 starts.

**Tasks:**

- [ ] Rebuild the header and footer components and the navigation behavior to the design.
- [ ] Implement shell-level transitions and animation using the tokens, with a reduced-motion path.
- [ ] Update the header tests and record Gustavo's approval.

### User Story F9-US3: Redesign the Home Page

**Status:** Proposed  
**Release:** R1  
**Estimate:** 3 US (36 hours), proposed 2026-10-02  
**Goal:** As a visitor, I see a home page whose layout and section order follow Gustavo's inspiration image closely and still make who he is and how to contact him clear.

**Acceptance tests:**

1. Given the approved design, when the home page renders at desktop and mobile widths, then its sections, order, and visual treatment match the inspiration image closely, as reviewed side by side with Gustavo.
2. Given the home page is read, when the first viewport is shown, then Gustavo's name, his positioning line, a concise introduction, and a contact action are visible (his portrait is shown in the About section, per the 2026-10-02 design decision), and no sensitive personal detail is exposed (the F1 content rules still apply).
3. Given animations are present, when reduced-motion is requested, then essential content is fully available without them.
4. Given Gustavo approves the page, when approval is recorded, then F9-US4 may start.

**Tasks:**

- [ ] Agree the new section list and order with Gustavo; update the home components and the shared profile content only where the design requires.
- [ ] Implement the layout, imagery, and animation from the tokens.
- [ ] Review side by side with the inspiration image and record approval.

### User Story F9-US4: Redesign Contact, Blog, and Account Pages

**Status:** Proposed  
**Release:** R1  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As a visitor or account holder, I see the same design on the contact form, the blog or story section, and the sign-in and email-verification screens.

**Acceptance tests:**

1. Given the contact form, when it renders, then fields, validation, and the verification-pending state use the new design and keep their current behavior and accessible labeling.
2. Given the blog or story section, when it renders, then lists and reading views use the new design.
3. Given the sign-in and email-verification screens, when they render, then they use the new design and keep the existing CSRF and explicit-confirmation behavior.
4. Given Gustavo reviews these pages, when he approves them, then approval is recorded before F9-US5.

**Tasks:**

- [ ] Re-skin the contact, blog, sign-in, and verify-email components using the tokens.
- [ ] Confirm existing component tests still pass and update them for markup changes.
- [ ] Record Gustavo's approval.

### User Story F9-US5: Apply the Design to the Editor

**Status:** Proposed  
**Release:** R1  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As Gustavo, I use a private editor that matches the new design while staying compact and task-focused.

**Acceptance tests:**

1. Given the editor workspace, lists, and project and story forms built in F4, when they render, then they use the new tokens and components with clear draft, preview, save, and publish states that do not rely on color alone.
2. Given the editor at 320 CSS-pixel width or by keyboard, when it is used, then actions stay reachable and focus is visible.
3. Given Gustavo reviews the editor, when he approves it, then approval is recorded.

**Tasks:**

- [ ] Re-skin the F4 editor screens with the tokens and base components.
- [ ] Update editor tests for markup changes and record Gustavo's approval.

### User Story F9-US6: Brand the Emails

**Status:** Proposed  
**Release:** R1  
**Estimate:** 1 US (12 hours), proposed 2026-10-02  
**Goal:** As a person who receives a message from the site, I see Gustavo's logo and design in the contact acknowledgement and account verification emails.
**Head start (2026-10-06):** the three contact email templates exist at MailerSend and the contact flow already sends them through the HTTP API (see the decision log). The HTML sources are in `Docs/email-templates/`. Remaining for this story: the account verification email template and checking all emails in real mail clients.

**Acceptance tests:**

1. Given the contact acknowledgement and the account verification email are sent, when they are opened in common email clients, then they show the logo and follow the approved design, with a readable plain-text alternative.
2. Given an email is inspected, when its content is reviewed, then it contains no tracking or third-party resources, and links, expiry guidance, and privacy rules are unchanged.
3. Given the test mail sink, when emails are sent in automated tests, then recipient, count, and message content assertions still pass.

**Tasks:**

- [ ] Add HTML and plain-text templates through the shared mail sender, embedding the logo without third-party hosting.
- [ ] Test rendering in representative clients and the automated mail-sink tests.

### User Story F9-US7: Verify Accessibility and Responsiveness After the Redesign

**Status:** Proposed  
**Release:** R1  
**Estimate:** 2 US (24 hours), proposed 2026-10-02  
**Goal:** As Gustavo, I know the new design is still accessible and works on the supported devices and browsers before launch.

**Acceptance tests:**

1. Given every public, account, and editor screen, when text and controls are measured against their backgrounds, then contrast meets WCAG AA and state is not conveyed by color alone.
2. Given every screen at 320 CSS pixels, when it is loaded, then there is no horizontal scrolling or clipped content.
3. Given keyboard-only use, when a visitor tabs through each screen, then every control is reachable in a logical order with visible focus.
4. Given reduced-motion is enabled, when pages and transitions run, then motion is removed or minimal and no content depends on it.
5. Given the latest two versions of Chrome, Edge, Firefox, and Safari, when the main flows are exercised, then they work without layout or behavior defects.
6. Given the F1 and F2-US1 acceptance tests, when they are re-run against the new design, then they pass, or the failure is fixed before launch.
7. Given an automated accessibility scan runs, when violations are reviewed, then there are zero critical or serious violations.

**Tasks:**

- [ ] Run contrast, 320 px, keyboard, reduced-motion, and browser-matrix checks and fix findings.
- [ ] Re-run the F1 and F2-US1 acceptance tests and record evidence.
- [ ] Run the automated accessibility scan (see enabler TE5) and record results.

**Feature estimate:** 14 US (168 hours)

## Decisions to Record Before Implementation

These are technical/product-detail choices not settled in the interview. Record the selected option here before the related story is marked `Ready`:

- Contact mail provider, sender identity, verification-link lifetime, pending-message storage mechanism, and delivery retry policy. (Current state: MailerSend SMTP configured through `Email:Smtp:*`, 30-minute in-memory pending store, no automatic retry.)
- Contact rate-limit threshold and exact abuse-control combination.
- Confirm F3 credential policy (the 12-character minimum is decided), verification/reset token lifetime, lockout duration, and authentication endpoint rate limits.
- Image storage location and deletion behavior for unreferenced uploads (formats and size are decided; proposed default: `/data/uploads` on the data volume, and delete an image when its content item is deleted).
- Member profiles (F8): profile URL scheme and where the profile data lives (proposed: a new `Profiles` module with its own table, one row per user).
- F7: how access rules are declared (per entity or per operation) and how a rejected operation is reported.
- Automated test tools for browsers and accessibility (see enabler TE5).
- Redesign (F9): the logo SVG and the inspiration PNG must be supplied, and the design analysis recorded, before F9 stories can be `Ready`.

### Decided on 2026-10-02

| Topic | Decision |
| --- | --- |
| Administrator recovery (F3-US3) | Operator-only interactive reset command on the host; no public or emailed administrator recovery. |
| Member profile viewers (F8) | Signed-in verified users only. |
| Administrator profiles (F8) | Administrators have no member profile; their public identity is the F1 profile. |
| Comment author links (F6, F8) | A commenter's display name links to their member profile (subject to the viewer rule above). |
| Member profile fields (F8) | Hobbies/interests (200 characters), profession (100), location (100, broad free text), about-me (500), all optional and plain text. |
| Image uploads (F4-US5) | JPEG, PNG, and WebP only, maximum 5 MB, files renamed to generated names. SVG and GIF are not accepted. |
| Story/project deletion (F4) | Hard delete after explicit confirmation. |
| Comment deletion (F6-US3) | The comment is replaced by a "[deleted]" placeholder; text and author are removed and replies stay attached. |
| Content URLs (F4, F5) | Readable slugs such as `/stories/my-title` and `/projects/my-title`, with a numeric suffix on collisions. |
| Browser support | Latest two versions of Chrome, Edge, Firefox, and Safari (including iOS Safari and Android Chrome). |
| User-written text (F7) | Normalize on save (trim, strip control characters), reject input over the field limits with a clear error, and always render as plain text. |
| Visual direction (F9) | The earlier "humanist engineering journal" direction and palette are superseded by the redesign in F9. See F9 for the confirmed scope. |

## Decision Log

| Date | Decision | Reason / impact |
| --- | --- | --- |
| 2026-09-25 | Created backlog from the confirmed purpose, profile/contact/editor scope, later public browsing, and later verified-reader comments. | Establishes a measurable product source of truth; no completion status is inferred from existing implementation. |
| 2026-09-25 | Added Gustavo's confirmed persona and a proposed humanist engineering journal visual direction. | Makes profile language, public/private detail boundaries, and design acceptance checks specific; palette and visual metaphor remain subject to Gustavo's explicit approval. |
| 2026-09-30 | Added F4-US1 for Gustavo's editor workspace, content lists, search/filter, create/edit entry points, and confirmed deletion. | Makes the editor's basic interface and management workflow explicit; adds 3 US / 36 hours to the editor feature and R1. |
| 2026-09-30 | Added F3 for administrator and verified-reader accounts; moved reader registration out of comments and renumbered downstream features. | Makes account creation, sign-in, verification, and role boundaries an explicit prerequisite; adds 2 US / 24 hours for R1 administrator authentication while retaining the reader-account estimate in R3. |
| 2026-09-30 | Added a proposed F3 architecture note for Identity cookie sessions, operator-authorized administrator provisioning, R3 reader verification, and API authorization boundaries. | Connects the planned F3 behavior to the existing ASP.NET Core Identity/SQLite stack and calls out mismatches in current public registration and demo seeding; proposed credential, recovery, and abuse-control values still require confirmation. |
| 2026-09-30 | Confirmed that multiple administrator accounts may exist and all explicitly provisioned administrators may manage editor content. | Removes the one-administrator restriction while keeping administrator assignment private and operator-authorized; updates the editor's authorization boundary to the provisioned administrator role. |
| 2026-10-01 | Restructured the code base into a modular monolith and renamed the project from TestWebsite to VaninWebsite (solution, projects, namespaces, cookie names `vaninwebsite.auth`/`vaninwebsite.csrf`, database file `vaninwebsite.db`). SMTP settings moved from `TestMailerSend:*` to `Email:Smtp:*` plus `Contact:RecipientEmail`, supplied by user-secrets or environment variables. Added `goal.md`, `project.md`, and `architecture.md`. | Makes features removable and new ones addable without touching unrelated code, and removes committed credentials from configuration. No product behavior changed. The credentials that were committed must be rotated. Existing Docker volumes keep the old database file name (see architecture.md, "Operations"). |
| 2026-10-05 | Renamed the project from VaninWebsite to GooseWebsite everywhere so it is easy to find in tooling: solution, projects, namespaces, folders, Docker (compose project, containers, image, volumes, network), cookie names `goosewebsite.auth`/`goosewebsite.csrf`, database file `goosewebsite.db`, user-secrets id `goosewebsite-api`, Angular project, and all documents. The person's name (Gustavo Couto Vanin) is unchanged. | The generic names made diagnostics hard. Existing sessions are signed out because the cookie names changed. The development database file and user-secrets were moved to the new names; Docker data under old names must be copied across (see architecture.md, Operations). |
| 2026-10-06 | F2-US2 marked Done after Gustavo confirmed the whole contact flow end to end (verification email, explicit confirmation, message and acknowledgement delivered). Draft cyberpunk HTML email templates were added for the sender confirmation, the sender receipt and the notification to Gustavo, as a head start on F9-US6. | The templates are drafts: SMTP cannot reference a provider-stored template, so they must be rendered by the application (F9-US6) with all values HTML-escaped. |
| 2026-10-06 | Contact emails now go through the MailerSend HTTP API using the three templates Gustavo created (confirmation `z86org868nk4ew13`, receipt `jy7zpl9vnmog5vx6`, owner notification `3vz9dle61vngkj50`) instead of plain-text SMTP; account verification emails stay on SMTP. Each request supplies the variables the templates read, pinned by tests. | SMTP cannot reference a stored template. Adds a `MailerSend:ApiKey` secret (user-secrets or `MAILERSEND_API_KEY`). F9-US6 now only needs the account verification email and in-app wiring checks. |
| 2026-10-06 | The email logo is sent as a plain URL (`{PublicBaseUrl}/email/gcv-logo.png`, overridable with `Contact:LogoUrl`). An inline `cid:` image was tried and reverted because it did not render in the MailerSend templates. | The logo appears in emails once the site has a public address. Until then, test emails show no logo. |
| 2026-10-06 | A second code review of the contact flow led to two fixes: confirmation is now claimed atomically so concurrent clicks deliver once, and a failed receipt no longer causes a duplicate notice to Gustavo on retry. Accepted risk: the per-address limit (2 verification emails per 10 minutes) also counts failed sends, so someone who knows a person's address can use it up and delay that person's own contact by up to 10 minutes; each device is still limited to 5 submissions per 10 minutes. The rename of the database file and secrets id affects only existing local setups (migrated for Gustavo's machine); there is no deployment yet. | Keeps the per-address limit simple. Revisit with a CAPTCHA or a longer-lived verification state if abuse appears. |
| 2026-10-01 | Reopened F2-US2 after code review: the verification link is returned in the API response instead of being emailed, and the Angular form never uses it. Added F3-US3 (administrator recovery, R1) and F3-US4 (reader password reset, R3) so the recovery decisions already named in F3 have stories. Reconciled feature statuses and totals. | Earlier "Done" status for F2-US2 was not supported by its own acceptance tests. Totals rise from 30 US to 32 US (F3 +2); R1 is 20 US, R2 4 US, R3 8 US. |
| 2026-10-02 | Gustavo answered the open questions from the restructure review: the public title in `profile.ts` is correct; the password minimum is **12 characters** (implemented in Identity, the register contract, and the sign-in form; supersedes the 8-character policy recorded in F3-US2); two-week sprints and a capacity of about 6 US per sprint are confirmed; the estimates proposed for F3-US3 and F3-US4 are authorized; sample blog posts and the demo posts in local databases stay during development and are removed before deployment (enabler TE4). | Resolves the password and title items in the risk list. Existing accounts created with shorter passwords keep working; the new minimum applies to new passwords. |
| 2026-10-02 | Resolved open decisions with Gustavo: administrator recovery is an operator-only reset command (F3-US3 now Ready); member profiles are viewable by signed-in verified users only, administrators have none, commenters link to profiles, and the four fields and limits are confirmed; uploads allow JPEG, PNG, and WebP up to 5 MB; story and project deletion is a hard delete after confirmation; deleted comments become a "[deleted]" placeholder; content URLs use readable slugs; browser support is the latest two versions of Chrome, Edge, Firefox, and Safari; user text is normalized and rejected over limits. Full table under "Decided on 2026-10-02". | Unblocks F3-US3, F4-US5, F5, F6, F7, and F8 planning. Remaining open items are listed under "Decisions to Record". |
| 2026-10-02 | Added F9 (Brand and Visual Redesign, R1) at Gustavo's request: a full re-skin from his SVG logo and an inspiration image, followed closely, in a single look; includes design tokens and typography, a rewritten home page, animation, the editor screens, and branded emails; approval after each story; accessibility verified afterward in F9-US7 and still a release gate. Seven stories proposed at 14 US. Supersedes the earlier proposed palette and visual direction. | Adds 14 US to R1 (22 to 36 US) and the total to 53 US. Assets (logo, inspiration image) are not yet supplied, so F9 cannot become Ready until they are. Design is built after F4 and before launch, so F4 is first built in the current look. |
| 2026-10-02 | Gustavo supplied the logo and inspiration image and answered the design questions: use the GCV logo as supplied (it is a raster PNG inside an SVG, so an optimized copy is added); the hero illustration is the background and his portrait moves to About (F9-US3 test 2 amended); keep "Build · Break · Improve", the four category cards, and the Japanese text; navigation matches the mockup with Skills and About as home-page sections. The intro line stays, the footer line "Solve complex problems" is dropped, and all four cards are confirmed. | Design analysis is recorded in Feature F9. Still needed before Sprint 4: the hero illustration as a file and the GitHub and LinkedIn URLs. |
| 2026-10-02 | Added F8 (Member Profiles, R3) at Gustavo's request: users have a profile other users can view; basic details such as hobbies, profession, and location are optional and can be skipped at creation. Stories F8-US1 to F8-US3 proposed at 5 US total. | Extends R3 from 8 US to 13 US and the total from 34 US to 39 US. Visibility (signed-in only versus public), field list, and storage location are recorded as decisions to confirm before the stories are Ready. |
| 2026-10-02 | `DatabaseService` is the intended single gateway for all database reads and writes and was restored as such; modules keep their query logic but never use the context or Identity managers directly. It performs no sanitization or access checks yet, so story F7-US1 was added (2 US, Sprint 2, before the editor). | Keeps cross-cutting safeguards in one place as features grow. Raises totals from 32 US to 34 US; R1 is now 22 US. |
| 2026-10-01 | Implemented F3-US2 ahead of its planned R3 release at Gustavo's request; retained R3 as the intended public-release stage. | Adds reader email verification, generic registration/resend responses, confirmed-email sign-in, and a live verified-reader policy. `Auth:ReaderRegistration:Enabled` defaults off and must be explicitly enabled for the R3 Production release; Development opts in. Reader password recovery remains a separate R3 follow-up and production SMTP delivery requires deployment verification. |
| 2026-10-09 | TE6: the Contact module now exposes `IContactSubmissionService`; the separate interfaces for the token store and abuse guard were removed (both are used only inside the module), and `IContactEmailDeliveryService` stays as the outbound seam that tests replace. | One adapter does not justify a seam (architecture review 2026-10-06). Controller tests that built `ContactController` by hand moved to `ContactSubmissionModuleTests`. |
| 2026-10-09 | Added the architecture-review work as technical enablers in `project.md`: TE6 Contact submission module (Sprint 1, [#2](https://github.com/GucciVanin/goosevaninWebsite/issues/2)), TE7 Reader account registration (Sprint 2, [#4](https://github.com/GucciVanin/goosevaninWebsite/issues/4)), TE8 one Mail module (Sprint 6, after F9-US6, [#5](https://github.com/GucciVanin/goosevaninWebsite/issues/5)), TE9 blog authoring (decided at the start of Sprint 3, [#6](https://github.com/GucciVanin/goosevaninWebsite/issues/6)); the persistence interface design became the first task of F7-US1 ([#3](https://github.com/GucciVanin/goosevaninWebsite/issues/3)). Added `CONTEXT.md`; Verification means a person proving control of an email address by clicking the Verification link. | Refactors rank ahead of feature work where dependencies allow (Gustavo, 2026-10-09). Enabler estimates are proposed and need confirmation; enablers rise from 5 US to 8.5 US. TE8 stays after F9-US6 because the dependency would force a redesign. |

## Progress Summary

| Feature | Release | Estimate | Status | Stories Done |
| --- | --- | ---: | --- | --- |
| F1 Public Profile | R1 | 2 US / 24 hours | Done | 2 of 2 |
| F2 Contact and Message Delivery | R1 | 4 US / 48 hours | Done | 3 of 3 |
| F3 User Accounts and Authentication | R1, R3 | 6 US / 72 hours | In Progress | 1 of 4 |
| F4 Gustavo's Private Content Editor | R1 | 11 US / 132 hours | Proposed | 0 of 5 |
| F5 Public Project and Story Browsing | R2 | 4 US / 48 hours | Proposed | 0 of 2 |
| F6 Verified Reader Comments | R3 | 5 US / 60 hours | Proposed | 0 of 3 |
| F7 Centralized Data Access Safeguards | R1 | 2 US / 24 hours | Proposed | 0 of 1 |
| F8 Member Profiles | R3 | 5 US / 60 hours | Proposed | 0 of 3 |
| F9 Brand and Visual Redesign | R1 | 14 US / 168 hours | Proposed | 0 of 7 |
| **Total planned effort** | **R1-R3** | **53 US / 636 hours** | **In Progress** | **6 of 30** |

By release: R1 is 36 US / 432 hours (F1 2, F2 4, F3-US1 and F3-US3 3, F4 11, F7 2, F9 14), R2 is 4 US / 48 hours, and R3 is 13 US / 156 hours (F3-US2 and F3-US4 3, F6 5, F8 5). Done stories account for 8 US of the 53. Technical enablers that are not product features are tracked in [project.md](./project.md). Re-estimate stories after implementation decisions are recorded or after a story is split; update both this summary and the feature total in the same change.