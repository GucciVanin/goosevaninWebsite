# Worker report - O1 page transition

## What it looks like
On a route change the old page fades out and the new page fades in while settling up about 12 px, over 240 ms (`--gcv-motion-base`, `--gcv-ease`). The header and footer have their own view-transition names, so they stay still. Nothing plays on the first load, on in-page home section scrolling, under reduced motion, or when a navigation keeps the same page (only the query/fragment changes).

## Changes
- `src/GooseWebsite.Client/src/app/shared/page-transition.ts` (new): `pageTransitionOptions` (`skipInitialTransition`, `onViewTransitionCreated`), skip under reduced motion and for same-page navigations.
- `app.config.ts`: `withViewTransitions(pageTransitionOptions)`.
- `shared/styles/site.scss`: header/footer names, `::view-transition-old/new(root)` animations (tokens only), keyframes.
- `shared/brand/brand-logo.ts`: `sizes` now bound before `srcset` (see doubt 1).
- Tests: `shared/page-transition.spec.ts` (new, 5 tests), `app.spec.ts` (+1: startViewTransition is requested on a route change), `verify/browser/tests/page-transition.spec.ts` (new: route change plays old/new(root) animations of 150-400 ms with the header unmoved, direct load plays none, home section scrolling plays none, reduced motion plays none; engines without the API are asserted to navigate with no animation).
- Docs: backlog F9-US2 test 2 restored with the design, evidence paragraph, task box note, status line, decision-log row superseding the rewording row; `Docs/architecture.md` new section 6.2; `Docs/tests/README.md` table row. No story status changed.

## Evidence
- Red first (partly): with `withViewTransitions` removed the new browser tests failed on all three engines (no pseudo-element animation) and the Karma app test failed. Honest note: I wrote the implementation and the specs together before the first run, then proved the red by removing the provider.
- Karma standalone (Windows, ChromeHeadless): 205 of 205 passed before the later edits. `dotnet test`: 191 passed, 0 failed (no .NET change).
- Final `./scripts/verify.ps1 -SkipCi`: **824 passed, 0 failed, 0 skipped, 0 known** (includes the Angular and .NET unit stages and the 3-engine browser stage).

## Doubts
1. Adding the transition made `contact-confirmation` and `email-confirmation` fail on chromium ("request-failed GET /brand/gcv-logo-512.webp ERR_ABORTED"), deterministically. Root cause is a latent race in `BrandLogo`: the static `srcset` was read before `sizes` existed, so Chromium started the 512 px file and aborted it. I fixed it in the component (bind `sizes` before `srcset`); verified green. I did not investigate why only those two pages exposed it.
2. Firefox now supports the View Transitions API (the Firefox browser test took the "animations present" path), so all three engines assert real animations; the "unsupported engine" branch is only exercised if one lacks it.
3. The transition covers all non-first, different-page navigations, including editor routes and sign-in (not only the public ones listed in decisions.md); judged harmless. Gustavo should judge the feel in the running site (240 ms, 12 px).
4. The first-load assertion was removed from the Karma app test because it was timing-dependent in the verify container; first-load behaviour is covered by the option unit test and the browser direct-load test.
