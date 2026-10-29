import { ActivatedRouteSnapshot, ViewTransitionInfo, ViewTransitionsFeatureOptions } from '@angular/router';

const prefersReducedMotion = () => typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

const leaf = (route: ActivatedRouteSnapshot): ActivatedRouteSnapshot => (route.firstChild ? leaf(route.firstChild) : route);

// True when the navigation keeps the same page (only the query string or fragment changed, for example when a
// confirmation page removes the token from its address). Nothing changes on screen, so there is nothing to animate.
export function isSamePage(from: ActivatedRouteSnapshot, to: ActivatedRouteSnapshot): boolean {
  const a = leaf(from);
  const b = leaf(to);
  return a.routeConfig === b.routeConfig && JSON.stringify(a.params) === JSON.stringify(b.params);
}

// Skips the browser transition when the visitor prefers reduced motion (the page changes instantly) or when the
// navigation stays on the same page.
export function pageTransitionStarted(info: ViewTransitionInfo, reducedMotion: () => boolean = prefersReducedMotion): void {
  if (reducedMotion() || isSamePage(info.from, info.to)) info.transition.skipTransition();
}

// Route changes cross-fade the page content with a small upward settle (the pseudo-element rules are in site.scss).
// The first load never animates; browsers without the View Transitions API simply navigate.
export const pageTransitionOptions: ViewTransitionsFeatureOptions = {
  skipInitialTransition: true,
  onViewTransitionCreated: (info) => pageTransitionStarted(info),
};
