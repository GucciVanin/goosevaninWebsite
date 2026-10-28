import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, TitleStrategy, provideRouter } from '@angular/router';
import { SiteTitleStrategy } from './shared/site-title-strategy';
import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app';
import { appConfig } from './app.config';
import { routes } from './app.routes';

// The shell: the one-page home keeps its sections; the standalone public pages and the not-found page sit between
// the same header and footer; the sign-in and editor screens have neither.
describe('App shell', () => {
  async function open(url: string) {
    TestBed.configureTestingModule({ providers: [provideRouter(routes), { provide: TitleStrategy, useClass: SiteTitleStrategy }, provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl(url);
    await fixture.whenStable();
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    return { fixture, http, element: fixture.nativeElement as HTMLElement };
  }

  it('shows the header, the projects page and the footer, and none of the home sections, on /projects', async () => {
    const { element } = await open('/projects');

    expect(element.querySelector('app-site-header')).not.toBeNull();
    expect(element.querySelector('app-site-footer')).not.toBeNull();
    expect(element.querySelector('app-projects-index-page')).not.toBeNull();
    expect(element.querySelector('app-home-page')).toBeNull();
    expect(element.querySelectorAll('main').length).toBe(1);
  });

  it('shows the header and footer around the not-found page on an unknown URL', async () => {
    const { element } = await open('/nowhere');

    expect(element.querySelector('app-site-header')).not.toBeNull();
    expect(element.querySelector('app-site-footer')).not.toBeNull();
    expect(element.querySelector('app-not-found-page')).not.toBeNull();
    expect(element.querySelector('app-home-page')).toBeNull();
  });

  it('keeps the one-page home with its sections on /', async () => {
    const { element } = await open('/');

    expect(element.querySelector('app-home-page')).not.toBeNull();
    expect(element.querySelector('app-contact-page')).not.toBeNull();
    expect(element.querySelector('app-projects-index-page')).toBeNull();
  });

  it('keeps the sign-in screen without the site header and footer', async () => {
    const { element } = await open('/sign-in');

    expect(element.querySelector('app-site-header')).toBeNull();
    expect(element.querySelector('app-site-footer')).toBeNull();
  });

  it('keeps the contact verification, e-mail verification and access-denied screens without the site header and footer', async () => {
    for (const url of ['/contact/verify', '/verify-email', '/access-denied']) {
      TestBed.resetTestingModule();
      const { element } = await open(url);

      expect(element.querySelector('app-site-header')).withContext(url).toBeNull();
      expect(element.querySelector('app-site-footer')).withContext(url).toBeNull();
      expect(element.querySelector('app-home-page')).withContext(url).toBeNull();
    }
  });

  it('keeps the editor without the site header and footer', async () => {
    // The sign-in marker makes the guard ask the API who is signed in.
    document.cookie = 'goosewebsite.signedin=1; path=/';
    TestBed.configureTestingModule({ providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    let done = false;
    const navigation = TestBed.inject(Router).navigateByUrl('/editor').then(() => (done = true));
    // The header on the home page asks who is signed in too, and so does the guard; answer every ask until the navigation ends.
    for (let waited = 0; !done && waited < 3000; waited += 25) {
      http.match('/api/auth/me').forEach((request) => request.flush({ displayName: 'Site administrator', isAdmin: true }));
      await new Promise((resolve) => setTimeout(resolve, 25));
    }
    await navigation;
    await fixture.whenStable();
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(TestBed.inject(Router).url).toBe('/editor');
    expect(element.querySelector('app-site-header')).toBeNull();
    expect(element.querySelector('app-site-footer')).toBeNull();
    document.cookie = 'goosewebsite.signedin=; path=/; max-age=0';
  });

  // The header and footer links from a routed page go home first, then to the section (the F5 reviewer's advisory).
  it('takes a header link on /projects back to the home page, with its sections, and scrolls to the target', async () => {
    const { fixture, http, element } = await open('/projects');
    http.match('/api/projects').forEach((request) => request.flush([]));
    const scrollIntoView = spyOn(Element.prototype, 'scrollIntoView');
    const link = Array.from(element.querySelectorAll<HTMLAnchorElement>('app-site-header .gcv-nav a')).find((a) => a.textContent!.trim() === 'Stories')!;

    link.click();
    await fixture.whenStable();
    fixture.detectChanges();
    TestBed.inject(ApplicationRef).tick();
    await fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/');
    expect(element.querySelector('app-home-page')).not.toBeNull();
    expect(element.querySelector('app-projects-index-page')).toBeNull();
    expect(scrollIntoView).toHaveBeenCalled();
    expect((scrollIntoView.calls.mostRecent().object as HTMLElement).id).toBe('blog');
  });

  it('sends the Projects item of the header to /projects from the home page', async () => {
    const { fixture, http, element } = await open('/');
    http.match(() => true);
    const link = Array.from(element.querySelectorAll<HTMLAnchorElement>('app-site-header .gcv-nav a')).find((a) => a.textContent!.trim() === 'Projects')!;

    expect(link.getAttribute('href')).toBe('/projects');
    link.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(Router).url).toBe('/projects');
    expect(element.querySelector('app-projects-index-page')).not.toBeNull();
  });
});

// A route change starts at the top of the new page, not where the previous page was scrolled to (the F5 reviewer's defect).
// The router's scroll handling starts with a bootstrapped application, so this test bootstraps the real configuration.
describe('App routing configuration', () => {
  it('scrolls to the top when a visitor navigates from a scrolled page to another page', async () => {
    const spacer = document.createElement('div');
    spacer.style.height = '4000px';
    const host = document.createElement('app-root');
    document.body.append(spacer, host);
    const application = await bootstrapApplication(App, { providers: [...appConfig.providers, provideHttpClientTesting()] });
    try {
      const router = application.injector.get(Router);
      await router.navigateByUrl('/nowhere');
      window.scrollTo({ top: 800, behavior: 'instant' });
      expect(window.scrollY).toBeGreaterThan(0);

      await router.navigateByUrl('/projects');
      // The page's CSS may scroll smoothly, so give the scroll a moment to arrive.
      for (let waited = 0; window.scrollY > 0 && waited < 2000; waited += 50) await new Promise((resolve) => setTimeout(resolve, 50));

      expect(window.scrollY).toBe(0);
    } finally {
      application.destroy();
      spacer.remove();
      host.remove();
    }
  });
});
