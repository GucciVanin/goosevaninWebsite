import { expect, test } from '@playwright/test';
import { settle, type Defect } from '../helpers/known';
import { openScreen } from '../helpers/page';
import { screens } from '../helpers/screens';

// TE5-AC6: with prefers-reduced-motion: reduce, nothing animates on the home page (the <gcv-scene> shadow root
// included) and the page content is complete.
test.use({ reducedMotion: 'reduce' });

test('home: reduced motion stops every animation and keeps the content', { tag: ['@screen:home', '@kind:motion'] }, async ({ page }, info) => {
  const home = screens.find((screen) => screen.id === 'home')!;
  await openScreen(page, home);

  // The preference really is in force for this page.
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);

  // Content: the heading, the scene with something in its shadow root, and a body with real text.
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.locator('gcv-scene')).toHaveCount(1);
  expect(await page.locator('gcv-scene').evaluate((scene) => (scene.shadowRoot?.childElementCount ?? 0))).toBeGreaterThan(0);
  expect(await page.evaluate(() => document.body.innerText.trim().length)).toBeGreaterThan(200);

  // Animations and transitions: poll until none is running (a finite one may still be finishing), and fail when
  // some are still running after the wait. Collected over the document and every open shadow root.
  const running = () =>
    page.evaluate(() => {
      const found = new Set<string>();
      const describe = (animation: Animation) => {
        const name = (animation as CSSAnimation).animationName ?? (animation as CSSTransition).transitionProperty ?? 'web-animation';
        const kind = animation instanceof CSSTransition ? 'transition' : 'animation';
        const target = animation.effect && 'target' in animation.effect ? (animation.effect.target as Element | null) : null;
        return `${kind}:${name} on <${target?.tagName.toLowerCase() ?? '?'}>`;
      };
      const visit = (root: Document | ShadowRoot) => {
        for (const animation of root.getAnimations()) if (animation.playState === 'running' || animation.pending) found.add(describe(animation));
        for (const element of Array.from(root.querySelectorAll('*'))) {
          if (element.shadowRoot) visit(element.shadowRoot);
          for (const animation of element.getAnimations()) if (animation.playState === 'running' || animation.pending) found.add(describe(animation));
        }
      };
      visit(document);
      return [...found].sort();
    });

  let last: string[] = [];
  try {
    await expect.poll(async () => (last = await running()), { timeout: 3_000, intervals: [100, 250, 500] }).toEqual([]);
  } catch {
    // fall through: `last` holds what was still running
  }
  const defects: Defect[] = last.map((description) => ({ id: `running-${description}`, detail: `${description} still running with prefers-reduced-motion: reduce` }));
  settle(info, 'home', 'motion', defects);
});
