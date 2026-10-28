import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { plainSummary } from '../../shared/plain-summary';
import { PublicProjectApi } from './public-project-api';

interface Entry {
  title: string;
  slug: string;
  overview: string;
  technologies: string[];
  coverImage: string;
}

// F5-US1: every published project as a compact, unframed entry (cover thumbnail, title, one-line overview, technologies).
// With none published, or when loading fails, the page says so in words.
@Component({
  selector: 'app-projects-index-page',
  imports: [RouterLink],
  template: `
    <main class="gcv-section" aria-labelledby="projects-title">
      <p class="gcv-section__label">Projects</p>
      <div class="intro">
        <h1 id="projects-title" class="gcv-heading gcv-heading--section">Projects</h1>
        <p class="gcv-lede">What Gustavo has built, and the part he played in each.</p>
      </div>

      @if (status() === 'loading') {
        <p class="notice" role="status">Loading projects...</p>
      } @else if (status() === 'failed') {
        <p class="notice" role="status">The projects could not be loaded right now. Please try again later.</p>
      } @else if (entries().length === 0) {
        <p class="notice" role="status">Projects will appear here as soon as Gustavo publishes them.</p>
      } @else {
        <ul class="list">
          @for (entry of entries(); track entry.slug) {
            <li class="entry">
              @if (entry.coverImage) {
                <img class="thumb" [src]="entry.coverImage" alt="" loading="lazy" />
              }
              <div class="text">
                <h2 class="gcv-heading gcv-heading--card"><a class="gcv-link" [routerLink]="['/projects', entry.slug]">{{ entry.title }}</a></h2>
                <p class="overview">{{ entry.overview }}</p>
                @if (entry.technologies.length > 0) {
                  <ul class="stack" aria-label="Technologies">
                    @for (item of entry.technologies; track item) {
                      <li class="gcv-chip">{{ item }}</li>
                    }
                  </ul>
                }
              </div>
            </li>
          }
        </ul>
      }
    </main>
  `,
  styles: `
    :host { display: block; }
    .intro { display: grid; gap: var(--gcv-space-4); margin-bottom: var(--gcv-space-6); }
    .list { display: grid; list-style: none; margin: 0; padding: 0; }
    .entry { align-items: start; border: 0; border-top: 1px solid var(--gcv-line); display: grid; gap: var(--gcv-space-5); grid-template-columns: 1fr; padding: var(--gcv-space-5) 0; }
    .thumb { aspect-ratio: 16 / 9; background: var(--gcv-bg-1); display: block; max-width: 100%; object-fit: cover; width: 100%; }
    .text { display: grid; gap: var(--gcv-space-3); min-width: 0; }
    .gcv-heading { margin: 0; }
    .overview { color: var(--gcv-text-muted); line-height: var(--gcv-leading-body); margin: 0; overflow-wrap: anywhere; }
    .stack { display: flex; flex-wrap: wrap; gap: var(--gcv-space-2); list-style: none; margin: 0; padding: 0; }
    .notice { background: var(--gcv-bg-1); border: 1px dashed var(--gcv-line-strong); color: var(--gcv-text-muted); font: var(--gcv-text-sm) / 1.6 var(--gcv-font-mono); letter-spacing: 0.06em; margin: 0; padding: var(--gcv-space-6) var(--gcv-space-5); }
    @media (min-width: 640px) { .entry:has(.thumb) { grid-template-columns: 200px 1fr; } }
  `,
})
export class ProjectsIndexPage implements OnInit {
  private readonly api = inject(PublicProjectApi);
  protected readonly entries = signal<Entry[]>([]);
  protected readonly status = signal<'loading' | 'ready' | 'failed'>('loading');

  ngOnInit(): void {
    this.api.list().subscribe({
      next: (projects) => {
        this.entries.set(projects.map((p) => ({ title: p.title, slug: p.slug, overview: plainSummary(p.overview, 140), technologies: p.technologies, coverImage: p.coverImage })));
        this.status.set('ready');
      },
      error: () => this.status.set('failed'),
    });
  }
}
