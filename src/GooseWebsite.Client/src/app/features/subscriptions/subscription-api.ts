import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

// The public subscription endpoints. The session interceptor adds the CSRF token to these writes.
@Injectable({ providedIn: 'root' })
export class SubscriptionApi {
  private readonly http = inject(HttpClient);

  // Always answers with the same message, whatever the address (the server never says whether it is known).
  subscribe(name: string, email: string, website: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/subscriptions', { name, email, website });
  }

  confirm(token: string): Observable<{ confirmed: boolean; message: string }> {
    return this.http.post<{ confirmed: boolean; message: string }>('/api/subscriptions/confirm', { token });
  }

  unsubscribe(token: string): Observable<{ unsubscribed: boolean; message: string }> {
    return this.http.post<{ unsubscribed: boolean; message: string }>('/api/subscriptions/unsubscribe', { token });
  }
}
