import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

export const siteName = 'Gustavo Couto Vanin';

// Gives every page its own document title (WCAG 2.4.2): "<page> - Gustavo Couto Vanin", or the site name alone for
// routes that declare no title (the home page, the sign-in screens). Pages whose title depends on their data
// (a project) refine it with the Title service once the data is in.
@Injectable({ providedIn: 'root' })
export class SiteTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const page = this.buildTitle(snapshot);
    this.title.setTitle(page ? pageTitle(page) : siteName);
  }
}

export function pageTitle(page: string): string {
  return `${page} - ${siteName}`;
}
