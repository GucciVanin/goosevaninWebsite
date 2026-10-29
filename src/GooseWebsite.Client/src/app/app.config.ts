import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TitleStrategy, provideRouter, withInMemoryScrolling, withViewTransitions } from '@angular/router';

import { pageTransitionOptions } from './shared/page-transition';
import { routes } from './app.routes';
import { SiteTitleStrategy } from './shared/site-title-strategy';
import { sessionInterceptor } from './shared/http/session.interceptor';

// Keeps root-level providers consistent across every routed page.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptors([sessionInterceptor])),
    // A new page starts at the top; a link with a fragment (or the back button) keeps its place.
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }), withViewTransitions(pageTransitionOptions)),
    { provide: TitleStrategy, useClass: SiteTitleStrategy },
  ],
};
