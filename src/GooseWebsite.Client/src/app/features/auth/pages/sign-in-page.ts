import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthApi } from '../api/auth-api';
import { AuthenticatedUser } from '../models/authenticated-user';

// Keeps sign-in and registration state within the authentication feature boundary.
@Component({
  selector: 'app-sign-in-page',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-page">
      <a class="back-link" routerLink="/">Back to Gustavo's site</a>
      <section class="auth-panel" aria-labelledby="auth-title">
        <p class="eyebrow">Account</p>
        <h1 id="auth-title">Sign in</h1>
        @if (user(); as signedInUser) {
          <p class="status" role="status">Signed in as {{ signedInUser.displayName }}.</p>
          <p class="status">Access level: {{ signedInUser.isAdmin ? 'Administrator' : 'Standard account' }}</p>
          <button type="button" [disabled]="isBusy()" (click)="signOut()">Sign out</button>
        } @else if (isRegistering) {
          <p class="intro">Create your Reader account.</p>
          @if (message()) {
            <p class="status" role="status">{{ message() }}</p>
          }
          <form (ngSubmit)="register()" #registerForm="ngForm">
            <label for="display-name">Display name</label>
            <input id="display-name" name="displayName" type="text" required maxlength="100" [(ngModel)]="displayName" />
            <label for="register-email">Email</label>
            <input id="register-email" name="registerEmail" type="email" autocomplete="username" required email [(ngModel)]="email" />
            <label for="register-password">Password</label>
            <input id="register-password" name="registerPassword" type="password" autocomplete="new-password" required minlength="12" [(ngModel)]="password" />
            <button type="submit" [disabled]="!registerForm.valid || isBusy()">
              {{ isBusy() ? 'Creating account…' : 'Create account' }}
            </button>
          </form>
          @if (verificationPending) {
            <button type="button" class="secondary" [disabled]="isBusy()" (click)="resendVerification()">
              {{ isBusy() ? 'Sending…' : 'Resend confirmation email' }}
            </button>
          }
          <button type="button" class="secondary" [disabled]="isBusy()" (click)="backToSignIn()">Back to sign in</button>
        } @else {
          <p class="intro">Sign in with your account to continue.</p>
          @if (sessionNotice()) {
            <p class="status" role="status">{{ sessionNotice() }}</p>
          }
          <form (ngSubmit)="signIn()" #signInForm="ngForm">
            <label for="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              autocomplete="username"
              required
              email
              [(ngModel)]="email"
            />
            <label for="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              autocomplete="current-password"
              required
              [(ngModel)]="password"
            />
            @if (message()) {
              <p class="status" role="alert">{{ message() }}</p>
            }
            <button type="submit" [disabled]="!signInForm.valid || isBusy()">
              {{ isBusy() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>
          @if (registrationEnabled()) {
            <button type="button" class="secondary" [disabled]="isBusy()" (click)="startRegistration()">Create account</button>
          }
        }
      </section>
    </main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--paper); color: var(--ink); }
    .auth-page { box-sizing: border-box; margin: 0 auto; max-width: 760px; min-height: 100vh; padding: 40px 24px; }
    .back-link { color: var(--accent-soft); font: 12px var(--mono); }
    .auth-panel { margin: clamp(64px, 16vh, 140px) auto 0; max-width: 440px; }
    .eyebrow { color: var(--accent-soft); font: 11px var(--mono); letter-spacing: .12em; text-transform: uppercase; }
    h1 { font: 500 clamp(2rem, 7vw, 3.5rem)/1.1 var(--display); margin: 14px 0; }
    .intro, .status { color: var(--muted); line-height: 1.6; }
    form { display: grid; gap: 12px; margin-top: 28px; }
    label { color: var(--muted); font: 11px var(--mono); letter-spacing: .08em; text-transform: uppercase; }
    input { background: #eef1e8; border: 1px solid var(--line); border-radius: 0; box-sizing: border-box; color: #172622; font: 16px var(--body); min-height: 48px; padding: 10px 12px; width: 100%; }
    button { background: var(--accent); border: 0; color: #071013; cursor: pointer; font: 600 14px var(--body); margin-top: 12px; min-height: 48px; padding: 12px 20px; }
    .secondary { background: transparent; border: 1px solid var(--line); color: var(--ink); }
    button:disabled { cursor: wait; opacity: .65; }
    a:focus-visible, input:focus-visible, button:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 4px; }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; } }
  `,
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

  constructor(private readonly auth: AuthApi) {}

  ngOnInit(): void {
    this.auth.getReaderRegistrationAvailability().subscribe({
      next: ({ enabled }) => this.registrationEnabled.set(enabled),
    });
    this.auth.getCurrentUser().subscribe({
      next: (user) => this.user.set(user),
      error: (error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.sessionNotice.set('You are not currently signed in. Your session may have expired.');
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