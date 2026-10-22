import { request } from '@playwright/test';
import { adminStatePath } from './paths';

// Signs the seeded test administrator in once (through the API, exactly as the sign-in page does) and keeps the
// session for the screens that need it. Credentials come from the environment (.env.verify); they are never printed.
export default async function globalSetup(): Promise<void> {
  const email = process.env.VERIFY_ADMIN_EMAIL;
  const password = process.env.VERIFY_ADMIN_PASSWORD;
  if (!email || !password) {
    throw new Error('VERIFY_ADMIN_EMAIL / VERIFY_ADMIN_PASSWORD are missing (see .env.verify.example)');
  }

  const api = await request.newContext({ baseURL: 'https://localhost', ignoreHTTPSErrors: true });
  const csrf = await api.get('/api/auth/csrf');
  if (!csrf.ok()) throw new Error(`the CSRF endpoint answered ${csrf.status()}`);
  const { requestToken } = (await csrf.json()) as { requestToken: string };

  const login = await api.post('/api/auth/login', { headers: { 'X-CSRF-TOKEN': requestToken }, data: { email, password } });
  if (!login.ok()) throw new Error(`administrator sign-in answered ${login.status()}`);

  await api.storageState({ path: adminStatePath });
  await api.dispose();
}
