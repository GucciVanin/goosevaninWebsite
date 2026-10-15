// After signing in, a person may be sent back to where they were, but only to a path on this site.
// Anything else (another site, a scheme, a protocol-relative address) is ignored, so a sign-in link cannot redirect away.
export function safeReturnUrl(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  return /^\/(?![/\\])[^\s\\]*$/.test(value) ? value : null;
}
