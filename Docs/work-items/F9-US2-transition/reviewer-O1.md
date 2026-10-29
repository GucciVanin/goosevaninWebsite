# Reviewer verdict - O1 page transition

Verdict: **accurate in substance, with one documentation inaccuracy to fix (D1)**. No code defect found. No file other than this one was touched.

## Runs
- `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true`: 191 passed, 0 failed.
- Karma (ChromeHeadless, cmd redirect): 207 of 207 SUCCESS.
- I did not run the 3-engine browser stage (it needs the container; I was told to touch none). Its result rests on the worker's verify.ps1 report (824 passed, 0 failed).

## Design versus decisions.md
- 240 ms: `--gcv-motion-base` is 240ms in `tokens.scss`, used for both keyframes with `--gcv-ease`. Tokens only. OK.
- 12 px: `translateY(12px)` is a literal, not a token. Acceptable (no spacing token is claimed), but note it is the only non-token value.
- Header and footer still: both get their own `view-transition-name`, and their groups have `animation-duration: 0s`. OK for the header (fixed, so its rect is identical on every page).
- None on first load: `skipInitialTransition: true`. None on same-page navigation: `isSamePage`. None under reduced motion: skipped in `onViewTransitionCreated`, and the tokens also drop to 0.01 ms. None on home section scrolling: the browser test asserts `startViewTransition` was called 0 times, which confirms the About link is not a router navigation.
- Scroll restoration is unchanged.

## page-transition.ts
- Skip conditions are correct. `isSamePage` compares the leaf `routeConfig` and the params, so two stories (same route, different slug) still animate. This is tested.
- No memory leak: no subscriptions, timers or listeners, and `matchMedia` is read on each call.
- SSR and unsupported browsers: Angular only calls `onViewTransitionCreated` when `document.startViewTransition` exists, and `matchMedia` is guarded with `typeof`. Safe.
- `skipTransition()` after creation is the documented use of this hook.

## site.scss
- The `::view-transition-old/new(root)` rules and the `@keyframes` are valid and use tokens. There is no separate `prefers-reduced-motion` media query in `site.scss`. This is deliberate: the tokens go to 0.01 ms and the router skips the transition. Acceptable.
- Layout and stacking: the header is already `position: fixed; z-index: 20` (already a stacking context), and its only absolutely positioned descendant, the mobile drawer, already has the fixed header as its containing block. The footer has no fixed or absolute descendants. `view-transition-name` adds no layout change and no focus-order change, because the pseudo-elements are not focusable and the DOM is unchanged. OK.
- During the transition the header snapshot is static (the drawer or hidden state is frozen for 240 ms). This is invisible in practice.

## brand-logo.ts side fix
- The explanation is plausible and the fix is sound regardless of the transition. Static `src` and `srcset` are written at creation, while `[attr.sizes]` is applied in the update pass. The browser can pick a candidate with `sizes` unset (default 100vw), which selects the 512 px file, and then re-select 192 px when `sizes` arrives. That is a wasted request or an aborted one. Binding `sizes` before `srcset` in template order means the browser first sees `src` alone (no srcset), then `sizes`, then `srcset` together. This is synchronous, before any image fetch.
- Regression and LCP: the fix cannot select a larger file than before. At 88 and 64 px, 192w is chosen at DPR up to 2, and 512w at DPR above 2. Fewer wasted 512 px fetches means equal or better LCP. No regression.
- The claim that the transition exposed the race is unproven (the worker admitted not investigating, and I cannot reproduce it without the stack). Treat it as a hypothesis. A transition holds the old snapshot while the new DOM is built, which changes timing, so it is plausible but not evidence-backed. The fix does not depend on it.
- Nit: the explanatory comment sits between the `@Component` decorator and the class. It works, but belongs inside the template or above the decorator.

## Browser spec
- Not tautological for the main path: it requires both `::view-transition-old(root)` and `::view-transition-new(root)` animations, the name `gcv-page-in`, a duration of 150 to 400 ms, no header animation longer than 1 ms, and an unmoved header. The worker showed it red with `withViewTransitions` removed on all three engines.
- Direct load: it counts `startViewTransition` calls and pseudo-element animations (both must be 0). Meaningful.
- Weakness (A1): the reduced-motion test only watches for animations via `requestAnimationFrame` polling. If the skip regressed, the tokens would still make the animations 0.01 ms long, and the poll could miss them, so the test could pass wrongly. Karma covers the skip logic, so overall coverage is fine, but the browser test alone is weak there. Hooking `skipTransition` or checking `transition.finished` timing would harden it.
- The "unsupported engine" branch is dead weight while all three engines support the API (the worker confirms Firefox does). The architecture text and the spec comment still say "Firefox today" lacks it; this is stale (A2).

## Documentation
- Backlog test 2 text is restored and the design is recorded. The decision-log row states that it supersedes the rewording row for criterion 2. The rewording row is still present (history) and the supersession is explicit. OK.
- **D1 (defect, docs):** the backlog evidence names the Karma test `asks the browser for a view transition on a route change but not on the first load`. The real test is `asks the browser for a view transition on a route change` (`app.spec.ts:213`), and the worker removed the first-load assertion. Rename the text in the backlog, or restore the assertion. First-load coverage actually comes from the `Page transition` option test and the browser direct-load test.
- A3: the new decision-log row was added above the `| --- | --- | --- |` separator, as the 2026-10-28 W5 row already was. The table markup is already off at that spot, and it renders as a row above the header separator. Pre-existing, but now doubled.
- `architecture.md` 6.2 matches the code (hook, skip conditions, names, tokens). Apart from the "Firefox at the time of writing" statement (A2), it is accurate. `Docs/tests/README.md` row matches the spec.

## Other advisories
- A4: the footer has a view-transition name and `animation-duration: 0s` on its group, but its default old/new image cross-fade (about 250 ms, not token-driven) is not zeroed. Between pages of different height the footer snaps to its new position instantly while the content fades. The footer is usually below the fold, so this is mostly invisible, but on short pages (not-found, sign-in) the footer pops in. Gustavo should judge the feel. Setting `::view-transition-old/new(gcv-footer)` to `animation: none` would make "footer stays still" precise.
- A5: the transition applies to editor and sign-in routes too (the worker's doubt 3). That matches the "sign-in" mention in decisions.md. Harmless.
