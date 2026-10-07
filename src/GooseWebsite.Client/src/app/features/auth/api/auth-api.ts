import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { AuthenticatedUser } from '../models/authenticated-user';

// Keeps session mutations on the same CSRF-protected request path.
@Injectable({ providedIn: 'root' })
export class AuthApi {
  constructor(private readonly http: HttpClient) {}

  getCurrentUser(): Observable<AuthenticatedUser> {
    return this.http.get<AuthenticatedUser>('/api/auth/me');
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
    return this.postWithCsrf<void>('/api/auth/logout', {});
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
