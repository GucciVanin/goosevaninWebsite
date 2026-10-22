import { rmSync } from 'node:fs';
import { adminStatePath } from './paths';

// The saved session is deleted when the run ends.
export default async function globalTeardown(): Promise<void> {
  rmSync(adminStatePath, { force: true });
}
