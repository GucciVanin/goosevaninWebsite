# Security review - O1 page transition

Reviewer: Security. Scope: uncommitted work on r1-signoff (`shared/page-transition.ts`, `app.config.ts`, `site.scss`, `brand-logo.ts`, `app.spec.ts`, `verify/browser/tests/page-transition.spec.ts`, architecture 6.2). Read-only review; no code run.

**Verdict: PASS. No blocking or medium findings. Three informational notes.**

| # | Area | Severity | Result |
| --- | --- | --- | --- |
| 1 | XSS / DOM injection | None | Pass |
| 2 | State visible across routes (snapshots) | Info | Pass |
| 3 | Information leak / logging | None | Pass |
| 4 | Motion safety (prefers-reduced-motion) | Low (hardening note) | Pass |
| 5 | brand-logo.ts srcset binding | Info | Pass |
| 6 | Browser spec and secrets | None | Pass |

## 1. XSS / DOM injection - Pass
`page-transition.ts` handles only router snapshots (`routeConfig`, `params`) and calls `skipTransition()`. It writes nothing to the DOM, uses no `innerHTML`, `eval` or dynamic style. `JSON.stringify(params)` is used only for equality and never rendered or logged. `site.scss` is static; no user data reaches CSS. `brand-logo.ts` binds a constant string literal to `srcset`; `size()` feeds `sizes`/width/height as a number input from component templates, not from request data (no new sink; Angular sanitizes attribute bindings anyway).

## 2. State exposed between routes - Pass (Info)
The View Transitions API snapshots the old page as a bitmap in the browser compositor for about 240 ms. The snapshot is same-document, not readable by script (pseudo-elements expose no pixels), not stored, and not sent anywhere. It does not cross origins. Therefore an editor, sign-in or signed-in page does not leak to the next page in any way a visitor on the same device could not already see. Risks considered:
- Sign-out or navigation from an editor to a public page briefly shows the fading editor content for 240 ms to someone looking at the screen. That is the same exposure as the page being visible a moment earlier; no new party gains access.
- Password manager or form values: bitmap only, not cleared or retained after the transition finishes.
- Info: worker doubt 3 (transition also applies to editor and sign-in routes) is acceptable from a security view. If Gustavo wants zero fade of private content, add a route data flag and skip in `pageTransitionStarted`; not required.

## 3. Information leaks - Pass
No logging, no network calls, no storage. The new Karma and browser tests record only animation names and durations. No new headers or endpoints; CSP and cache headers are untouched (the API does not set a CSP in code, and view transitions need none).

## 4. Motion safety - Pass (Low note)
Reduced motion is honoured twice: JS skips the transition (`matchMedia('(prefers-reduced-motion: reduce)')`), and `tokens.scss` already collapses `--gcv-motion-base` under reduced motion, which both pseudo-element animations use. Duration is 240 ms and travel 12 px, well below vestibular-trigger thresholds; no looping or parallax. `skipInitialTransition` avoids a first-load animation. Low hardening note: `matchMedia` is read at transition time, so a mid-session preference change is honoured. A late-arriving fallback is not needed. Keyboard focus is unaffected since the DOM updates normally; during the 240 ms the page is not interactive to pointer events (browser behaviour), which is acceptable.

## 5. brand-logo.ts - Pass
Moving `srcset` to an attribute binding after `sizes` is a loading-order fix with no security effect. Same two same-origin static assets.

## 6. Browser spec and secrets - Pass
The spec uses local routes and `window` instrumentation (monkey-patching `document.startViewTransition` in the test page only, with `addInitScript` limited to the test context). No credentials, tokens, emails or hosts. No secrets in the diff, docs or work item files.
