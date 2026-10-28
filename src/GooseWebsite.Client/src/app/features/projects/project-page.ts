import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { pageTitle } from '../../shared/site-title-strategy';
import { renderMarkdown } from '../editor/markdown';
import { NotFoundPage } from '../not-found/not-found-page';
import { PublicProject, PublicProjectApi } from './public-project-api';

// F5-US1: one published project in a readable editorial column. Order: title, overview, Gustavo's contribution,
// technologies, cover image, then the demo and source links. An unknown slug (the API answers 404) shows the site's
// not-found page; the overview is Markdown, rendered safely (this page is its own lazy chunk, so the renderer is only fetched with it), and the page works on a direct load.
@Component({
  selector: 'app-project-page',
  imports: [NotFoundPage, RouterLink],
  template: `
    @if (status() === 'missing') {
      <app-not-found-page />
    } @else {
      <main class="gcv-section gcv-section--page" aria-labelledby="project-title">
        <p class="back"><a class="gcv-link" routerLink="/projects"><span aria-hidden="true">←</span> All projects</a></p>
        @if (status() === 'loading') {
          <h1 id="project-title" class="gcv-heading gcv-heading--section">Project</h1>
          <p class="notice" role="status">Loading project...</p>
        } @else if (status() === 'failed') {
          <h1 id="project-title" class="gcv-heading gcv-heading--section">Project</h1>
          <p class="notice" role="status">This project could not be loaded right now. Please try again later.</p>
        } @else if (project(); as p) {
          <article class="column">
            <h1 id="project-title" class="gcv-heading gcv-heading--section">{{ p.title }}</h1>
            <div class="overview gcv-preview" [innerHTML]="overviewHtml()"></div>
            @if (p.contribution) {
              <section class="contribution" aria-labelledby="contribution-title">
                <h2 id="contribution-title" class="gcv-heading gcv-heading--card">Gustavo's contribution</h2>
                <p>{{ p.contribution }}</p>
              </section>
            }
            @if (p.technologies.length > 0) {
              <section class="technologies" aria-labelledby="technologies-title">
                <h2 id="technologies-title" class="gcv-heading gcv-heading--card">Technologies</h2>
                <ul class="stack">
                  @for (item of p.technologies; track item) {
                    <li class="gcv-chip">{{ item }}</li>
                  }
                </ul>
              </section>
            }
            @if (p.coverImage) {
              <img class="cover" [src]="p.coverImage" [alt]="p.coverImageAlt" />
            }
            @if (p.demoUrl || p.sourceUrl) {
              <p class="links">
                @if (p.demoUrl) {
                  <a class="gcv-link" [href]="p.demoUrl" target="_blank" rel="noopener noreferrer">Demo <span class="sr-only">(opens in a new tab)</span><span aria-hidden="true">↗</span></a>
                }
                @if (p.sourceUrl) {
                  <a class="gcv-link" [href]="p.sourceUrl" target="_blank" rel="noopener noreferrer">Source code <span class="sr-only">(opens in a new tab)</span><span aria-hidden="true">↗</span></a>
                }
              </p>
            }
          </article>
        }
      </main>
    }
  `,
  styles: `
    :host { display: block; }
    .back { margin: 0 0 var(--gcv-space-5); }
    .column { display: grid; gap: var(--gcv-space-6); max-width: 70ch; min-width: 0; }
    .column > * { min-width: 0; }
    .gcv-heading { margin: 0; overflow-wrap: anywhere; text-transform: none; }
    .overview, .contribution p { color: var(--gcv-text-muted); line-height: var(--gcv-leading-body); margin: 0; overflow-wrap: anywhere; }
    .contribution, .technologies { display: grid; gap: var(--gcv-space-3); }
    .stack { display: flex; flex-wrap: wrap; gap: var(--gcv-space-2); list-style: none; margin: 0; padding: 0; }
    .cover { display: block; height: auto; max-width: 100%; }
    .links { display: flex; flex-wrap: wrap; gap: var(--gcv-space-3) var(--gcv-space-6); margin: 0; }
    .links a, .back a { text-decoration: underline; }
    .notice { background: var(--gcv-bg-1); border: 1px dashed var(--gcv-line-strong); color: var(--gcv-text-muted); font: var(--gcv-text-sm) / 1.6 var(--gcv-font-mono); letter-spacing: 0.06em; margin: 0; padding: var(--gcv-space-6) var(--gcv-space-5); }
    .sr-only { clip: rect(0 0 0 0); height: 1px; margin: -1px; overflow: hidden; position: absolute; white-space: nowrap; width: 1px; }
  `,
})
export class ProjectPage implements OnInit {
  private readonly api = inject(PublicProjectApi);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  protected readonly project = signal<PublicProject | null>(null);
  protected readonly overviewHtml = signal('');
  protected readonly status = signal<'loading' | 'ready' | 'failed' | 'missing'>('loading');

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.load(params.get('slug') ?? ''));
  }

  private load(slug: string): void {
    this.status.set('loading');
    this.api.get(slug).subscribe({
      next: (project) => {
        this.overviewHtml.set(renderMarkdown(project.overview, { headingOffset: 1 }));
        this.project.set(project);
        this.title.setTitle(pageTitle(project.title));
        this.status.set('ready');
      },
      error: (error: unknown) => this.status.set(error instanceof HttpErrorResponse && error.status === 404 ? 'missing' : 'failed'),
    });
  }
}
