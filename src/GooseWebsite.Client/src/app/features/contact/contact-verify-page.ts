import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BrandLogo } from '../../shared/brand/brand-logo';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

// Requires an explicit user action so email link scanners cannot deliver a contact message.
@Component({
  selector: 'app-contact-verify-page',
  imports: [BrandLogo, RouterLink],
  template: `
    <main class="gcv-auth">
      <header class="gcv-auth__top"><a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a><a class="gcv-link" routerLink="/">Back to the site</a></header>
      <section class="gcv-auth__panel gcv-card" aria-labelledby="verification-title">
        <p class="gcv-eyebrow">Contact</p>
        <h1 id="verification-title" class="gcv-heading gcv-heading--section">Confirm your email</h1>
        @if (message()) {
          <p class="gcv-status" role="status">{{ message() }}</p>
        } @else if (hasToken) {
          <p class="gcv-status">Confirm your email address to send your message to Gustavo.</p>
          <button type="button" class="gcv-btn gcv-btn--solid" [disabled]="isBusy()" (click)="confirm()">
            {{ isBusy() ? 'Sending…' : 'Confirm and send message' }}
          </button>
        } @else {
          <p class="gcv-status" role="status">This verification link is invalid or incomplete.</p>
        }
      </section>
    </main>
  `,
  styles: `:host { display: block; }`,
})
export class ContactVerifyPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private token = '';
  protected hasToken = false;
  protected readonly message = signal('');
  protected readonly isBusy = signal(false);

  ngOnInit(): void {
    this.token = new URLSearchParams(this.route.snapshot.fragment ?? '').get('token') ?? '';
    this.hasToken = Boolean(this.token);
  }

  protected confirm(): void {
    if (!this.hasToken || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    // Drop the token from the address bar before it is used.
    void this.router.navigate([], { relativeTo: this.route, replaceUrl: true });
    this.http.post<{ message: string }>('/api/contact/verify', { token: this.token }).subscribe({
      next: ({ message }) => this.finish(message),
      error: (error: HttpErrorResponse) =>
        this.finish(error.error?.message ?? error.error?.error ?? 'This verification link is invalid or expired.'),
    });
  }

  private finish(message: string): void {
    this.token = '';
    this.hasToken = false;
    this.message.set(message);
    this.isBusy.set(false);
  }
}
