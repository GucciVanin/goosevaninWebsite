import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NavigationItem } from '../../shared/models/navigation-item';
import { profile } from '../../shared/models/profile';

// Keeps navigation behavior and accessibility shared across all site pages.
@Component({
  selector: 'app-site-header',
  imports: [RouterLink],
  templateUrl: './site-header.html',
  styleUrl: './site-header.scss',
})
export class SiteHeader {
  protected readonly profile = profile;
  readonly activeSection = input('home');
  readonly hidden = input(false);
  readonly navItems = input.required<NavigationItem[]>();
  readonly navigate = output<string>();

  protected onNavigate(id: string, event: Event): void {
    event.preventDefault();
    this.navigate.emit(id);
  }
}
