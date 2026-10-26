import { eachScreen } from '../helpers/each-screen';
import { settle } from '../helpers/known';
import { openScreen, watchProblems } from '../helpers/page';

// TE5-AC2: every screen loads with no failed request and no uncaught console error.
eachScreen('load', 'loads with no failed request and no console error', async ({ page, screen, info }) => {
  const problems = watchProblems(page);
  await openScreen(page, screen);
  settle(info, screen.id, 'load', problems.defects);
});
