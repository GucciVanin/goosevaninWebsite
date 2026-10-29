# Worker report - F3-US1 order O2 (test hygiene)

Only `verify/browser/tests/auth.spec.ts` changed. No application code, no git write, no credential printed.

- Security S1: the two `toHaveValue('')` assertions on the password field (after the unknown-email and wrong-password failures) are now `expect.poll(async () => (await field.inputValue()) === '').toBe(true)`. They still wait for the late clear (the race workaround stays) and still fail if the field is not cleared, but the failure shows only true/false.
- Reviewer note 1: the assertion `expect(visibleToScript).not.toContain('goosewebsite.auth')` is removed. The `names` equality assertion before it (`['goosewebsite.signedin']`) already fails first for any extra cookie and prints names only.

Verification: full `./scripts/verify.ps1 -SkipCi` once: 790 passed, 0 failed, 0 skipped, 0 known (browser 249 of 249 reported; the three auth tests passed on all engines).
