import { expect, test, type Page } from '@playwright/test';
import { openScreen } from '../helpers/page';
import { screens } from '../helpers/screens';

// F9-US2 acceptance test 2 (page transition, PT-AC1..PT-AC3). A route change plays the router's view transition:
// the page content cross-fades and settles up (::view-transition-old/new(root), duration from the motion tokens),
// while the header and footer stay still. With reduced motion nothing animates; a direct load never animates.
// All three engines (Chromium, Firefox, WebKit) support the View Transitions API and run the real animation path;
// the unsupported-engine branch only guards a future engine that lacks it (instant navigation, nothing animates).
const home = screens.find((screen) => screen.id === 'home')!;

// Records every view-transition pseudo-element animation seen from now on, and how often a transition was requested.
async function watchTransitions(page: Page) {
  await page.evaluate(() => {
    const w = window as any;
    w.__vt = { started: 0, skipped: 0, animations: [] as { pseudo: string; name: string; duration: number }[] };
    const original = (document as any).startViewTransition;
    if (original) {
      (document as any).startViewTransition = function (...args: unknown[]) {
        w.__vt.started++;
        const transition = original.apply(document, args);
        // skipTransition() rejects `ready` with an AbortError: that is how the router's skip shows from outside.
        transition.ready.catch(() => { w.__vt.skipped++; });
        return transition;
      };
    }
    const tick = () => {
      for (const animation of document.getAnimations()) {
        const effect = animation.effect as KeyframeEffect | null;
        const pseudo = (effect as any)?.pseudoElement as string | undefined;
        if (pseudo?.startsWith('::view-transition')) {
          const record = { pseudo, name: (animation as CSSAnimation).animationName ?? '', duration: Number(effect!.getComputedTiming().duration) };
          if (!w.__vt.animations.some((seen: any) => seen.pseudo === record.pseudo && seen.name === record.name)) w.__vt.animations.push(record);
        }
      }
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });
}

const seen = (page: Page) => page.evaluate(() => (window as any).__vt as { started: number; skipped: number; animations: { pseudo: string; name: string; duration: number }[] });
const supported = (page: Page) => page.evaluate(() => 'startViewTransition' in document);
const headerTop = (page: Page) => page.locator('header.gcv-bar').evaluate((element) => element.getBoundingClientRect().top);
const footerBox = (page: Page) => page.locator('footer.gcv-footer').evaluate((element) => { const r = element.getBoundingClientRect(); return { top: r.top + window.scrollY, left: r.left }; });

test.describe('with normal motion', () => {
  test.use({ reducedMotion: 'no-preference' });

  test('a route change plays the transition and the header stays still', { tag: ['@screen:home', '@kind:page-transition'] }, async ({ page }) => {
    await openScreen(page, home);
    await watchTransitions(page);
    const before = await headerTop(page);

    await page.locator('header.gcv-bar a[href="/projects"]').first().click();
    await expect(page).toHaveURL(/\/projects$/);
    await expect(page.locator('main, [role="main"]').first()).toBeVisible();

    if (await supported(page)) {
      await expect.poll(async () => (await seen(page)).animations.length, { timeout: 5_000 }).toBeGreaterThan(0);
      const { animations } = await seen(page);
      const incoming = animations.find((animation) => animation.pseudo === '::view-transition-new(root)');
      const outgoing = animations.find((animation) => animation.pseudo === '::view-transition-old(root)');
      expect(incoming, 'the new page animates in').toBeDefined();
      expect(outgoing, 'the old page animates out').toBeDefined();
      expect(incoming!.name).toBe('gcv-page-in');
      for (const animation of [incoming!, outgoing!]) expect(animation.duration, `${animation.pseudo} duration`).toBeGreaterThanOrEqual(150);
      for (const animation of [incoming!, outgoing!]) expect(animation.duration, `${animation.pseudo} duration`).toBeLessThanOrEqual(400);
      // The header has its own view-transition name, so it is never animated.
      expect(animations.filter((animation) => animation.pseudo.includes('gcv-header') && animation.duration > 1)).toEqual([]);
      // The footer is still too: neither its old nor its new image cross-fades (animation: none).
      expect(animations.filter((animation) => animation.pseudo.includes('gcv-footer') && animation.pseudo !== '::view-transition-group(gcv-footer)')).toEqual([]);
    } else {
      expect((await seen(page)).animations).toEqual([]);
    }
    expect(Math.abs((await headerTop(page)) - before)).toBeLessThan(1);
    // The footer does not slide or drift: once the new page is in, its box is settled and stays put.
    const settled = await footerBox(page);
    await page.waitForTimeout(400);
    const later = await footerBox(page);
    expect(Math.abs(later.top - settled.top)).toBeLessThan(1);
    expect(Math.abs(later.left - settled.left)).toBeLessThan(1);
  });

  test('a direct load shows the page with no transition', { tag: ['@screen:projects', '@kind:page-transition'] }, async ({ page }) => {
    await page.addInitScript(() => {
      const w = window as any;
      w.__initialVt = 0;
      const original = (document as any).startViewTransition;
      if (original) (document as any).startViewTransition = function (...args: unknown[]) { w.__initialVt++; return original.apply(document, args); };
    });
    await openScreen(page, screens.find((screen) => screen.id === 'projects')!);
    expect(await page.evaluate(() => (window as any).__initialVt)).toBe(0);
    expect(await page.evaluate(() => document.getAnimations().filter((a) => ((a.effect as any)?.pseudoElement ?? '').startsWith('::view-transition')).length)).toBe(0);
  });

  test('home section scrolling is unchanged: no transition', { tag: ['@screen:home', '@kind:page-transition'] }, async ({ page }) => {
    await openScreen(page, home);
    await watchTransitions(page);
    await page.locator('header.gcv-bar a, header.gcv-bar button').filter({ hasText: /about/i }).first().click();
    await page.waitForTimeout(600);
    expect((await seen(page)).started).toBe(0);
    expect((await seen(page)).animations).toEqual([]);
  });
});

test.describe('with reduced motion', () => {
  test.use({ reducedMotion: 'reduce' });

  test('a route change navigates instantly and nothing animates', { tag: ['@screen:home', '@kind:page-transition'] }, async ({ page }) => {
    await openScreen(page, home);
    await watchTransitions(page);
    await page.locator('header.gcv-bar a[href="/projects"]').first().click();
    await expect(page).toHaveURL(/\/projects$/);
    await expect(page.locator('main, [role="main"]').first()).toBeVisible();
    await page.waitForTimeout(600);
    expect((await seen(page)).animations).toEqual([]);
    // Deterministic: every requested transition was skipped, so no ::view-transition tree was ever created.
    const after = await seen(page);
    if (await supported(page)) {
      expect(after.started, 'a transition was requested').toBeGreaterThan(0);
      expect(after.skipped, 'and the router skipped it').toBe(after.started);
    }
  });
});
