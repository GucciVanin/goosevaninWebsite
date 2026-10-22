import { expect, test } from '@playwright/test';
import { settle } from '../helpers/known';
import { listInteractive, tabThrough } from '../helpers/keyboard';
import { openScreen } from '../helpers/page';
import { screens } from '../helpers/screens';
import { assertViewportWidth } from '../helpers/viewport';

// TE5-AC5 at a phone width: on the home page at 320 px the navigation is behind the menu button. With the menu
// opened, the button and every link of the drawer must be reachable with Tab, in page order, with a visible focus.
test('home: keyboard at 320 px with the mobile menu open', { tag: ['@screen:home', '@kind:keyboard-mobile'] }, async ({ page }, info) => {
  const home = screens.find((screen) => screen.id === 'home')!;
  await page.setViewportSize({ width: 320, height: 640 });
  await openScreen(page, home);
  await assertViewportWidth(page, 320);

  const menuButton = page.locator('button.gcv-menu');
  await expect(menuButton).toBeVisible();
  await menuButton.click();
  await expect(menuButton).toHaveAttribute('aria-expanded', 'true');
  const drawerLinks = page.locator('#gcv-drawer a');
  await expect(drawerLinks.first()).toBeVisible();
  const drawerCount = await drawerLinks.count();
  expect(drawerCount, 'the drawer has links').toBeGreaterThan(0);

  const expected = await listInteractive(page);
  // Both the button and each drawer link are part of the list that Tab must reach.
  const numbered = await page.evaluate(() => ({
    button: document.querySelector('button.gcv-menu')?.hasAttribute('data-verify-order') ?? false,
    links: document.querySelectorAll('#gcv-drawer a[data-verify-order]').length,
  }));
  expect(numbered.button, 'the menu button is among the interactive elements').toBe(true);
  expect(numbered.links, 'every drawer link is among the interactive elements').toBe(drawerCount);

  settle(info, 'home', 'keyboard-mobile', await tabThrough(page, info, expected));
});
