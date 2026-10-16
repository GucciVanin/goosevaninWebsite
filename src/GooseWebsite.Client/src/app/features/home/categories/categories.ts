import { Component, output } from '@angular/core';

interface Category {
  title: string;
  target: string;
  token: string;
  image: string;
  imageAlt: string;
  icon: 'code' | 'cloud' | 'shield' | 'gear';
}

// The four category cards from the landing-page mockup, each with its own accent, icon and image strip. Each card
// takes the visitor to the part of the page about that kind of work.
@Component({
  selector: 'app-categories',
  template: `
    <section class="categories" aria-label="Areas of work">
      <ul>
        @for (card of cards; track card.title) {
          <li [style.--card]="'var(' + card.token + ')'">
            <a [href]="'#' + card.target" (click)="onOpen(card.target, $event)">
              <span class="icon" aria-hidden="true">
                @switch (card.icon) {
                  @case ('code') {
                    <svg viewBox="0 0 32 32"><path d="M11 9 4 16l7 7M21 9l7 7-7 7M18 6l-4 20" /></svg>
                  }
                  @case ('cloud') {
                    <svg viewBox="0 0 32 32"><path d="M9 24a6 6 0 0 1-.8-11.9A8 8 0 0 1 23.6 11 6.5 6.5 0 0 1 23 24Z" /></svg>
                  }
                  @case ('shield') {
                    <svg viewBox="0 0 32 32"><path d="M16 3 5 7v8c0 7 4.6 11.6 11 14 6.4-2.4 11-7 11-14V7Z" /><path d="M13 15h6v6h-6zM14.5 15v-2a1.5 1.5 0 0 1 3 0v2" /></svg>
                  }
                  @case ('gear') {
                    <svg viewBox="0 0 32 32"><circle cx="16" cy="16" r="4.5" /><path d="m16 3 2 3.4 3.9-.9.9 3.9L26.2 11 24.5 14.6l1.7 3.6-3.4 2 -.9 3.9-3.9-.9L16 27l-2-3.4-3.9.9-.9-3.9L5.8 21l1.7-3.6L5.8 13.8l3.4-2 .9-3.9 3.9.9Z" /></svg>
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
    @media (min-width: 1000px) { ul { grid-template-columns: repeat(4, 1fr); } }
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
  readonly navigate = output<string>();

  protected readonly cards: Category[] = [
    { title: 'Applications', target: 'work', token: '--gcv-magenta', image: '/brand/cards/applications.webp', imageAlt: 'Source code on a screen', icon: 'code' },
    { title: 'Cloud & DevOps', target: 'skills', token: '--gcv-cyan', image: '/brand/cards/cloud.webp', imageAlt: 'A glowing server room', icon: 'cloud' },
    { title: 'Security', target: 'skills', token: '--gcv-green', image: '/brand/cards/security.webp', imageAlt: 'A padlock made of light', icon: 'shield' },
    { title: 'Systems & Tools', target: 'skills', token: '--gcv-orange', image: '/brand/cards/systems.webp', imageAlt: 'A network of glowing circuit lines', icon: 'gear' },
  ];

  protected onOpen(id: string, event: Event): void {
    event.preventDefault();
    this.navigate.emit(id);
  }
}
