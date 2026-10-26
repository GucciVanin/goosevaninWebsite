import { expect, type Page } from '@playwright/test';

/** Asserts the page really has the requested viewport width before anything is measured in it. */
export async function assertViewportWidth(page: Page, width: number): Promise<void> {
  const actual = await page.evaluate(() => window.innerWidth);
  expect(actual, `the page must have a ${width} px viewport before it is measured (innerWidth is ${actual})`).toBe(width);
}
