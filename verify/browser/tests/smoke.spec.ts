import { expect, test } from '@playwright/test';

// Proves the browser layer works end to end on every engine: the stack is reachable through the forwarder, the
// certificate is accepted, and the Angular application renders its home page.
test('smoke: the home page loads', async ({ page }) => {
  const response = await page.goto('/', { waitUntil: 'networkidle' });
  expect(response?.status()).toBe(200);
  await expect(page).toHaveTitle('Gustavo Couto Vanin');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
});
