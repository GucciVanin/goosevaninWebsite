> **Archived 2026-10-01.** Superseded by [project.md](../project.md); kept for reference. Items that conflict with confirmed decisions (for example durable contact storage) were intentionally dropped.

# Plan to Stand Out

The current site has a clear point of view, a responsive portfolio flow, and a working contact integration. The next improvements should deepen proof, trust, and memorable interaction without turning the page into a collection of effects.

## 1. Make the work undeniable

- Replace the illustrative project panels with three real case studies containing the brief, constraints, architecture diagram, decisions, and measurable outcomes.
- Add short technical write-ups showing one hard problem, the rejected alternatives, and the final tradeoff.
- Add links to live demos, repositories, or redacted walkthroughs for every project that can be shared.
- Add a compact results layer: latency reduced, deployment frequency improved, cost avoided, or user completion rate increased.

## 2. Add a credible engineering signal

- Publish a small public status page for the portfolio API and contact delivery path.
- Add a now page describing current experiments, reading, and tools being evaluated.
- Add a downloadable, accessible one-page resume generated from the same structured profile data.
- Add verified testimonials from collaborators with role, company, and context.

## 3. Turn the contact path into a real workflow

- Introduce a server-side `IContactService` with durable storage and email notification.
- Add rate limiting, honeypot fields, request size limits, email validation, and spam monitoring.
- Add a privacy notice and consent handling appropriate to the visitor's region.
- Add an automated acknowledgement only after delivery is confirmed.

## 4. Improve discoverability

- Add page metadata, Open Graph images, JSON-LD `Person` and `CreativeWork` structured data, and a generated sitemap.
- Create dedicated, indexable routes for each case study rather than keeping every story on one page.
- Add a content layer for engineering notes with tags, reading time, and RSS.
- Measure search and referral performance with privacy-conscious analytics.

## 5. Create one signature interaction

- Add a lightweight architecture playground where a visitor can toggle tradeoffs such as speed, cost, and resilience and see a system diagram respond.
- Keep the interaction keyboard accessible, reduced-motion friendly, and optional so the portfolio still communicates without it.
- Use real project decisions as the data behind the interaction; the experience should demonstrate thinking, not decoration.

## 6. Raise product quality

- Add Playwright coverage for desktop and mobile layouts, anchor navigation, form validation, and API success/failure states.
- Add visual regression snapshots for the hero, work grid, and contact state.
- Add a proper design token contract and component extraction when more pages arrive.
- Run Lighthouse and axe checks in CI, with performance budgets kept below the current Angular limits.

## Suggested sequence

1. Add real case-study content and outcome metrics.
2. Make contact delivery durable and abuse-resistant.
3. Add SEO, resume, testimonials, and a now page.
4. Add the architecture playground as the signature interaction.
5. Automate browser, accessibility, and performance checks.
