import { expect, type Page } from '@playwright/test';
import type { Defect } from './known';
import type { Screen } from './screens';

/** Opens a screen and waits until the application has rendered and the network is quiet. */
export async function openScreen(page: Page, screen: Screen): Promise<void> {
  await page.goto(screen.path, { waitUntil: 'networkidle' });
  // A screen that redirected (for example a signed-out editor) is a failed test, not a different screen.
  expect(new URL(page.url()).pathname, `${screen.id} stayed on ${screen.path}`).toBe(screen.path);
  await expect(page.locator('app-root')).not.toBeEmpty();
  await expect(page.locator('main, [role="main"]').first()).toBeVisible();
}

/** Records failed requests and console errors from now on. Read `.defects` at the end of the test. */
export function watchProblems(page: Page): { defects: Defect[] } {
  const defects: Defect[] = [];
  const route = (url: string) => new URL(url).pathname;
  page.on('pageerror', (error) => defects.push({ id: 'uncaught-error', detail: error.message.slice(0, 300) }));
  page.on('console', (message) => {
    // A refused request already shows as a console error in Chromium; it is reported once, as the request.
    if (message.type() !== 'error' || /^Failed to load resource/.test(message.text())) return;
    defects.push({ id: `console-error:${message.text().replace(/\s+/g, ' ').slice(0, 80)}`, detail: message.text().slice(0, 300) });
  });
  page.on('requestfailed', (request) =>
    defects.push({ id: `request-failed:${request.method()} ${route(request.url())}`, detail: `${request.url()} ${request.failure()?.errorText ?? ''}` }),
  );
  page.on('response', (response) => {
    if (response.status() >= 400) {
      defects.push({ id: `request-${response.status()}:${response.request().method()} ${route(response.url())}`, detail: `${response.url()} answered ${response.status()}` });
    }
  });
  return { defects };
}
