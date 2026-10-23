import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';
import { axeDefects } from '../helpers/axe';
import { evaluate, type KnownIssue } from '../helpers/known';
import { listInteractive, tabThrough } from '../helpers/keyboard';
import { assertViewportWidth } from '../helpers/viewport';

// Self-tests of the checks themselves (negative controls kept on purpose): each feeds a check a page or a case that
// is wrong in a known way and asserts that the check notices. They need no running application.

const ids = (defects: { id: string }[]) => defects.map((defect) => defect.id);

test('selftest: a focus indicator must be a change from the unfocused state', { tag: '@selftest' }, async ({ page }, info) => {
  await page.setContent(`
    <style>
      #good:focus-visible { outline: 3px solid blue; }
      #shadow:focus-visible { box-shadow: 0 0 0 3px blue; }
      #none, #none:focus-visible { outline: none; box-shadow: none; }
      #static, #static:focus-visible { outline: 2px solid red; }
    </style>
    <button id="good">good</button><button id="shadow">shadow</button><button id="none">none</button><button id="static">static</button>`);
  const expected = await listInteractive(page);
  const defects = ids(await tabThrough(page, info, expected));
  expect(defects.sort()).toEqual(['no-focus-indicator:button#none "none"', 'no-focus-indicator:button#static "static"']);
});

test('selftest: the reachable-element selector covers every kind of control', { tag: '@selftest' }, async ({ page }) => {
  await page.setContent(`
    <a href="#x">link</a><button>button</button><input aria-label="field"><select aria-label="pick"><option>a</option></select><textarea aria-label="area"></textarea>
    <details><summary>summary</summary>more</details>
    <div id="editable" contenteditable="true">edit</div>
    <div id="role-button" role="button" tabindex="0">rb</div><div id="role-link" role="link" tabindex="0">rl</div>
    <div id="role-checkbox" role="checkbox" tabindex="0">rc</div><div id="role-tab" role="tab" tabindex="0">rt</div><div id="role-menuitem" role="menuitem" tabindex="0">rm</div>
    <div id="plain-tabindex" tabindex="0">ti</div>
    <iframe id="frame" title="frame" srcdoc="<p>x</p>"></iframe>
    <video id="vid" controls width="100" height="50"></video><audio id="aud" controls></audio>
    <button id="off" disabled>disabled</button><div id="skip" tabindex="-1">skip</div><input type="hidden" id="hid"><button id="gone" hidden>gone</button>`);
  const found = (await listInteractive(page)).join(' | ');
  const wantedElements = ['a[href=#x]', 'button "button"', 'input', 'select', 'textarea', 'summary', 'div#editable', 'div#role-button', 'div#role-link', 'div#role-checkbox', 'div#role-tab', 'div#role-menuitem', 'div#plain-tabindex', 'iframe#frame', 'video#vid', 'audio#aud'];
  for (const wanted of wantedElements) expect(found, `${wanted} is listed`).toContain(wanted);
  for (const unwanted of ['#off', '#skip', '#hid', '#gone']) expect(found, `${unwanted} is not listed`).not.toContain(unwanted);
});

test('selftest: an axe violation is identified by rule and element', { tag: '@selftest' }, async ({ page }, info) => {
  await page.setContent('<main><h1>t</h1><img id="first" src="data:,"><img id="second" src="data:,"></main>');
  const results = await new AxeBuilder({ page }).withTags(['wcag2a']).analyze();
  const defects = ids(axeDefects(info, results.violations)).filter((id) => id.includes('image-alt'));
  expect(defects.length).toBe(2);
  expect(defects[0]).toContain('image-alt');
  expect(defects[0]).toContain('#first');
  expect(defects[1]).toContain('#second');
});

test('selftest: a known axe entry for one element does not mask another element', { tag: '@selftest' }, async () => {
  const entry = (id: string): KnownIssue => ({ id, screen: 'home', kind: 'a11y', description: 'x', fixedBy: 'F9-US7' });
  const known = [entry('axe:image-alt:img#first')];
  const defect = (id: string) => ({ id, detail: 'd' });
  expect(evaluate(known, 'chromium', 'home', 'a11y', [defect('axe:image-alt:img#first')]).problems).toEqual([]);
  const masked = evaluate(known, 'chromium', 'home', 'a11y', [defect('axe:image-alt:img#first'), defect('axe:image-alt:img#second')]).problems;
  expect(masked.length).toBe(1);
  expect(masked[0]).toContain('new defect axe:image-alt:img#second');
});

test('selftest: the 320 px check asserts the real viewport width', { tag: '@selftest' }, async ({ page }) => {
  await page.setViewportSize({ width: 400, height: 640 });
  await expect(assertViewportWidth(page, 320)).rejects.toThrow(/320/);
  await page.setViewportSize({ width: 320, height: 640 });
  await assertViewportWidth(page, 320);
});
