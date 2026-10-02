import { Component, HostListener, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { BlogPage } from './features/blog/blog-page';
import { ContactPage } from './features/contact/contact-page';
import { HomePage } from './features/home/home-page';
import { SiteFooter } from './layout/site-footer/site-footer';
import { SiteHeader } from './layout/site-header/site-header';
import { NavigationItem } from './shared/models/navigation-item';

// Composes the site shell once so shared navigation surrounds every route.
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeader, SiteFooter, HomePage, BlogPage, ContactPage],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  protected readonly activeSection = signal('home');
  protected readonly headerHidden = signal(false);
  protected readonly authRoute = signal(this.isAuthenticationRoute(this.router.url));
  private lastScrollY = 0;

  protected readonly navItems: NavigationItem[] = [
    { label: 'Home', id: 'home' },
    { label: 'Work', id: 'work' },
    { label: 'Approach', id: 'approach' },
    { label: 'Blog', id: 'blog' },
    { label: 'Contact', id: 'contact' },
  ];

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.authRoute.set(this.isAuthenticationRoute(event.urlAfterRedirects)));
  }

  private isAuthenticationRoute(url: string): boolean {
    const path = url.split(/[?#]/, 1)[0];
    return path === '/sign-in' || path === '/verify-email';
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
    this.activeSection.set(id);
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }

}
