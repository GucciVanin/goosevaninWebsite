import { tmpdir } from 'node:os';
import { join } from 'node:path';

/** The administrator's saved session (cookies). Lives outside the repository and is removed after the run. */
export const adminStatePath = process.env.VERIFY_BROWSER_STATE ?? join(tmpdir(), 'goosewebsite-verify-admin-state.json');
