import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

const SAFE_METHODS = ['GET', 'HEAD', 'OPTIONS'];

// Gives every write to the API its CSRF token, and sends the person to sign in when a request finds the session gone.
export const sessionInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const http = inject(HttpClient);

  const isApi = request.url.startsWith('/api/');
  const needsToken = isApi && !SAFE_METHODS.includes(request.method) && !request.headers.has('X-CSRF-TOKEN');

  const response$ = needsToken
    ? http
        .get<{ requestToken: string }>('/api/auth/csrf')
        .pipe(switchMap(({ requestToken }) => next(request.clone({ setHeaders: { 'X-CSRF-TOKEN': requestToken } }))))
    : next(request);

  return response$.pipe(
    catchError((error: unknown) => {
      // /api/auth/* answers 401 for a wrong password or an already signed-out caller; that is not an expired session.
      const sessionGone =
        isApi && !request.url.startsWith('/api/auth/') && error instanceof HttpErrorResponse && error.status === 401;
      // Already on the sign-in page: do not nest it inside its own return address.
      if (sessionGone && !router.url.startsWith('/sign-in')) {
        void router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url, reason: 'session-expired' } });
      }

      return throwError(() => error);
    }),
  );
};
