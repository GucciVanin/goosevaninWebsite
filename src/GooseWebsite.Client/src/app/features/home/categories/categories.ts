import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

interface Category {
  title: string;
  token: string;
  image: string;
  imageAlt: string;
  icon: 'code' | 'book' | 'trophy';
  /** The card opens its own page, with an optional query (Competitions is the stories tagged `competition`). */
  route: string;
  query?: Record<string, string>;
}

// The three category cards: Projects, Life stories and Competitions (competitions are life stories tagged "competition").
// Each card has its own accent, icon and image strip, and opens a page of its own: /projects, /stories, and
// /stories?tag=competition (a plain, shareable address; plain navigation to /stories clears the tag).
@Component({
  selector: 'app-categories',
  template: `
    <section class="categories" aria-label="Kinds of content">
      <ul>
        @for (card of cards; track card.title) {
          <li [style.--card]="'var(' + card.token + ')'">
            <a [href]="href(card)" (click)="onOpen(card, $event)">
              <span class="icon" aria-hidden="true">
                @switch (card.icon) {
                  @case ('code') {
                    <svg viewBox="0 0 32 32"><path d="M11 9 4 16l7 7M21 9l7 7-7 7M18 6l-4 20" /></svg>
                  }
                  @case ('book') {
                    <svg viewBox="0 0 32 32"><path d="M16 8c-2.5-2-6-3-11-3v18c5 0 8.5 1 11 3 2.5-2 6-3 11-3V5c-5 0-8.5 1-11 3ZM16 8v18" /></svg>
                  }
                  @case ('trophy') {
                    <svg viewBox="0 0 32 32"><path d="M10 4h12v8a6 6 0 0 1-12 0ZM10 7H5c0 4 2 6 5 6.5M22 7h5c0 4-2 6-5 6.5M16 18v5M11 28h10M13 23h6" /></svg>
                  }
                }
              </span>
              <span class="title">
                <span class="gcv-heading gcv-heading--card">{{ card.title }}</span>
                <span class="arrow" aria-hidden="true">→</span>
              </span>
              <img [src]="card.image" [alt]="card.imageAlt" loading="lazy" />
            </a>
          </li>
        }
      </ul>
    </section>
  `,
  styles: `
    :host { display: block; margin-top: calc(var(--gcv-space-7) * -1); position: relative; z-index: 1; }
    .categories { margin: 0 auto; max-width: 1500px; padding: 0 var(--gcv-gutter); }
    ul { display: grid; gap: var(--gcv-space-5); grid-template-columns: 1fr; list-style: none; margin: 0; padding: 0; }
    @media (min-width: 560px) { ul { grid-template-columns: repeat(2, 1fr); } }
    @media (min-width: 1000px) { ul { grid-template-columns: repeat(3, 1fr); } }
    li { background: rgba(5, 6, 13, 0.78); border: 1px solid var(--card); border-radius: var(--gcv-radius-card); overflow: hidden; transition: box-shadow var(--gcv-motion-base) var(--gcv-ease), transform var(--gcv-motion-base) var(--gcv-ease); }
    li:hover { box-shadow: 0 0 24px color-mix(in srgb, var(--card) 35%, transparent); transform: translateY(-3px); }
    a { color: var(--gcv-text); display: grid; gap: var(--gcv-space-3); padding: var(--gcv-space-5) var(--gcv-space-5) 0; text-decoration: none; }
    .icon svg { fill: none; height: 38px; stroke: var(--card); stroke-linecap: round; stroke-linejoin: round; stroke-width: 1.6; width: 38px; }
    .title { align-items: center; border-bottom: 1px solid color-mix(in srgb, var(--card) 45%, transparent); display: flex; justify-content: space-between; padding-bottom: var(--gcv-space-3); }
    .title .gcv-heading { font-size: var(--gcv-text-sm); letter-spacing: var(--gcv-track-label); }
    .arrow { color: var(--card); }
    img { display: block; height: 96px; margin: 0 calc(var(--gcv-space-5) * -1); max-width: none; object-fit: cover; opacity: 0.85; width: calc(100% + var(--gcv-space-5) * 2); }
  `,
})
export class Categories {
  private readonly router = inject(Router);

  protected readonly cards: Category[] = [
    { title: 'Projects', route: '/projects', token: '--gcv-magenta', image: '/brand/cards/projects.webp', imageAlt: 'Source code on a computer screen', icon: 'code' },
    { title: 'Life stories', route: '/stories', token: '--gcv-cyan', image: '/brand/cards/life-stories.webp', imageAlt: 'A neon city skyline at dusk', icon: 'book' },
    { title: 'Competitions', route: '/stories', query: { tag: 'competition' }, token: '--gcv-orange', image: '/brand/cards/competitions.webp', imageAlt: 'A glowing neon sign on a city street', icon: 'trophy' },
  ];

  protected href(card: Category): string {
    return this.router.serializeUrl(this.router.createUrlTree([card.route], { queryParams: card.query }));
  }

  protected onOpen(card: Category, event: Event): void {
    event.preventDefault();
    void this.router.navigate([card.route], { queryParams: card.query });
  }
}
