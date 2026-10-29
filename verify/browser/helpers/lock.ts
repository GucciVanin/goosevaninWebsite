import { mkdirSync, rmSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

// A cross-process mutex for tests that sign an account OUT. Signing out rotates the account's security stamp, which
// ends every session of that account, so two engines doing it at once (workers run in parallel) would end each
// other's session mid-test. `mkdir` is atomic, so the directory is the lock; a lock older than a minute is stale
// (a crashed worker) and is taken over. Polling here waits for another test, never for the page.
const root = join(tmpdir(), 'goosewebsite-verify-locks');

export async function withLock<T>(name: string, body: () => Promise<T>): Promise<T> {
  mkdirSync(root, { recursive: true });
  const lock = join(root, name);
  for (;;) {
    try {
      mkdirSync(lock);
      break;
    } catch {
      try {
        if (Date.now() - statSync(lock).mtimeMs > 60_000) rmSync(lock, { recursive: true, force: true });
      } catch {
        // The holder just released it; try again.
      }
      await new Promise((resolve) => setTimeout(resolve, 200));
    }
  }
  try {
    return await body();
  } finally {
    rmSync(lock, { recursive: true, force: true });
  }
}
