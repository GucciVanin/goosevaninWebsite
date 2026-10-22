import { BrandLogo } from '../../../shared/brand/brand-logo';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthApi } from '../api/auth-api';

// Requires an explicit user action so email link scanners cannot confirm accounts.
@Component({
  selector: 'app-verify-email-page',
  imports: [BrandLogo, RouterLink],
  template: `
    <main class="gcv-auth">
      <header class="gcv-auth__top"><a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a><a class="gcv-link" routerLink="/sign-in">Back to sign in</a></header>
      <section class="gcv-auth__panel gcv-card" aria-labelledby="verification-title">
        <p class="gcv-eyebrow">Reader account</p>
        <h1 id="verification-title" class="gcv-heading gcv-heading--section">Confirm your email</h1>
        @if (message()) {
          <p class="gcv-status" role="status">{{ message() }}</p>
        } @else if (hasVerificationRequest) {
          <p class="gcv-status">Confirm the email address for your Reader account to finish verification.</p>
          <button type="button" class="gcv-btn gcv-btn--solid" [disabled]="isBusy()" (click)="confirmEmail()">
            {{ isBusy() ? 'Confirming…' : 'Confirm email address' }}
          </button>
        } @else {
          <p class="gcv-status" role="status">This verification link is invalid or incomplete.</p>
        }
      </section>
    </main>
  `,
  styles: `:host { display: block; }`,
})
export class VerifyEmailPage implements OnInit {
  protected userId = '';
  protected token = '';
  protected hasVerificationRequest = false;
  protected readonly message = signal('');
  protected readonly isBusy = signal(false);

  constructor(
    private readonly auth: AuthApi,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    const fragmentParameters = new URLSearchParams(this.route.snapshot.fragment ?? '');
    this.userId = fragmentParameters.get('userId') ?? '';
    this.token = fragmentParameters.get('token') ?? '';
    this.hasVerificationRequest = Boolean(this.userId && this.token);
  }

  protected confirmEmail(): void {
    if (!this.hasVerificationRequest || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true,
    });
    this.auth.verifyEmail(this.userId, this.token).subscribe({
      next: ({ message }) => {
        this.token = '';
        this.userId = '';
        this.hasVerificationRequest = false;
        this.message.set(message);
        this.isBusy.set(false);
      },
      error: () => {
        this.token = '';
        this.userId = '';
        this.hasVerificationRequest = false;
        this.message.set('This verification link is invalid or expired.');
        this.isBusy.set(false);
      },
    });
  }
}