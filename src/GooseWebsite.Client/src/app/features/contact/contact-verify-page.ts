import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

// Requires an explicit user action so email link scanners cannot deliver a contact message.
@Component({
  selector: 'app-contact-verify-page',
  imports: [RouterLink],
  template: `
    <main class="auth-page">
      <a class="back-link" routerLink="/">Back to the site</a>
      <section class="auth-panel" aria-labelledby="verification-title">
        <p class="eyebrow">Contact</p>
        <h1 id="verification-title">Confirm your email</h1>
        @if (message()) {
          <p class="status" role="status">{{ message() }}</p>
        } @else if (hasToken) {
          <p class="intro">Confirm your email address to send your message to Gustavo.</p>
          <button type="button" [disabled]="isBusy()" (click)="confirm()">
            {{ isBusy() ? 'Sending…' : 'Confirm and send message' }}
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
