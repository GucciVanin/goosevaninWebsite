import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthApi } from '../auth/api/auth-api';

// The administrator's private editor. This is the secured shell (F4-US2); the story and project workspace
// is added by the next stories, so nothing here pretends to manage content yet.
@Component({
  selector: 'app-editor-page',
  imports: [RouterLink],
  template: `
    <main class="editor-page">
      <a class="back-link" routerLink="/">Back to Gustavo's site</a>
      <section class="editor-panel" aria-labelledby="editor-title">
        <p class="eyebrow">Private</p>
        <h1 id="editor-title">Editor</h1>
        @if (name(); as administrator) {
          <p class="status" role="status">Signed in as {{ administrator }} (Administrator).</p>
        }
        <p class="intro">
          Stories and projects will be managed here. Drafts stay private until you publish them.
        </p>
        <p class="status">Status: the content workspace has not been built yet.</p>
        <a class="secondary" routerLink="/sign-in">Account and sign out</a>
      </section>
    </main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--paper); color: var(--ink); }
    .editor-page { box-sizing: border-box; margin: 0 auto; max-width: 960px; min-height: 100vh; padding: 40px 24px; }
    .back-link { color: var(--accent-soft); font: 12px var(--mono); }
    .editor-panel { margin: clamp(48px, 12vh, 110px) 0 0; max-width: 640px; }
    .eyebrow { color: var(--accent-soft); font: 11px var(--mono); letter-spacing: .12em; text-transform: uppercase; }
    h1 { font: 500 clamp(2rem, 7vw, 3.5rem)/1.1 var(--display); margin: 14px 0; }
    .intro, .status { color: var(--muted); line-height: 1.6; }
    .secondary { border: 1px solid var(--line); color: var(--ink); display: inline-block; margin-top: 20px; padding: 12px 20px; text-decoration: none; }
    a:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 4px; }
  `,
})
export class EditorPage implements OnInit {
  protected readonly name = signal('');

  constructor(private readonly auth: AuthApi) {}

  ngOnInit(): void {
    this.auth.getCurrentUser().subscribe({ next: (user) => this.name.set(user.displayName) });
  }
}
