import { Component, output } from '@angular/core';
import { BrandLogo } from '../../shared/brand/brand-logo';
import { profile } from '../../shared/models/profile';

// Keeps shared footer content consistent without duplicating it across pages.
@Component({
  selector: 'app-site-footer',
  imports: [BrandLogo],
  templateUrl: './site-footer.html',
  styleUrl: './site-footer.scss',
})
export class SiteFooter {
  protected readonly profile = profile;
  protected readonly year = new Date().getFullYear();
  readonly navigate = output<string>();

  protected onNavigate(id: string, event: Event): void {
    event.preventDefault();
    this.navigate.emit(id);
  }
}
