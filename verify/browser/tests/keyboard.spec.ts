import { eachScreen } from '../helpers/each-screen';
import { settle } from '../helpers/known';
import { listInteractive, tabThrough } from '../helpers/keyboard';
import { openScreen } from '../helpers/page';

// TE5-AC5: tabbing from the top reaches every interactive element in page order, and each focused element shows a
// visible focus indicator that is a change from its unfocused state.
eachScreen('keyboard', 'keyboard: every control is reachable in page order with a visible focus', async ({ page, screen, info }) => {
  await openScreen(page, screen);
  const expected = await listInteractive(page);
  settle(info, screen.id, 'keyboard', await tabThrough(page, info, expected));
});
