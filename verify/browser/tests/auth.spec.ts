import { expect, test, type Page } from '@playwright/test';
import { withLock } from '../helpers/lock';
import { adminStatePath } from '../helpers/paths';

// F3-US1 (administrator authentication) through the real sign-in page, on every engine. The seeded test
// administrator's credentials come from the environment (.env.verify); they are typed into the page and never logged.
//
// Lockout: five wrong passwords lock an account. Each test types at most one wrong password per engine, and a correct
// sign-in resets the count, so no account gets near the limit.
//
// Signing out ends every session of the account (the security stamp changes). The sign-out flow therefore uses the
// SECOND seeded administrator (password as reset by the operator-command test), never the first, whose saved session
// the editor screens use; and it holds a lock so the three engines do not end each other's session.

const GENERIC_FAILURE = 'Unable to sign in with those credentials.';

function secondCredentials(): { email: string; password: string } {
  const email = process.env.VERIFY_ADMIN2_EMAIL;
  const password = process.env.VERIFY_ADMIN2_NEW_PASSWORD;
  if (!email || !password) throw new Error('VERIFY_ADMIN2_EMAIL / VERIFY_ADMIN2_NEW_PASSWORD are missing (see .env.verify.example)');
  return { email, password };
}

function credentials(): { email: string; password: string } {
  const email = process.env.VERIFY_ADMIN_EMAIL;
  const password = process.env.VERIFY_ADMIN_PASSWORD;
  if (!email || !password) throw new Error('VERIFY_ADMIN_EMAIL / VERIFY_ADMIN_PASSWORD are missing (see .env.verify.example)');
  return { email, password };
}

async function attemptSignIn(page: Page, email: string, password: string): Promise<void> {
  const emailField = page.getByLabel('Email', { exact: true });
  const passwordField = page.getByLabel('Password', { exact: true });
  await emailField.fill(email);
  await passwordField.fill(password);
  // The form model updates after the input event; the button enables once both fields are valid.
  // Compared as booleans so a failure never prints a credential.
  expect((await emailField.inputValue()) === email, 'email field holds what was typed').toBe(true);
  expect((await passwordField.inputValue()) === password, 'password field holds what was typed').toBe(true);
  const submit = page.getByRole('button', { name: 'Sign in', exact: true });
  const enabled = await expect(submit, 'Sign in is enabled once both fields are filled').toBeEnabled({ timeout: 5_000 }).then(() => true, () => false);
  if (!enabled) {
    const state = await page.evaluate(() => ({
      email: document.querySelector('#email')?.className,
      password: document.querySelector('#password')?.className,
      emailLen: (document.querySelector('#email') as HTMLInputElement | null)?.value.length,
      passwordLen: (document.querySelector('#password') as HTMLInputElement | null)?.value.length,
      busy: document.querySelector('button[type=submit]')?.textContent?.trim(),
      url: location.pathname,
    }));
    throw new Error(`Sign in stayed disabled: ${JSON.stringify(state)}`);
  }
  await submit.click();
}

async function openSignIn(page: Page): Promise<void> {
  await page.goto('/sign-in', { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Sign in', level: 1 })).toBeVisible();
}

// F3-US1-AC1: the session is not readable by script. The page's own script sees only the constant marker.
test('auth: after a real sign-in the session cookie is invisible to script and only the marker is readable', async ({ page, context }) => {
  const { email, password } = credentials();
  await openSignIn(page);
  await attemptSignIn(page, email, password);
  await expect(page.getByText('Signed in as', { exact: false })).toBeVisible();

  const visibleToScript = await page.evaluate(() => document.cookie);
  const names = visibleToScript.split(';').map((pair) => pair.trim().split('=')[0]).filter(Boolean);
  expect(names, 'cookies readable from script').toEqual(['goosewebsite.signedin']);

  // The browser does hold the session cookie, and it is HttpOnly: that is why script cannot see it.
  const held = await context.cookies();
  const session = held.find((cookie) => cookie.name === 'goosewebsite.auth');
  expect(session, 'the browser holds the session cookie').toBeDefined();
  expect(session?.httpOnly).toBe(true);
  expect(session?.secure).toBe(true);
});

// F3-US1-AC5 and AC3: non-enumerating failure text, then a real sign-in and a real sign-out through the page.
test('auth: a wrong password and an unknown email show the same message, sign-in works, Sign out ends the session', async ({ page }) => {
  const { email, password } = secondCredentials();
  await withLock('second-administrator', async () => {
    await openSignIn(page);

    await attemptSignIn(page, 'nobody-at-all@example.test', 'Wrong-password-123');
    const alert = page.getByRole('alert');
    await expect(alert).toHaveText(GENERIC_FAILURE);
    // The page clears the password after a failure; wait for that before typing again, or the late clear erases the new text.
    // Polled as a boolean so a failure never prints the field's content.
    await expect.poll(async () => (await page.getByLabel('Password', { exact: true }).inputValue()) === '', { message: 'password field is cleared after a failed sign-in' }).toBe(true);
    const unknownEmailText = (await alert.textContent())?.trim();

    await attemptSignIn(page, email, 'Wrong-password-123');
    await expect(alert).toHaveText(GENERIC_FAILURE);
    // The page clears the password after a failure; wait for that before typing again, or the late clear erases the new text.
    // Polled as a boolean so a failure never prints the field's content.
    await expect.poll(async () => (await page.getByLabel('Password', { exact: true }).inputValue()) === '', { message: 'password field is cleared after a failed sign-in' }).toBe(true);
    const wrongPasswordText = (await alert.textContent())?.trim();
    expect(wrongPasswordText).toBe(unknownEmailText);

    await attemptSignIn(page, email, password);
    await expect(page.getByText('Signed in as', { exact: false })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign out', exact: true })).toBeVisible();

    // Signed in: the editor opens.
    await page.goto('/editor', { waitUntil: 'networkidle' });
    expect(new URL(page.url()).pathname).toBe('/editor');

    await openSignIn(page);
    await page.getByRole('button', { name: 'Sign out', exact: true }).click();
    await expect(page.getByText('You have signed out.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign out', exact: true })).toHaveCount(0);

    // Signed out: the editor sends the visitor to sign in, and the old session no longer answers.
    await page.goto('/editor', { waitUntil: 'networkidle' });
    expect(new URL(page.url()).pathname).toBe('/sign-in');
    expect((await page.request.get('/api/auth/me')).status()).toBe(401);
  });
});

// F3-US1-AC3 in the browser: the session is gone (cookie cleared) while the marker stays, the way an expired session
// looks. The next write or protected request sends the person to sign in and says why.
test.describe('auth: expired session', () => {
  test.use({ storageState: adminStatePath });

  test('the session cookie is gone but the marker stays: the editor sends the person to sign in and explains', async ({ page, context }) => {
    await page.goto('/editor', { waitUntil: 'networkidle' });
    expect(new URL(page.url()).pathname).toBe('/editor');

    await context.clearCookies({ name: 'goosewebsite.auth' });
    const left = (await context.cookies()).map((cookie) => cookie.name);
    expect(left).not.toContain('goosewebsite.auth');
    expect(left).toContain('goosewebsite.signedin');

    // Switching the list asks the API for the projects; the API answers 401 and the app redirects.
    await page.getByRole('button', { name: 'Projects', exact: true }).click();
    await page.waitForURL(/\/sign-in\?/);

    const url = new URL(page.url());
    expect(url.pathname).toBe('/sign-in');
    expect(url.searchParams.get('reason')).toBe('session-expired');
    expect(url.searchParams.get('returnUrl')).toBe('/editor');
    await expect(page.getByText('Your session expired. Sign in again to continue.')).toBeVisible();
  });
});
