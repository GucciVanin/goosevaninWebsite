import { Component, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { pageTitle } from '../../shared/site-title-strategy';

// The site's not-found state: any unknown URL (the wildcard route) and any project or story slug the API does not know.
// The shell supplies the header and footer around it.
@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <main class="gcv-section" aria-labelledby="not-found-title">
      <p class="gcv-section__label">404</p>
      <h1 id="not-found-title" class="gcv-heading gcv-heading--section">Page not found</h1>
      <p class="gcv-lede">There is nothing at this address. It may have moved, or it may never have been published.</p>
      <p><a class="gcv-link" routerLink="/">Back to the home page <span aria-hidden="true">→</span></a></p>
    </main>
  `,
  styles: `
    :host { display: block; }
    p { margin: 0 0 var(--gcv-space-5); }
    .gcv-link { text-decoration: underline; }
  `,
})
export class NotFoundPage {
  // Also shown inside a project page whose slug the API does not know, so it sets the title itself.
  constructor() {
    inject(Title).setTitle(pageTitle('Page not found'));
  }
}
