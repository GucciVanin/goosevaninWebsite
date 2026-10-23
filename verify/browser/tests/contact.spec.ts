import { expect } from '@playwright/test';
import { eachScreen } from '../helpers/each-screen';
import { settle, type Defect } from '../helpers/known';
import { openScreen } from '../helpers/page';
import { assertViewportWidth } from '../helpers/viewport';

// F9-US7-AC6 (F2-US1 acceptance tests 1 to 4) on the home page contact form, on every engine.
//
// LIMIT, on purpose: this test never submits a valid form. The verification stack may be configured with a real mail
// provider (.env, see verify/runtime/40-contact.sh and 45-mail.sh), and a valid submission emails a verification link
// to the given address. The test therefore goes up to validation and stops. What the stack cannot show here (test 3:
// "the UI confirms that verification is required and does not claim delivery", and the delivery itself) is covered by
// the xUnit tests ContactVerificationApiTests (the link is emailed to the sender, nothing is delivered before
// confirmation, the token and URL are never returned) and by the Karma specs contact-page.spec.ts (the pending status
// after a valid submit) and contact-verify-page.spec.ts (the confirmation screen).
//
// Tests 2 and 4 (order O5): an invalid field is identified by a text error next to it, aria-invalid and
// aria-describedby; the submit button is not disabled, so a keyboard user pressing Enter gets the errors; nothing is
// sent while the form is invalid.

const fields = [
  { id: 'contact-name', label: 'Your name', role: 'textbox', valid: 'Test Visitor', error: 'Enter your name' },
  { id: 'contact-email', label: 'Email address', role: 'textbox', valid: 'visitor@example.test', error: 'Enter a valid email address' },
  { id: 'contact-reason', label: 'Reason', role: 'combobox', valid: 'Personal note', error: 'Choose a reason' },
  { id: 'contact-message', label: 'What are you thinking about?', role: 'textbox', valid: 'A question about a system.', error: 'Write a message' },
] as const;

eachScreen(
  'contact',
  'the contact form has its fields, shows a text error beside each invalid field without sending a request, and keeps labels with the fields',
  async ({ page, screen, info }) => {
    const defects: Defect[] = [];
    const sent: string[] = [];
    // Any request to /api/contact, of any method, is a defect in this test: nothing may be sent.
    page.on('request', (request) => {
      const path = new URL(request.url()).pathname;
      if (path.startsWith('/api/contact')) sent.push(`${request.method()} ${path}`);
    });
    await openScreen(page, screen);

    const form = page.locator('#contact form');
    await form.scrollIntoViewIfNeeded();
    const submit = form.locator('button[type="submit"]');

    // Test 1: the four fields, each reachable by its label (an accessible name), and the exact reason choices.
    for (const field of fields) {
      const byName = form.getByRole(field.role, { name: field.label, exact: true });
      if ((await byName.count()) !== 1) defects.push({ id: `field-missing:${field.id}`, detail: `no single ${field.role} named "${field.label}"` });
      else if ((await byName.getAttribute('id')) !== field.id) defects.push({ id: `label-not-associated:${field.id}`, detail: `"${field.label}" does not name #${field.id}` });
    }
    const choices = await form.locator('#contact-reason option:not([disabled])').allTextContents();
    if (JSON.stringify(choices) !== JSON.stringify(['Work or collaboration', 'Personal note'])) {
      defects.push({ id: 'reason-choices', detail: `choices are ${JSON.stringify(choices)}` });
    }

    const fill = async (id: string, value: string) =>
      id === 'contact-reason' ? form.locator(`#${id}`).selectOption({ label: value }) : form.locator(`#${id}`).fill(value);
    const clear = (id: string) =>
      form.locator(`#${id}`).evaluate((node) => {
        if (node instanceof HTMLSelectElement) node.selectedIndex = 0;
        else (node as HTMLInputElement | HTMLTextAreaElement).value = '';
        node.dispatchEvent(new Event(node instanceof HTMLSelectElement ? 'change' : 'input', { bubbles: true }));
      });
    // Leave the field, as a person does with Tab: focus it, then take the focus away.
    const leave = (id: string) =>
      form.locator(`#${id}`).evaluate((node) => {
        (node as HTMLElement).focus();
        (node as HTMLElement).blur();
      });
    const invalidIds = () => form.locator(':invalid').evaluateAll((nodes) => nodes.map((node) => node.id).filter(Boolean));

    // What the page shows for one field, read from the DOM; the answer settles a moment after the event (Angular).
    type Shown = { text: string | null; ariaInvalid: string | null; describedBy: string | null; adjacent: boolean; alertClass: boolean };
    const shown = (id: string): Promise<Shown> =>
      form.locator(`#${id}`).evaluate((node) => {
        const error = document.getElementById(`${node.id}-error`);
        return {
          text: error ? (error.textContent ?? '').trim() : null,
          ariaInvalid: node.getAttribute('aria-invalid'),
          describedBy: node.getAttribute('aria-describedby'),
          adjacent: !!error && node.nextElementSibling === error,
          alertClass: !!error && error.classList.contains('gcv-status--alert'),
        };
      });
    // Wait until a field shows (or does not show) its error, then report any mismatch as a defect.
    const expectError = async (id: string, text: string | null, why: string) => {
      const settled = await expect.poll(async () => (await shown(id)).text, { timeout: 3000 }).toBe(text).then(() => true, () => false);
      const state = await shown(id);
      if (!settled) defects.push({ id: `error-text:${why}`, detail: `${id} shows ${JSON.stringify(state.text)}, expected ${JSON.stringify(text)}` });
      if (text === null) {
        if (state.ariaInvalid === 'true' || state.describedBy) defects.push({ id: `stale-aria:${why}`, detail: `${id} keeps aria-invalid or aria-describedby` });
      } else if (settled) {
        if (state.ariaInvalid !== 'true') defects.push({ id: `no-aria-invalid:${why}`, detail: `${id} aria-invalid=${state.ariaInvalid}` });
        if (!(state.describedBy ?? '').split(/\s+/).includes(`${id}-error`)) defects.push({ id: `no-aria-describedby:${why}`, detail: `${id} aria-describedby=${state.describedBy}` });
        if (!state.adjacent) defects.push({ id: `error-not-adjacent:${why}`, detail: `${id}-error is not the next element after the field` });
        if (!state.alertClass) defects.push({ id: `error-not-alert-style:${why}`, detail: `${id}-error lacks gcv-status--alert` });
      }
    };
    const expectOnly = async (id: string, why: string) => {
      for (const field of fields) if (field.id !== id) await expectError(field.id, null, `${why}:${field.id}`);
    };

    // Test 2, empty form: nothing is flagged before the person acts, the button is available, and the fields are
    // programmatically invalid.
    for (const field of fields) await expectError(field.id, null, `untouched:${field.id}`);
    if (await submit.isDisabled()) defects.push({ id: 'submit-disabled', detail: 'the submit button is disabled; a keyboard user could not get the errors' });
    const onEmpty = await invalidIds();
    if (JSON.stringify(onEmpty) !== JSON.stringify(fields.map((f) => f.id))) {
      defects.push({ id: 'empty-fields-not-identified', detail: `invalid fields on an empty form: ${JSON.stringify(onEmpty)}` });
    }

    // Each field emptied in turn and left: its error appears (and only its), and goes away after correction.
    for (const missing of fields) {
      for (const field of fields) await fill(field.id, field.valid);
      await clear(missing.id);
      await leave(missing.id);
      await expectError(missing.id, missing.error, `blank:${missing.id}`);
      await expectOnly(missing.id, `blank:${missing.id}`);
      const invalid = await invalidIds();
      if (JSON.stringify(invalid) !== JSON.stringify([missing.id])) defects.push({ id: `missing-not-identified:${missing.id}`, detail: `invalid fields: ${JSON.stringify(invalid)}` });
      await fill(missing.id, missing.valid);
      await expectError(missing.id, null, `corrected:${missing.id}`);
    }

    // Malformed email addresses: the email error appears on the email field only, then disappears when corrected.
    for (const field of fields) await fill(field.id, field.valid);
    for (const malformed of ['not-an-email', 'a@', '@b.test', 'a b@c.test']) {
      await fill('contact-email', malformed);
      await leave('contact-email');
      await expectError('contact-email', 'Enter a valid email address', `malformed:${malformed}`);
      await expectOnly('contact-email', `malformed:${malformed}`);
      await fill('contact-email', 'visitor@example.test');
      await expectError('contact-email', null, `malformed-corrected:${malformed}`);
    }

    // A keyboard user pressing Enter on a fresh, empty form gets every error; nothing is sent.
    await openScreen(page, screen);
    await form.scrollIntoViewIfNeeded();
    for (const field of fields) await expectError(field.id, null, `reloaded:${field.id}`);
    await form.locator('#contact-email').focus();
    await page.keyboard.press('Enter');
    for (const field of fields) await expectError(field.id, field.error, `enter:${field.id}`);
    // The focus moves to the first invalid field (the name), so a screen-reader user is told what is wrong.
    const focusedAfterEnter = await page.evaluate(() => document.activeElement?.id ?? '');
    if (focusedAfterEnter !== 'contact-name') defects.push({ id: 'focus-not-moved-on-submit', detail: `focus is on "${focusedAfterEnter}", expected contact-name` });

    // Fully valid input clears all errors and the form is accepted (the button is available), but it is not pressed.
    for (const field of fields) await fill(field.id, field.valid);
    for (const field of fields) await expectError(field.id, null, `valid:${field.id}`);
    const onValid = await invalidIds();
    if (onValid.length > 0) defects.push({ id: 'valid-form-invalid', detail: `invalid fields: ${JSON.stringify(onValid)}` });
    if (await submit.isDisabled()) defects.push({ id: 'valid-form-blocked', detail: 'the submit button is disabled for valid input' });

    // Test 4: labels stay associated and the form still fits on a phone; no alert region lives outside the form.
    await page.setViewportSize({ width: 375, height: 812 });
    await assertViewportWidth(page, 375);
    for (const field of fields) {
      const control = form.locator(`#${field.id}`);
      await control.scrollIntoViewIfNeeded();
      const label = form.locator(`label[for="${field.id}"]`);
      const labelBox = await label.boundingBox();
      const controlBox = await control.boundingBox();
      if (!labelBox || !controlBox) defects.push({ id: `mobile-field-hidden:${field.id}`, detail: 'label or control has no box at 375 px' });
      else if (controlBox.x + controlBox.width > 375 || labelBox.y > controlBox.y) {
        defects.push({ id: `mobile-field-layout:${field.id}`, detail: `label y=${labelBox.y}, control x=${controlBox.x} w=${controlBox.width}` });
      }
    }
    const strayErrors = await page.locator('#contact [role="alert"]').evaluateAll((nodes) => nodes.filter((node) => !node.closest('form')).length);
    if (strayErrors > 0) defects.push({ id: 'error-outside-form', detail: `${strayErrors} alert region(s) in the contact section outside the form` });

    if (sent.length > 0) defects.push({ id: 'contact-request-sent', detail: `requests: ${sent.join(', ')}` });
    settle(info, screen.id, 'contact', defects);
  },
  ['home'],
);
