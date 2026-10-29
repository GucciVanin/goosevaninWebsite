import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page, type Route } from '@playwright/test';
import { axeDefects } from '../helpers/axe';
import { settle, type Defect } from '../helpers/known';
import { watchProblems } from '../helpers/page';

// F10-US1 (O1): the subscribe form on the home page (section and footer) and the confirmation page.
//
// LIMIT, on purpose: no request ever reaches /api/subscriptions. The stack may have a real mail provider configured
// (.env), and a valid subscription emails a confirmation link. Every call to the two subscription endpoints is
// answered by the test itself (page.route), so the form's states can be read without a server, a database row or an
// email. The server side (double opt-in, identical answers, expiry, abuse controls, the stored columns, log privacy)
// is covered by the xUnit tests in SubscriptionsApiTests, with the test mail service.
const generic = 'Thanks. If that address can be subscribed, a confirmation email is on its way. Open the link in it to finish.';

const forms = [
  { id: 'section', root: '#subscribe', prefix: 'home-subscribe' },
  { id: 'footer', root: 'footer', prefix: 'footer-subscribe' },
];

async function answer(page: Page, path: string, status: number, body: object): Promise<string[]> {
  const bodies: string[] = [];
  await page.route(`**${path}`, async (route: Route) => {
    bodies.push(route.request().postData() ?? '');
    await route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
  });
  return bodies;
}

for (const form of forms) {
  test(`subscribe-${form.id}: labelled fields, a hidden honeypot, errors in words and nothing sent while invalid`, { tag: ['@screen:subscribe-home', '@kind:subscribe'] }, async ({ page }, info) => {
    const defects: Defect[] = watchProblems(page).defects;
    const sent: string[] = [];
    page.on('request', (request) => {
      if (new URL(request.url()).pathname.startsWith('/api/subscriptions')) sent.push(request.method());
    });
    await page.goto('/', { waitUntil: 'networkidle' });
    const root = page.locator(form.root).locator('form');
    await root.scrollIntoViewIfNeeded();

    const name = root.getByRole('textbox', { name: 'Your name', exact: true });
    const email = root.getByRole('textbox', { name: 'Email address', exact: true });
    await expect(name).toHaveAttribute('id', `${form.prefix}-name`);
    await expect(email).toHaveAttribute('id', `${form.prefix}-email`);
    await expect(root.getByRole('button', { name: /^Subscribe/ })).toBeEnabled();

    // The honeypot exists but a person can neither see nor reach it.
    const trap = root.locator('input[name="website"]');
    await expect(trap).toHaveCount(1);
    await expect(trap).toHaveAttribute('tabindex', '-1');
    if ((await trap.evaluate((node) => node.closest('[aria-hidden="true"]') !== null)) === false) defects.push({ id: 'honeypot-not-hidden', detail: 'the honeypot is not inside an aria-hidden wrapper' });
    const trapBox = await trap.boundingBox();
    if (trapBox && trapBox.x > -100 && trapBox.x < 4000 && trapBox.width > 2) defects.push({ id: 'honeypot-visible', detail: `honeypot box ${JSON.stringify(trapBox)}` });

    // Enter on an empty form gives both errors, next to the fields, and moves focus to the first.
    await email.focus();
    await page.keyboard.press('Enter');
    await expect(root.locator(`#${form.prefix}-name-error`)).toHaveText('Enter your name');
    await expect(root.locator(`#${form.prefix}-email-error`)).toHaveText('Enter a valid email address');
    await expect(name).toHaveAttribute('aria-invalid', 'true');
    await expect(name).toBeFocused();

    // A malformed address is refused in words; correcting it clears the error.
    await name.fill('Test Visitor');
    await email.fill('not-an-email');
    await root.getByRole('button', { name: /^Subscribe/ }).click();
    await expect(root.locator(`#${form.prefix}-email-error`)).toBeVisible();
    await email.fill('visitor@example.test');
    await expect(root.locator(`#${form.prefix}-email-error`)).toHaveCount(0);

    if (sent.length > 0) defects.push({ id: 'subscription-request-sent', detail: `requests: ${sent.join(', ')}` });
    settle(info, 'subscribe-home', 'subscribe', defects);
  });

  test(`subscribe-${form.id}: a valid submission posts name, email and the empty honeypot and then says what to do next`, { tag: ['@screen:subscribe-home', '@kind:subscribe'] }, async ({ page }, info) => {
    const defects: Defect[] = watchProblems(page).defects;
    const bodies = await answer(page, '/api/subscriptions', 200, { message: generic });
    await page.goto('/', { waitUntil: 'networkidle' });
    const root = page.locator(form.root).locator('form');
    await root.scrollIntoViewIfNeeded();
    await root.getByRole('textbox', { name: 'Your name', exact: true }).fill('Test Visitor');
    await root.getByRole('textbox', { name: 'Email address', exact: true }).fill('visitor@example.test');
    await root.getByRole('button', { name: /^Subscribe/ }).click();

    await expect(page.locator(form.root).getByRole('status').filter({ hasText: generic })).toBeVisible();
    await expect(page.locator(form.root).locator('form')).toHaveCount(0);
    if (bodies.length !== 1) defects.push({ id: 'request-count', detail: `${bodies.length} request(s) reached the (stubbed) endpoint` });
    else {
      const posted = JSON.parse(bodies[0]);
      if (posted.name !== 'Test Visitor' || posted.email !== 'visitor@example.test' || posted.website !== '') defects.push({ id: 'request-body', detail: JSON.stringify(Object.keys(posted)) });
    }
    settle(info, 'subscribe-home', 'subscribe', defects);
  });
}

test('subscribe-section: too many attempts are explained in words and the form stays usable', { tag: ['@screen:subscribe-home', '@kind:subscribe'] }, async ({ page }, info) => {
  const defects: Defect[] = watchProblems(page).defects.filter((defect) => defect.id !== 'request-429:POST /api/subscriptions');
  await answer(page, '/api/subscriptions', 429, {});
  await page.goto('/', { waitUntil: 'networkidle' });
  const root = page.locator('#subscribe form');
  await root.scrollIntoViewIfNeeded();
  await root.getByRole('textbox', { name: 'Your name', exact: true }).fill('Test Visitor');
  await root.getByRole('textbox', { name: 'Email address', exact: true }).fill('visitor@example.test');
  await root.getByRole('button', { name: /^Subscribe/ }).click();

  await expect(root.getByRole('alert')).toContainText('Too many attempts');
  await expect(root.getByRole('button', { name: /^Subscribe/ })).toBeEnabled();
  settle(info, 'subscribe-home', 'subscribe', defects);
});

test('subscribe-confirm: opening the link changes nothing; the button confirms and the page says so', { tag: ['@screen:subscribe-confirm', '@kind:subscribe'] }, async ({ page }, info) => {
  const defects: Defect[] = watchProblems(page).defects;
  const bodies = await answer(page, '/api/subscriptions/confirm', 200, { confirmed: true, message: 'Your subscription is confirmed. Thank you.' });
  await page.goto('/subscribe/confirm#token=stub-token', { waitUntil: 'networkidle' });

  await expect(page).toHaveTitle('Confirm subscription - Gustavo Couto Vanin');
  await expect(page.getByRole('heading', { level: 1, name: 'Confirm your subscription' })).toBeVisible();
  await expect(page.getByRole('banner')).toHaveCount(0);
  if (bodies.length !== 0) defects.push({ id: 'confirmed-on-open', detail: 'a request was sent before any action' });

  await page.getByRole('button', { name: 'Confirm subscription' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Your subscription is confirmed' })).toBeVisible();
  if (bodies.length !== 1 || JSON.parse(bodies[0]).token !== 'stub-token') defects.push({ id: 'confirm-body', detail: bodies.join('|') });
  if (page.url().includes('stub-token')) defects.push({ id: 'token-left-in-address', detail: 'the token is still in the address bar' });
  settle(info, 'subscribe-confirm', 'subscribe', defects);
});

test('subscribe-confirm: an invalid, expired or used link is explained in words', { tag: ['@screen:subscribe-confirm', '@kind:subscribe'] }, async ({ page }, info) => {
  const defects: Defect[] = watchProblems(page).defects.filter((defect) => defect.id !== 'request-400:POST /api/subscriptions/confirm');
  await answer(page, '/api/subscriptions/confirm', 400, { error: 'This confirmation link is no longer valid.' });
  await page.goto('/subscribe/confirm#token=stale', { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: 'Confirm subscription' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'no longer valid' })).toBeVisible();
  await expect(page.getByRole('button')).toHaveCount(0);

  // A link with no token at all says the same, with no button.
  await page.goto('/subscribe/confirm', { waitUntil: 'networkidle' });
  await expect(page.getByRole('status').filter({ hasText: 'no longer valid' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Confirm subscription' })).toHaveCount(0);
  settle(info, 'subscribe-confirm', 'subscribe', defects);
});

test('subscribe-home: no critical or serious axe violation with the form open to errors', { tag: ['@screen:subscribe-home', '@kind:subscribe'] }, async ({ page }, info) => {
  await page.goto('/', { waitUntil: 'networkidle' });
  const root = page.locator('#subscribe form');
  await root.scrollIntoViewIfNeeded();
  await root.getByRole('button', { name: /^Subscribe/ }).click();
  await expect(root.locator('#home-subscribe-name-error')).toBeVisible();
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
  settle(info, 'subscribe-home', 'subscribe', axeDefects(info, results.violations));
});

// F10-US2 (O2): the unsubscribe page. Same limit as above: the endpoint is stubbed, nothing reaches the server.
test('unsubscribe: opening the link changes nothing; the button unsubscribes and the page says the data is deleted', { tag: ['@screen:unsubscribe', '@kind:subscribe'] }, async ({ page }, info) => {
  const defects: Defect[] = watchProblems(page).defects;
  const bodies = await answer(page, '/api/subscriptions/unsubscribe', 200, { unsubscribed: true, message: 'You are unsubscribed and your name and email address are deleted.' });
  await page.goto('/unsubscribe#token=stub-token', { waitUntil: 'networkidle' });

  await expect(page).toHaveTitle('Unsubscribe - Gustavo Couto Vanin');
  await expect(page.getByRole('heading', { level: 1, name: 'Unsubscribe' })).toBeVisible();
  await expect(page.getByRole('banner')).toHaveCount(0);
  if (bodies.length !== 0) defects.push({ id: 'unsubscribed-on-open', detail: 'a request was sent before any action' });

  await page.getByRole('button', { name: 'Unsubscribe' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'You are unsubscribed' })).toBeVisible();
  if (bodies.length !== 1 || JSON.parse(bodies[0]).token !== 'stub-token') defects.push({ id: 'unsubscribe-body', detail: bodies.join('|') });
  if (page.url().includes('stub-token')) defects.push({ id: 'token-left-in-address', detail: 'the token is still in the address bar' });
  settle(info, 'unsubscribe', 'subscribe', defects);
});

test('unsubscribe: an invalid or used link gets one neutral message, and a link with no token offers no button', { tag: ['@screen:unsubscribe', '@kind:subscribe'] }, async ({ page }, info) => {
  const defects: Defect[] = watchProblems(page).defects.filter((defect) => defect.id !== 'request-400:POST /api/subscriptions/unsubscribe');
  await answer(page, '/api/subscriptions/unsubscribe', 400, { error: 'This unsubscribe link is no longer valid.' });
  await page.goto('/unsubscribe#token=stale', { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: 'Unsubscribe' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'This unsubscribe link is no longer valid.' })).toBeVisible();
  await expect(page.getByRole('button')).toHaveCount(0);

  await page.goto('/unsubscribe', { waitUntil: 'networkidle' });
  await expect(page.getByRole('status').filter({ hasText: 'no longer valid' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Unsubscribe' })).toHaveCount(0);
  settle(info, 'unsubscribe', 'subscribe', defects);
});
