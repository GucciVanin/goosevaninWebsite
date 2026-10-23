import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, switchMap, tap, throwError } from 'rxjs';
import { AuthenticatedUser } from '../models/authenticated-user';
import { clearSignInMarker, hasSignInMarker } from './session-marker';

// Keeps session mutations on the same CSRF-protected request path.
@Injectable({ providedIn: 'root' })
export class AuthApi {
  constructor(private readonly http: HttpClient) {}

  // Asks the API only when the sign-in marker is present. Without it nobody has signed in, so the answer is
  // "not signed in" (a 401, as the API would say) without a request that the browser would report as a failure.
  getCurrentUser(): Observable<AuthenticatedUser> {
    if (!hasSignInMarker()) {
      return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized', url: '/api/auth/me' }));
    }

    return this.http.get<AuthenticatedUser>('/api/auth/me').pipe(
      // The session is gone (expired or ended elsewhere): forget the hint so no page asks again.
      tap({ error: (error: unknown) => error instanceof HttpErrorResponse && error.status === 401 && clearSignInMarker() }),
    );
  }

  getReaderRegistrationAvailability(): Observable<{ enabled: boolean }> {
    return this.http.get<{ enabled: boolean }>('/api/auth/reader-registration');
  }

  login(email: string, password: string): Observable<AuthenticatedUser> {
    return this.postWithCsrf<AuthenticatedUser>('/api/auth/login', { email, password });
  }

  register(email: string, password: string, displayName: string): Observable<{ message: string }> {
    return this.postWithCsrf<{ message: string }>('/api/auth/register', { email, password, displayName });
  }

  resendEmailVerification(email: string): Observable<{ message: string }> {
    return this.postWithCsrf<{ message: string }>('/api/auth/verification/resend', { email });
  }

  verifyEmail(userId: string, token: string): Observable<{ message: string }> {
    return this.postWithCsrf<{ message: string }>('/api/auth/verify-email', { userId, token });
  }

  logout(): Observable<void> {
    return this.postWithCsrf<void>('/api/auth/logout', {}).pipe(tap(() => clearSignInMarker()));
  }

  private postWithCsrf<TResponse>(url: string, body: unknown): Observable<TResponse> {
    return this.http.get<{ requestToken: string }>('/api/auth/csrf').pipe(
      switchMap(({ requestToken }) =>
        this.http.post<TResponse>(url, body, {
          headers: new HttpHeaders({ 'X-CSRF-TOKEN': requestToken }),
        }),
      ),
    );
  }
}
