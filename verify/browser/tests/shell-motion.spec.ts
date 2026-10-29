import { expect, test, type Page } from '@playwright/test';
import { openScreen } from '../helpers/page';
import { screens } from '../helpers/screens';

// F9-US2 acceptance test 2 (shell transitions with a reduced-motion path). The shell has one transition that
// reacts to the visitor: the fixed header slides away when scrolling down and returns when scrolling up (its
// `transform` and `background` transitions), and the navigation links fade their colour. This proves both halves:
// with normal motion the header's transition is real (a non-zero duration, and the header does move off screen);
// with prefers-reduced-motion: reduce the same header change is immediate (duration at most 0.01 ms, nothing
// running). Route changes use no transition at all (the router renders the next page in place), so there is
// nothing else to cover.
const home = screens.find((screen) => screen.id === 'home')!;

const seconds = (value: string) => Math.max(...value.split(',').map((part) => (part.trim().endsWith('ms') ? parseFloat(part) / 1000 : parseFloat(part))));

async function durations(page: Page) {
  return page.evaluate(() => {
    const bar = getComputedStyle(document.querySelector('header.gcv-bar')!);
    const link = getComputedStyle(document.querySelector('header.gcv-bar .gcv-nav a')!);
    return { bar: bar.transitionDuration, link: link.transitionDuration, barProperty: bar.transitionProperty };
  });
}

async function scrollDownThenUp(page: Page) {
  const bar = page.locator('header.gcv-bar');
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' }));
  await page.evaluate(() => window.scrollTo({ top: 400, behavior: 'instant' }));
  await expect(bar).toHaveClass(/header-hidden/);
  await page.evaluate(() => window.scrollTo({ top: 300, behavior: 'instant' }));
  await expect(bar).not.toHaveClass(/header-hidden/);
}

test.describe('with normal motion', () => {
  test.use({ reducedMotion: 'no-preference' });

  test('home: the header slides away and back with a real transition', { tag: ['@screen:home', '@kind:shell-motion'] }, async ({ page }) => {
    await openScreen(page, home);
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(false);

    const before = await durations(page);
    expect(before.barProperty).toContain('transform');
    expect(seconds(before.bar), `header transition duration is "${before.bar}"`).toBeGreaterThan(0.05);
    expect(seconds(before.link), `link transition duration is "${before.link}"`).toBeGreaterThan(0.05);

    await scrollDownThenUp(page);
  });
});

test.describe('with reduced motion', () => {
  test.use({ reducedMotion: 'reduce' });

  test('home: the header change is immediate and nothing transitions', { tag: ['@screen:home', '@kind:shell-motion'] }, async ({ page }) => {
    await openScreen(page, home);
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);

    const reduced = await durations(page);
    expect(seconds(reduced.bar), `header transition duration is "${reduced.bar}"`).toBeLessThanOrEqual(0.00002);
    expect(seconds(reduced.link), `link transition duration is "${reduced.link}"`).toBeLessThanOrEqual(0.00002);

    await scrollDownThenUp(page);
    // The header still works (it hid and returned), and no transition is left running on it.
    const running = () => page.evaluate(() => document.querySelector('header.gcv-bar')!.getAnimations().filter((animation) => animation.playState === 'running').length);
    await expect.poll(running, { timeout: 3_000, intervals: [50, 100, 250] }).toBe(0);
  });
});
