# Decisions - F9-US2 page transition (R1 sign-off follow-up)

Gustavo (2026-10-28, review of PR #24): "A page transition is necessary." (He also confirmed that merging counts as approval for F4/F7/F9.) This reverses the Worker's rewording of F9-US2 acceptance test 2: the original criterion (a section or route transition matching an approved motion design) stands, and this work item supplies the motion design.

Motion design (Captain's proposal, built on the existing tokens; Gustavo reviews it in the running site):
- Route changes (home, /projects, /projects/:slug, /stories, /stories/:slug, not-found, sign-in) cross-fade the page content with a small upward settle (about 12 px), using the existing motion duration and ease tokens (`--gcv-motion-*`, `--gcv-ease`), roughly 240 ms. The header and footer stay still.
- Implementation preference: the browser View Transitions API through the Angular router (`withViewTransitions()`), scoped to the routed content so the fixed header does not animate; fall back to no animation where unsupported. Angular route animations are the alternative if View Transitions cannot be scoped.
- `prefers-reduced-motion: reduce`: no transition at all (instant), consistent with the existing reduced-motion rule.
- Scroll restoration (already enabled) must still land at the top; the transition must not delay focus or break the keyboard order, must not cause layout shift, and must not run on the first page load.

Acceptance criteria:
- **PT-AC1** Navigating between routes plays the transition (computed animation or view-transition pseudo-element present for the routed content, duration within 150-400 ms, tokens only); the header does not move.
- **PT-AC2** With reduced motion nothing animates.
- **PT-AC3** Direct load of any route shows the page with no transition and no flash; in-page home section scrolling is unchanged.
- **PT-AC4** Existing behaviour holds: scroll to top on navigation, focus/keyboard checks, axe, 320 px reflow, browser stage on 3 engines (engines without the API simply navigate instantly: the test asserts the page is correct either way).
- **PT-AC5** F9-US2 acceptance test 2 in the backlog is restored to the transition criterion with this design recorded, and the decision-log row of the rewording is superseded.
