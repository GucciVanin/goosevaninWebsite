import type { Page, TestInfo } from '@playwright/test';
import { log, type Defect } from './known';

// Keyboard check (TE5-AC5), shared by the per-screen test, the 320 px menu test and the self-tests.

/**
 * Finds every element that takes keyboard focus, in document order (open shadow roots included), numbers it with
 * `data-verify-order`, and remembers how it looks unfocused (outline and box-shadow) for the focus-indicator check.
 * Covered: links, buttons, form fields, summary, contenteditable, iframes, audio and video with controls, anything with
 * a tabindex of 0 or more (which includes the ARIA widgets button, link, checkbox, tab and menuitem when they get one).
 * Not covered (a known limit): the content of iframes, and positive tabindex values reordering the tab order.
 */
export async function listInteractive(page: Page): Promise<string[]> {
  return page.evaluate(() => {
    const selector = [
      'a[href]', 'button', 'input:not([type="hidden"])', 'select', 'textarea', 'summary', 'iframe',
      'audio[controls]', 'video[controls]', '[contenteditable=""]', '[contenteditable="true"]', '[contenteditable="plaintext-only"]',
      '[tabindex]', '[role="button"][tabindex]', '[role="link"][tabindex]', '[role="checkbox"][tabindex]', '[role="tab"][tabindex]', '[role="menuitem"][tabindex]',
    ].join(', ');
    // Elements whose tabIndex property reads -1 although the browser focuses them with Tab.
    const focusableByDefault = 'iframe, audio[controls], video[controls], [contenteditable=""], [contenteditable="true"], [contenteditable="plaintext-only"], details > summary:first-of-type';
    const signature = (element: Element) => {
      const style = getComputedStyle(element);
      return [style.outlineStyle, style.outlineWidth, style.outlineColor, style.outlineOffset, style.boxShadow].join('|');
    };
    const baseline: Record<number, string> = {};
    (window as unknown as { __verifyBaseline: Record<number, string> }).__verifyBaseline = baseline;
    const found: string[] = [];
    const visit = (root: Document | ShadowRoot) => {
      for (const element of Array.from(root.querySelectorAll('*'))) {
        const html = element as HTMLElement & { disabled?: boolean };
        const attribute = html.getAttribute('tabindex');
        const explicit = attribute !== null && !Number.isNaN(parseInt(attribute, 10));
        const takesTab = explicit ? parseInt(attribute as string, 10) >= 0 : html.tabIndex >= 0 || element.matches(focusableByDefault);
        if (element.matches(selector) && !html.disabled && takesTab && html.checkVisibility({ visibilityProperty: true })) {
          html.setAttribute('data-verify-order', String(found.length));
          baseline[found.length] = signature(element);
          const text = (html.getAttribute('aria-label') || html.innerText || html.getAttribute('placeholder') || '').replace(/\s+/g, ' ').trim().slice(0, 30);
          const href = html.getAttribute('href');
          found.push(`${element.tagName.toLowerCase()}${html.id ? '#' + html.id : ''}${href ? '[href=' + href + ']' : ''} "${text}"`);
        }
        if (element.shadowRoot) visit(element.shadowRoot);
      }
    };
    visit(document);
    return found;
  });
}

/** Presses Tab from the top until every listed element was reached, and returns the defects found. */
export async function tabThrough(page: Page, info: TestInfo, expected: string[]): Promise<Defect[]> {
  // Start from the top of the document. Blurring is not enough: after a click or an earlier focus the browser remembers
  // where it was and Tab continues from there. A throw-away focusable marker at the very start moves that position.
  await page.evaluate(() => {
    (document.activeElement as HTMLElement | null)?.blur();
    const marker = document.createElement('span');
    marker.tabIndex = -1;
    document.body.prepend(marker);
    marker.focus();
    marker.remove();
    window.scrollTo(0, 0);
  });
  const visited: number[] = [];
  // An element can hold several Tab stops (a date field has one per part and one for its picker); only the stops where
  // the element itself has focus are judged, and it needs an indicator on at least one of them.
  const indicated = new Set<number>();
  for (let presses = 0; presses < expected.length * 3 + 10 && new Set(visited).size < expected.length; presses++) {
    await page.keyboard.press('Tab');
    const focus = await page.evaluate(() => {
      let active: Element | null = document.activeElement;
      while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
      const order = active?.getAttribute('data-verify-order');
      if (order === null || order === undefined || !active) return null;
      const style = getComputedStyle(active);
      const outline = style.outlineStyle !== 'none' && parseFloat(style.outlineWidth) > 0;
      const shadow = style.boxShadow !== 'none' && style.boxShadow !== '';
      const now = [style.outlineStyle, style.outlineWidth, style.outlineColor, style.outlineOffset, style.boxShadow].join('|');
      const before = (window as unknown as { __verifyBaseline: Record<number, string> }).__verifyBaseline[Number(order)];
      // The indicator must be visible and must differ from the unfocused look; a decoration that is always there marks nothing.
      return { order: Number(order), focused: active.matches(':focus'), indicator: (outline || shadow) && now !== before };
    });
    // Stops outside the numbered list (a browser's own scroll areas, the address bar) are not page elements.
    if (!focus) continue;
    visited.push(focus.order);
    if (focus.focused && focus.indicator) indicated.add(focus.order);
  }

  log(info, `${expected.length} interactive element(s) in page order: ${expected.join(' | ')}`);
  log(info, `Tab visited ${new Set(visited).size} of them, in the order ${[...new Set(visited)].join(',')}`);
  const defects: Defect[] = [];
  expected.forEach((description, index) => {
    if (!visited.includes(index)) defects.push({ id: `unreachable:${description}`, detail: `${description} was never reached with Tab` });
  });
  const firstSeen = [...new Set(visited)];
  const outOfOrder = firstSeen.find((order, index) => index > 0 && order < firstSeen[index - 1]);
  if (outOfOrder !== undefined) {
    defects.push({ id: `order:${expected[outOfOrder]}`, detail: `Tab order differs from page order; first seen sequence ${firstSeen.join(',')}` });
  }
  for (const order of new Set(visited)) {
    if (indicated.has(order)) continue;
    defects.push({ id: `no-focus-indicator:${expected[order]}`, detail: `${expected[order]} shows no outline or box-shadow change when focused` });
  }
  return defects;
}
