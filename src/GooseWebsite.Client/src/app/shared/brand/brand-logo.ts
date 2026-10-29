import { Component, input } from '@angular/core';

// The GCV mark. The supplied logo is a raster image (transparent PNG wrapped in an SVG), so the site serves resized
// WebP copies: 192 px wide for the header and footer, 512 px wide for large uses and high-density screens.
// Clear space around it is half its height, and it is never shown narrower than 48 px.
// `sizes` is bound before `srcset` on purpose: a static srcset is read before sizes exists, which makes the browser
// start the 512 px file (sizes defaults to 100vw) and then abort it when the real size arrives.
@Component({
  selector: 'app-brand-logo',
  template: `
    <img
      class="logo"
      src="/brand/gcv-logo-192.webp"
      [attr.sizes]="size() + 'px'"
      [attr.srcset]="'/brand/gcv-logo-192.webp 192w, /brand/gcv-logo-512.webp 512w'"
      [width]="size()"
      [height]="size() * 0.807"
      alt="GCV, the logo of Gustavo Couto Vanin"
    />
  `,
  styles: `
    :host { display: inline-block; padding: calc(var(--gcv-space-2)); }
    .logo { display: block; height: auto; max-width: 100%; min-width: 48px; }
  `,
})
export class BrandLogo {
  /** Display width in CSS pixels (not below 48). */
  readonly size = input(96);
}
