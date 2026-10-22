import { eachScreen } from '../helpers/each-screen';
import { settle, type Defect } from '../helpers/known';
import { openScreen } from '../helpers/page';
import { assertViewportWidth } from '../helpers/viewport';

// TE5-AC4: at a 320 CSS-pixel viewport nothing makes the page scroll sideways.
eachScreen('reflow', 'fits a 320 px viewport without sideways scrolling', async ({ page, screen, info }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  await openScreen(page, screen);
  await assertViewportWidth(page, 320);

  const widths = await page.evaluate(() => ({
    document: document.documentElement.scrollWidth,
    body: document.body.scrollWidth,
    viewport: document.documentElement.clientWidth,
  }));
  const defects: Defect[] = [];
  const widest = Math.max(widths.document, widths.body);
  if (widest > 320) {
    defects.push({ id: 'overflow-x', detail: `scrollWidth ${widest} px (document ${widths.document}, body ${widths.body}) in a ${widths.viewport} px viewport` });
  }
  settle(info, screen.id, 'reflow', defects);
});
