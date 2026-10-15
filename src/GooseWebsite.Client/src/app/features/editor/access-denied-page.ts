import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

// Shown to a signed-in account that is not an administrator and opened the editor.
@Component({
  selector: 'app-access-denied-page',
  imports: [RouterLink],
  template: `
    <main class="denied-page">
      <section class="denied-panel" aria-labelledby="denied-title">
        <p class="eyebrow">Access</p>
        <h1 id="denied-title">You don't have access to this page</h1>
        <p class="intro">The editor is only for the site's administrators.</p>
        <p class="actions">
          <a routerLink="/">Back to Gustavo's site</a>
          <a routerLink="/sign-in">Use a different account</a>
        </p>
      </section>
    </main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--paper); color: var(--ink); }
    .denied-page { box-sizing: border-box; margin: 0 auto; max-width: 760px; min-height: 100vh; padding: 40px 24px; }
    .denied-panel { margin: clamp(64px, 16vh, 140px) auto 0; max-width: 520px; }
    .eyebrow { color: var(--accent-soft); font: 11px var(--mono); letter-spacing: .12em; text-transform: uppercase; }
    h1 { font: 500 clamp(1.8rem, 6vw, 3rem)/1.15 var(--display); margin: 14px 0; }
    .intro { color: var(--muted); line-height: 1.6; }
    .actions { display: flex; flex-wrap: wrap; gap: 20px; margin-top: 24px; }
    a { color: var(--accent-soft); font: 13px var(--mono); }
    a:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 4px; }
  `,
})
export class AccessDeniedPage {}
