import AxeBuilder from '@axe-core/playwright';
import { axeDefects } from '../helpers/axe';
import { eachScreen } from '../helpers/each-screen';
import { settle } from '../helpers/known';
import { openScreen } from '../helpers/page';

// TE5-AC3: axe-core with the WCAG 2.1 A and AA rule sets. A critical or serious violation fails the test (or is
// KNOWN when listed, per rule and element); every violation, whatever its impact, is written to the log with its elements.
eachScreen('a11y', 'has no critical or serious WCAG 2.1 A/AA violation', async ({ page, screen, info }) => {
  await openScreen(page, screen);
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
  settle(info, screen.id, 'a11y', axeDefects(info, results.violations));
});
