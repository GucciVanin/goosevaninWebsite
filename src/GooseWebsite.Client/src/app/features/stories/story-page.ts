import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { pageTitle } from '../../shared/site-title-strategy';
import { storyDate, storyMoment } from '../../shared/story-date';
import { renderMarkdown } from '../editor/markdown';
import { NotFoundPage } from '../not-found/not-found-page';
import { PublicStoryApi, Story } from './public-story-api';

// F5-US2: one published story in a quiet reading column. The title comes first and the body follows; the date and the
// tags are secondary, after the text. The body is Markdown, rendered safely (raw HTML off, unsafe links refused). An
// unknown or draft slug (the API answers 404) shows the site's not-found page, and the page works on a direct load.
@Component({
  selector: 'app-story-page',
  imports: [NotFoundPage, RouterLink],
  template: `
    @if (status() === 'missing') {
      <app-not-found-page />
    } @else {
      <main class="gcv-section gcv-section--page" aria-labelledby="story-title">
        <p class="back"><a class="gcv-link" routerLink="/stories"><span aria-hidden="true">←</span> All stories</a></p>
        @if (status() === 'loading') {
          <h1 id="story-title" class="gcv-heading gcv-heading--section">Story</h1>
          <p class="notice" role="status">Loading story...</p>
        } @else if (status() === 'failed') {
          <h1 id="story-title" class="gcv-heading gcv-heading--section">Story</h1>
          <p class="notice" role="status">This story could not be loaded right now. Please try again later.</p>
        } @else if (story(); as s) {
          <article class="column">
            <h1 id="story-title" class="gcv-heading gcv-heading--section">{{ s.title }}</h1>
            @if (s.coverImage) {
              <img class="cover" [src]="s.coverImage" [alt]="s.coverImageAlt" />
            }
            <div class="body gcv-preview gcv-reading" [innerHTML]="bodyHtml()"></div>
            <div class="meta">
              <p class="gcv-label">{{ s.category }} / <time [attr.datetime]="moment(s)">{{ date(s) }}</time></p>
              @if (s.tags.length > 0) {
                <ul class="tags" aria-label="Tags">
                  @for (tag of s.tags; track tag) {
                    <li class="gcv-chip">#{{ tag }}</li>
                  }
                </ul>
              }
            </div>
          </article>
        }
      </main>
    }
  `,
  styles: `
    :host { display: block; }
    .back { margin: 0 0 var(--gcv-space-5); }
    .back a { text-decoration: underline; }
    .column { display: grid; gap: var(--gcv-space-6); max-width: 70ch; min-width: 0; }
    .column > * { min-width: 0; }
    .gcv-heading { margin: 0; overflow-wrap: anywhere; text-transform: none; }
    .cover { display: block; height: auto; max-width: 100%; }
    .body { color: var(--gcv-text); overflow-wrap: anywhere; }
    .meta { display: grid; gap: var(--gcv-space-3); }
    .meta p { margin: 0; }
    .tags { display: flex; flex-wrap: wrap; gap: var(--gcv-space-2); list-style: none; margin: 0; padding: 0; }
    .notice { background: var(--gcv-bg-1); border: 1px dashed var(--gcv-line-strong); color: var(--gcv-text-muted); font: var(--gcv-text-sm) / 1.6 var(--gcv-font-mono); letter-spacing: 0.06em; margin: 0; padding: var(--gcv-space-6) var(--gcv-space-5); }
  `,
})
export class StoryPage implements OnInit {
  private readonly api = inject(PublicStoryApi);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  protected readonly story = signal<Story | null>(null);
  protected readonly bodyHtml = signal('');
  protected readonly status = signal<'loading' | 'ready' | 'failed' | 'missing'>('loading');
  protected readonly moment = storyMoment;
  protected readonly date = (story: Story) => storyDate(storyMoment(story));

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.load(params.get('slug') ?? ''));
  }

  private load(slug: string): void {
    this.status.set('loading');
    this.api.get(slug).subscribe({
      next: (story) => {
        this.bodyHtml.set(renderMarkdown(story.content, { headingOffset: 1 }));
        this.story.set(story);
        this.title.setTitle(pageTitle(story.title));
        this.status.set('ready');
      },
      error: (error: unknown) => this.status.set(error instanceof HttpErrorResponse && error.status === 404 ? 'missing' : 'failed'),
    });
  }
}
