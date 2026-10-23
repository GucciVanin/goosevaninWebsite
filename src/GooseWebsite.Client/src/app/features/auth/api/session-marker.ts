// The sign-in marker: a plain cookie the API sets at sign-in and clears at sign-out (Secure, SameSite=Lax, path /,
// a session cookie, value "1"). It is only a hint that lets the app skip asking GET /api/auth/me when nobody has
// signed in, so a signed-out visitor causes no 401. It holds no identity and no token and proves nothing: the
// HttpOnly session cookie is the only proof, and the API checks it on every request.
const NAME = 'goosewebsite.signedin';

export function hasSignInMarker(): boolean {
  return document.cookie.split(';').some((part) => part.trim().startsWith(`${NAME}=`));
}

// Used when the API reports the session is gone (401) and after sign-out. Same name, path and flags as the API's
// cookie, so the browser matches it; Secure is added only where the page itself is on https.
export function clearSignInMarker(): void {
  const secure = location.protocol === 'https:' ? '; Secure' : '';
  document.cookie = `${NAME}=; path=/; max-age=0; SameSite=Lax${secure}`;
}
