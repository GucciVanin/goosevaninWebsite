import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';
import { axeDefects } from '../helpers/axe';
import { settle, type Defect } from '../helpers/known';
import { watchProblems } from '../helpers/page';

// F5-US1-AC1, AC2 and AC4: the public project pages. The verification stack publishes no projects, so the index shows
// its empty state, and an unknown project shows the site's not-found page (the API's 404 for that slug is expected and
// is the only failed request allowed). Each page is read at 375 px and at desktop width: no sideways scroll, and no
// critical or serious axe violation.
const pages = [
  { id: 'projects-empty', path: '/projects', heading: 'Projects', text: 'Projects will appear here as soon as Gustavo publishes them.', expected404: '' },
  { id: 'project-not-found', path: '/projects/nope', heading: 'Page not found', text: 'There is nothing at this address.', expected404: 'request-404:GET /api/projects/nope' },
];

async function overflow(page: Page): Promise<number> {
  return page.evaluate(() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - document.documentElement.clientWidth);
}

for (const target of pages) {
  test(`${target.id}: direct load shows the state, with header and footer`, { tag: [`@screen:${target.id}`, '@kind:projects'] }, async ({ page }, info) => {
    const problems = watchProblems(page);
    const response = await page.goto(target.path, { waitUntil: 'networkidle' });
    expect(response?.status(), 'the shell is served for a direct load').toBe(200);

    const defects: Defect[] = problems.defects.filter((defect) => defect.id !== target.expected404);
    await expect(page.getByRole('main')).toBeVisible();
    await expect(page.getByRole('heading', { level: 1, name: target.heading })).toBeVisible();
    await expect(page.getByText(target.text)).toBeVisible();
    await expect(page.getByRole('banner')).toBeVisible();
    await expect(page.getByRole('contentinfo')).toBeVisible();
    if (target.id === 'project-not-found') await expect(page.getByRole('link', { name: /back to the home page/i })).toHaveAttribute('href', '/');
    settle(info, target.id, 'projects', defects);
  });

  for (const width of [375, 1280]) {
    test(`${target.id}: no sideways scroll at ${width} px`, { tag: [`@screen:${target.id}`, '@kind:projects'] }, async ({ page }, info) => {
      await page.setViewportSize({ width, height: 800 });
      await page.goto(target.path, { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { level: 1, name: target.heading })).toBeVisible();
      const extra = await overflow(page);
      settle(info, target.id, 'projects', extra > 0 ? [{ id: `overflow-x:${width}`, detail: `${extra} px wider than the viewport` }] : []);
    });
  }

  for (const width of [375, 1280]) {
    test(`${target.id}: the first line sits below the fixed header at ${width} px`, { tag: [`@screen:${target.id}`, '@kind:projects'] }, async ({ page }, info) => {
      await page.setViewportSize({ width, height: 800 });
      await page.goto(target.path, { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { level: 1, name: target.heading })).toBeVisible();
      const gap = await page.evaluate(() => {
        const bar = document.querySelector('.gcv-bar')!.getBoundingClientRect();
        const first = document.querySelector('main > :first-child')!.getBoundingClientRect();
        return first.top - bar.bottom;
      });
      settle(info, target.id, 'projects', gap < 0 ? [{ id: `under-header:${width}`, detail: `the first line starts ${-gap} px under the header` }] : []);
    });
  }

  test(`${target.id}: axe finds no critical or serious violation`, { tag: [`@screen:${target.id}`, '@kind:projects'] }, async ({ page }, info) => {
    await page.goto(target.path, { waitUntil: 'networkidle' });
    await expect(page.getByRole('heading', { level: 1, name: target.heading })).toBeVisible();
    const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
    settle(info, target.id, 'projects', axeDefects(info, results.violations));
  });
}
