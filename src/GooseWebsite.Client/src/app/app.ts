import { Component, HostListener, Injector, afterNextRender, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { BlogPage } from './features/blog/blog-page';
import { ContactPage } from './features/contact/contact-page';
import { HomePage } from './features/home/home-page';
import { SiteFooter } from './layout/site-footer/site-footer';
import { SiteHeader } from './layout/site-header/site-header';
import { NavigationItem } from './shared/models/navigation-item';
import { PageState } from './shared/page-state';
import { SubscribeSection } from './features/subscriptions/subscribe-section';

// Composes the site shell once so shared navigation surrounds every route.
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeader, SiteFooter, HomePage, BlogPage, SubscribeSection, ContactPage],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  private readonly pageState = inject(PageState);
  private readonly section = signal('home');
  // No section is current while the not-found page shows (an unknown project or story address).
  protected readonly activeSection = computed(() => (this.pageState.notFound() ? '' : this.section()));
  protected readonly headerHidden = signal(false);
  private readonly injector = inject(Injector);
  protected readonly authRoute = signal(this.isAuthenticationRoute(this.router.url));
  // The one-page home renders its sections inside the shell; every other public URL renders a routed page there.
  protected readonly homeRoute = signal(this.isHomeRoute(this.router.url));
  private lastScrollY = 0;

  protected readonly navItems: NavigationItem[] = [
    { label: 'Home', id: 'home' },
    { label: 'Projects', id: 'work', route: '/projects' },
    { label: 'About', id: 'approach' },
    { label: 'Stories', id: 'blog', route: '/stories' },
  ];

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.authRoute.set(this.isAuthenticationRoute(event.urlAfterRedirects));
        this.homeRoute.set(this.isHomeRoute(event.urlAfterRedirects));
        if (this.isPageRoute(event.urlAfterRedirects, '/projects')) this.section.set('work');
        if (this.isPageRoute(event.urlAfterRedirects, '/stories')) this.section.set('blog');
      });
  }

  private isHomeRoute(url: string): boolean {
    const path = url.split(/[?#]/, 1)[0];
    return path === '/' || path === '';
  }

  private isPageRoute(url: string, base: string): boolean {
    const path = url.split(/[?#]/, 1)[0];
    return path === base || path.startsWith(base + '/');
  }

  private isAuthenticationRoute(url: string): boolean {
    const path = url.split(/[?#]/, 1)[0];
    return ['/sign-in', '/verify-email', '/contact/verify', '/subscribe/confirm', '/unsubscribe', '/access-denied'].includes(path) || path === '/editor' || path.startsWith('/editor/');
  }

  @HostListener('window:scroll', [])
  protected onWindowScroll(): void {
    const scrollY = window.scrollY;
    const scrollingDown = scrollY > this.lastScrollY;
    const scrollingUp = scrollY < this.lastScrollY;

    if (scrollY > 90 && scrollingDown) this.headerHidden.set(true);
    if (scrollingUp || scrollY <= 24) this.headerHidden.set(false);
    this.lastScrollY = scrollY;
  }

  protected scrollTo(id: string): void {
    this.section.set(id);
    if (!this.homeRoute()) {
      // From a standalone page, the header and footer links go to the home page first, then to the section.
      this.router.navigateByUrl('/').then(() => afterNextRender(() => document.getElementById(id)?.scrollIntoView(), { injector: this.injector }));
      return;
    }
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }

}
