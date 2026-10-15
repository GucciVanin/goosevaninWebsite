import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthApi } from '../auth/api/auth-api';

// Keeps everyone but a signed-in administrator off the editor. The API enforces the same rule on every write;
// this only spares visitors a page they cannot use.
export const editorGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);

  return inject(AuthApi)
    .getCurrentUser()
    .pipe(
      map((user): boolean | UrlTree => (user.isAdmin ? true : router.createUrlTree(['/access-denied']))),
      // Not signed in (401), or the session could not be checked: ask to sign in, then come back here.
      catchError(() => of(router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } }))),
    );
};
