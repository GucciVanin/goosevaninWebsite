import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BrandLogo } from '../../shared/brand/brand-logo';

// Shown to a signed-in account that is not an administrator and opened the editor.
@Component({
  selector: 'app-access-denied-page',
  imports: [BrandLogo, RouterLink],
  template: `
    <main class="gcv-auth">
      <header class="gcv-auth__top">
        <a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a>
        <a class="gcv-link" routerLink="/">Back to Gustavo's site</a>
      </header>
      <section class="gcv-auth__panel gcv-card" aria-labelledby="denied-title">
        <p class="gcv-eyebrow">Access</p>
        <h1 id="denied-title" class="gcv-heading gcv-heading--section">You don't have access to this page</h1>
        <p class="gcv-status">The editor is only for the site's administrators.</p>
        <a class="gcv-btn" routerLink="/sign-in">Use a different account</a>
      </section>
    </main>
  `,
  styles: `:host { display: block; }`,
})
export class AccessDeniedPage {}
