import { Component, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BrandLogo } from '../../shared/brand/brand-logo';
import { AuthApi } from '../../features/auth/api/auth-api';
import { NavigationItem } from '../../shared/models/navigation-item';
import { profile } from '../../shared/models/profile';

// Keeps navigation behavior and accessibility shared across all site pages.
@Component({
  selector: 'app-site-header',
  imports: [BrandLogo, RouterLink],
  templateUrl: './site-header.html',
  styleUrl: './site-header.scss',
})
export class SiteHeader {
  protected readonly profile = profile;
  readonly activeSection = input('home');
  readonly hidden = input(false);
  readonly navItems = input.required<NavigationItem[]>();
  readonly navigate = output<string>();

  // Only a convenience: the API decides who may use the editor.
  protected readonly isAdmin = signal(false);

  constructor() {
    inject(AuthApi)
      .getCurrentUser()
      .subscribe({ next: (user) => this.isAdmin.set(user.isAdmin), error: () => this.isAdmin.set(false) });
  }

  protected onNavigate(id: string, event: Event): void {
    event.preventDefault();
    this.navigate.emit(id);
  }
}
