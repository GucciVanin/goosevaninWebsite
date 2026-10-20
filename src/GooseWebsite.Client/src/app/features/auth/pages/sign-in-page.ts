import { HttpErrorResponse } from '@angular/common/http';
import { BrandLogo } from '../../../shared/brand/brand-logo';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthApi } from '../api/auth-api';
import { AuthenticatedUser } from '../models/authenticated-user';
import { safeReturnUrl } from '../return-url';

// Keeps sign-in and registration state within the authentication feature boundary.
@Component({
  selector: 'app-sign-in-page',
  imports: [BrandLogo, FormsModule, RouterLink],
  template: `
    <main class="gcv-auth">
      <header class="gcv-auth__top"><a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a><a class="gcv-link" routerLink="/">Back to Gustavo's site</a></header>
      <section class="gcv-auth__panel gcv-card" aria-labelledby="auth-title">
        <p class="gcv-eyebrow">Account</p>
        <h1 id="auth-title" class="gcv-heading gcv-heading--section">Sign in</h1>
        @if (user(); as signedInUser) {
          <p class="gcv-status" role="status">Signed in as {{ signedInUser.displayName }}.</p>
          <p class="gcv-status">Access level: {{ signedInUser.isAdmin ? 'Administrator' : 'Standard account' }}</p>
          <button type="button" class="gcv-btn gcv-btn--solid" [disabled]="isBusy()" (click)="signOut()">Sign out</button>
        } @else if (isRegistering) {
          <p class="gcv-status">Create your Reader account.</p>
          @if (message()) {
            <p class="gcv-status" role="status">{{ message() }}</p>
          }
          <form (ngSubmit)="register()" #registerForm="ngForm">
            <div class="gcv-field"><label for="display-name">Display name</label>
            <input id="display-name" name="displayName" type="text" required maxlength="100" [(ngModel)]="displayName" /></div>
            <div class="gcv-field"><label for="register-email">Email</label>
            <input id="register-email" name="registerEmail" type="email" autocomplete="username" required email [(ngModel)]="email" /></div>
            <div class="gcv-field"><label for="register-password">Password</label>
            <input id="register-password" name="registerPassword" type="password" autocomplete="new-password" required minlength="12" [(ngModel)]="password" /></div>
            <button type="submit" class="gcv-btn gcv-btn--solid" [disabled]="!registerForm.valid || isBusy()">
              {{ isBusy() ? 'Creating account…' : 'Create account' }}
            </button>
          </form>
          @if (verificationPending) {
            <button type="button" class="gcv-btn" [disabled]="isBusy()" (click)="resendVerification()">
              {{ isBusy() ? 'Sending…' : 'Resend confirmation email' }}
            </button>
          }
          <button type="button" class="gcv-btn" [disabled]="isBusy()" (click)="backToSignIn()">Back to sign in</button>
        } @else {
          <p class="gcv-status">Sign in with your account to continue.</p>
          @if (sessionNotice()) {
            <p class="gcv-status" role="status">{{ sessionNotice() }}</p>
          }
          <form (ngSubmit)="signIn()" #signInForm="ngForm">
            <div class="gcv-field"><label for="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              autocomplete="username"
              required
              email
              [(ngModel)]="email"
            /></div>
            <div class="gcv-field"><label for="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              autocomplete="current-password"
              required
              [(ngModel)]="password"
            /></div>
            @if (message()) {
              <p class="gcv-status gcv-status--alert" role="alert">{{ message() }}</p>
            }
            <button type="submit" class="gcv-btn gcv-btn--solid" [disabled]="!signInForm.valid || isBusy()">
              {{ isBusy() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>
          @if (registrationEnabled()) {
            <button type="button" class="gcv-btn" [disabled]="isBusy()" (click)="startRegistration()">Create account</button>
          }
        }
      </section>
    </main>
  `,
  styles: `:host { display: block; }`,
})
export class SignInPage implements OnInit {
  protected email = '';
  protected password = '';
  protected displayName = '';
  protected isRegistering = false;
  protected verificationPending = false;
  protected readonly registrationEnabled = signal(false);
  protected readonly user = signal<AuthenticatedUser | null>(null);
  protected readonly message = signal('');
  protected readonly sessionNotice = signal('');
  protected readonly isBusy = signal(false);

  constructor(
    private readonly auth: AuthApi,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    // A request that found the session gone sends the person here; say why, and come back to where they were afterwards.
    if (this.route.snapshot.queryParamMap.get('reason') === 'session-expired') {
      this.sessionNotice.set('Your session expired. Sign in again to continue.');
    }

    this.auth.getReaderRegistrationAvailability().subscribe({
      next: ({ enabled }) => this.registrationEnabled.set(enabled),
    });
    this.auth.getCurrentUser().subscribe({
      next: (user) => this.user.set(user),
      error: (error: HttpErrorResponse) => {
        if (error.status === 401) {
          if (!this.sessionNotice()) {
            this.sessionNotice.set('You are not currently signed in. Your session may have expired.');
          }
        } else {
          this.message.set('Session status could not be checked. Try again.');
        }
      },
    });
  }

  protected startRegistration(): void {
    this.message.set('');
    this.sessionNotice.set('');
    this.isRegistering = true;
    this.verificationPending = false;
  }

  protected backToSignIn(): void {
    this.message.set('');
    this.sessionNotice.set('');
    this.isRegistering = false;
    this.verificationPending = false;
    this.displayName = '';
  }

  protected signIn(): void {
    this.message.set('');
    this.isBusy.set(true);
    this.auth.login(this.email.trim(), this.password).subscribe({
      next: (user) => {
        this.password = '';
        this.user.set(user);
        this.isBusy.set(false);
        const returnTo = safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
        if (returnTo) {
          void this.router.navigateByUrl(returnTo);
        }
      },
      error: () => {
        this.message.set('Unable to sign in with those credentials.');
        this.password = '';
        this.isBusy.set(false);
      },
    });
  }

  protected register(): void {
    this.message.set('');
    this.isBusy.set(true);
    this.auth.register(this.email.trim(), this.password, this.displayName.trim()).subscribe({
      next: ({ message }) => {
        this.message.set(message);
        this.password = '';
        this.displayName = '';
        this.verificationPending = true;
        this.isBusy.set(false);
      },
      error: () => {
        this.message.set('Please check your email to confirm your account.');
        this.password = '';
        this.isBusy.set(false);
      },
    });
  }

  protected resendVerification(): void {
    this.isBusy.set(true);
    this.auth.resendEmailVerification(this.email.trim()).subscribe({
      next: ({ message }) => {
        this.message.set(message);
        this.isBusy.set(false);
      },
      error: () => {
        this.message.set('The request could not be processed. Please try again later.');
        this.isBusy.set(false);
      },
    });
  }

  protected signOut(): void {
    this.isBusy.set(true);
    this.auth.logout().subscribe({
      next: () => {
        this.user.set(null);
        this.message.set('You have signed out.');
        this.isBusy.set(false);
      },
      error: () => {
        this.message.set('Sign out failed. Please retry.');
        this.isBusy.set(false);
      },
    });
  }
}