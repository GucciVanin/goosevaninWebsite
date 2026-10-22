import { test, type Page, type TestInfo } from '@playwright/test';
import { adminStatePath } from './paths';
import { screens, type Screen } from './screens';

/**
 * Defines one test per screen in the screen list. Administrator screens use the seeded administrator's session.
 * Each test is tagged with its screen and its check kind (`@screen:home`, `@kind:keyboard`): 40-browser.sh reads the
 * tags from `playwright test --list` to find known-issues entries that match no test.
 */
export function eachScreen(
  kind: string,
  title: string,
  body: (context: { page: Page; screen: Screen; info: TestInfo }) => Promise<void>,
  only: string[] = screens.map((screen) => screen.id),
): void {
  for (const screen of screens.filter((s) => only.includes(s.id))) {
    test.describe(screen.id, () => {
      if (screen.admin) test.use({ storageState: adminStatePath });
      test(`${screen.id}: ${title}`, { tag: [`@screen:${screen.id}`, `@kind:${kind}`] }, async ({ page }, info) => body({ page, screen, info }));
    });
  }
}
