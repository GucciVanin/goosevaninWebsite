import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthApi } from '../api/auth-api';

// Requires an explicit user action so email link scanners cannot confirm accounts.
@Component({
  selector: 'app-verify-email-page',
  imports: [RouterLink],
  template: `
    <main class="auth-page">
      <a class="back-link" routerLink="/sign-in">Back to sign in</a>
      <section class="auth-panel" aria-labelledby="verification-title">
        <p class="eyebrow">Reader account</p>
        <h1 id="verification-title">Confirm your email</h1>
        @if (message()) {
          <p class="status" role="status">{{ message() }}</p>
        } @else if (hasVerificationRequest) {
          <p class="intro">Confirm the email address for your Reader account to finish verification.</p>
          <button type="button" [disabled]="isBusy()" (click)="confirmEmail()">
            {{ isBusy() ? 'Confirming…' : 'Confirm email address' }}
          </button>
        } @else {
          <p class="status" role="status">This verification link is invalid or incomplete.</p>
        }
      </section>
    </main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--paper); color: var(--ink); }
    .auth-page { box-sizing: border-box; margin: 0 auto; max-width: 760px; min-height: 100vh; padding: 40px 24px; }
    .back-link { color: var(--accent-soft); font: 12px var(--mono); }
    .auth-panel { margin: clamp(64px, 16vh, 140px) auto 0; max-width: 440px; }
    .eyebrow { color: var(--accent-soft); font: 11px var(--mono); text-transform: uppercase; }
    h1 { font: 500 clamp(2rem, 7vw, 3.5rem)/1.1 var(--display); margin: 14px 0; }
    .intro, .status { color: var(--muted); line-height: 1.6; }
    button { background: var(--accent); border: 0; color: #071013; cursor: pointer; font: 600 14px var(--body); margin-top: 20px; min-height: 48px; padding: 12px 20px; }
    button:disabled { cursor: wait; opacity: .65; }
    a:focus-visible, button:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 4px; }
  `,
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