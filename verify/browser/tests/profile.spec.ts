import { expect } from '@playwright/test';
import { eachScreen } from '../helpers/each-screen';
import { settle, type Defect } from '../helpers/known';
import { openScreen } from '../helpers/page';

// F9-US7-AC6 (F1-US1 tests 1 and 2): the home page shows who Gustavo is, in visible text and not only to assistive
// technology, and links to his profiles. The expected strings are the profile content (shared/models/profile.ts).
const expected = {
  name: 'Gustavo Couto Vanin',
  positioning: 'Engineer interested in people and technology.',
  bio: 'I am a curious person who likes learning across sports, gaming, and culture.',
  summary: 'I build scalable security programs, automate complex workflows, and partner directly with engineering teams to ship secure software fast.',
};

// A small deny-list: an age or a street address must never appear on the public page.
const denied: { id: string; pattern: RegExp }[] = [
  { id: 'age', pattern: /\b\d{1,3}[\s-]+years?[\s-]+old\b/i },
  { id: 'age', pattern: /\b(?:age|aged)\s*:?\s*\d{2}\b/i },
  { id: 'street-address', pattern: /\b\d{1,5}\s+(?:[A-Z][A-Za-z]+\s+){1,3}(?:Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Lane|Ln|Drive|Dr|Way|Court|Ct)\b\.?/ },
  { id: 'zip-code', pattern: /\b[A-Z]{2}\s+\d{5}(?:-\d{4})?\b/ },
];

eachScreen(
  'profile',
  'shows the name, positioning, bio and summary, the portrait and the profile links',
  async ({ page, screen, info }) => {
    const defects: Defect[] = [];
    await openScreen(page, screen);

    // Visible text: the About section renders each piece in a box a person can see (not the screen-reader-only copy).
    const about = page.locator('#approach');
    await about.evaluate((node) => node.scrollIntoView({ block: 'start' }));
    for (const [key, text] of Object.entries(expected)) {
      const element = about.getByText(text, { exact: false }).first();
      const box = (await element.count()) === 0 ? null : await element.boundingBox({ timeout: 3000 }).catch(() => null);
      const isVisibleText = box !== null && box.width > 40 && box.height > 8;
      const screenReaderOnly = box !== null && (await element.evaluate((node) => node.closest('.sr-only') !== null));
      if (!isVisibleText || screenReaderOnly) defects.push({ id: `missing-visible-text:${key}`, detail: `"${text.slice(0, 40)}" is not visibly shown in the About section` });
    }

    // Portrait: descriptive alt text, and the image actually loads.
    const portrait = page.locator('#approach img').first();
    await portrait.evaluate((img) => img.scrollIntoView({ block: 'center' }));
    const alt = (await portrait.getAttribute('alt')) ?? '';
    if (alt.trim().length < 15 || !/portrait/i.test(alt)) defects.push({ id: 'portrait-alt', detail: `alt text is "${alt}"` });
    await expect.poll(() => portrait.evaluate((img: HTMLImageElement) => img.naturalWidth), { message: 'portrait loaded' }).toBeGreaterThan(0).catch(() => {
      defects.push({ id: 'portrait-not-loaded', detail: 'naturalWidth is 0' });
    });

    // Links: GitHub, LinkedIn and email, with the expected targets; external ones open safely in a new tab.
    const links = [
      { id: 'github', href: 'https://github.com/GucciVanin', external: true },
      { id: 'linkedin', href: 'https://www.linkedin.com/in/gustavovanin/', external: true },
      { id: 'email', href: 'mailto:gustavo.vanin@hotmail.com', external: false },
    ];
    for (const link of links) {
      const anchors = page.locator(`a[href="${link.href}"]`);
      if ((await anchors.count()) === 0) {
        defects.push({ id: `link-missing:${link.id}`, detail: `no link to ${link.href}` });
        continue;
      }
      if (link.external) {
        for (const anchor of await anchors.all()) {
          const rel = ((await anchor.getAttribute('rel')) ?? '').split(/\s+/);
          const target = await anchor.getAttribute('target');
          if (target !== '_blank' || !rel.includes('noopener') || !rel.includes('noreferrer')) {
            defects.push({ id: `link-unsafe:${link.id}`, detail: `target=${target} rel=${rel.join(' ')}` });
          }
        }
      }
    }

    // Deny-list over all the text of the page.
    const text = await page.locator('body').innerText();
    for (const rule of denied) {
      const match = rule.pattern.exec(text);
      if (match) defects.push({ id: `denied-text:${rule.id}`, detail: `found "${match[0]}"` });
    }

    settle(info, screen.id, 'profile', defects);
  },
  ['home'],
);
