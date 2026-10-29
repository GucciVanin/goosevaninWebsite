import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BrandLogo } from '../../shared/brand/brand-logo';
import { SubscriptionApi } from './subscription-api';

// The page every subscription email links to. The token is in the address fragment (never sent to the server on
// load), and the subscriber is removed only when the person presses the button, so a mail scanner that opens the
// link (even one that runs scripts) cannot unsubscribe anyone. An invalid or used link gets one neutral message.
@Component({
  selector: 'app-unsubscribe-page',
  imports: [BrandLogo, RouterLink],
  template: `
    <main class="gcv-auth">
      <header class="gcv-auth__top"><a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a><a class="gcv-link" routerLink="/">Back to the site</a></header>
      <section class="gcv-auth__panel gcv-card" aria-labelledby="unsubscribe-title">
        <p class="gcv-eyebrow">Subscription</p>
        <h1 id="unsubscribe-title" class="gcv-heading gcv-heading--section">Unsubscribe</h1>
        @if (message()) {
          <p class="gcv-status" role="status">{{ message() }}</p>
        } @else if (hasToken) {
          <p class="gcv-status">Press the button to stop the emails. Your name and email address are deleted.</p>
          <button type="button" class="gcv-btn gcv-btn--solid" [disabled]="isBusy()" (click)="unsubscribe()">
            {{ isBusy() ? 'Unsubscribing…' : 'Unsubscribe' }}
          </button>
        } @else {
          <p class="gcv-status" role="status">This unsubscribe link is no longer valid.</p>
        }
      </section>
    </main>
  `,
  styles: `:host { display: block; } h1 { overflow-wrap: anywhere; }`,
})
export class UnsubscribePage implements OnInit {
  private readonly api = inject(SubscriptionApi);
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

  protected unsubscribe(): void {
    if (!this.hasToken || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    // Drop the token from the address bar before it is used.
    void this.router.navigate([], { relativeTo: this.route, replaceUrl: true });
    this.api.unsubscribe(this.token).subscribe({
      next: ({ message }) => this.finish(message),
      error: (error: HttpErrorResponse) => this.finish(error.error?.error ?? 'This unsubscribe link is no longer valid.'),
    });
  }

  private finish(message: string): void {
    this.token = '';
    this.hasToken = false;
    this.message.set(message);
    this.isBusy.set(false);
  }
}
