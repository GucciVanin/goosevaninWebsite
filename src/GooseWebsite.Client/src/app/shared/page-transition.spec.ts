import { ActivatedRouteSnapshot } from '@angular/router';
import { pageTransitionOptions, pageTransitionStarted } from './page-transition';

const page = (config: object, params = {}) => ({ routeConfig: config, params, firstChild: null }) as unknown as ActivatedRouteSnapshot;
const pageA = {};
const pageB = {};

// F9-US2 page transition: the router asks the browser for a view transition on every route change except the first
// load, and the transition is skipped (instant navigation) when the visitor prefers reduced motion.
describe('Page transition', () => {
  it('does not play on the first page load', () => {
    expect(pageTransitionOptions.skipInitialTransition).toBeTrue();
  });

  it('is skipped when the visitor prefers reduced motion', () => {
    const skipTransition = jasmine.createSpy('skipTransition');
    pageTransitionStarted({ transition: { skipTransition } as unknown as ViewTransition, from: page(pageA), to: page(pageB) } as never, () => true);
    expect(skipTransition).toHaveBeenCalledTimes(1);
  });

  it('plays when the visitor has no motion preference', () => {
    const skipTransition = jasmine.createSpy('skipTransition');
    pageTransitionStarted({ transition: { skipTransition } as unknown as ViewTransition, from: page(pageA), to: page(pageB) } as never, () => false);
    expect(skipTransition).not.toHaveBeenCalled();
  });

  it('is skipped when the navigation stays on the same page (only the query string changed)', () => {
    const skipTransition = jasmine.createSpy('skipTransition');
    pageTransitionStarted({ transition: { skipTransition } as unknown as ViewTransition, from: page(pageA, { slug: 'x' }), to: page(pageA, { slug: 'x' }) } as never, () => false);
    expect(skipTransition).toHaveBeenCalledTimes(1);
  });

  it('plays between two stories (same route, different slug)', () => {
    const skipTransition = jasmine.createSpy('skipTransition');
    pageTransitionStarted({ transition: { skipTransition } as unknown as ViewTransition, from: page(pageA, { slug: 'x' }), to: page(pageA, { slug: 'y' }) } as never, () => false);
    expect(skipTransition).not.toHaveBeenCalled();
  });
});
