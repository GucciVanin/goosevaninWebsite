import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { storyDate, storyMoment } from '../../shared/story-date';
import { PublicStoryApi, Story } from './public-story-api';

// F5-US2: every published story as a compact, unframed entry (category and date, title, excerpt, tags), with the
// search and the category filter the home section used to carry. `?tag=` filters by one tag (case-insensitive, exact);
// the address is the whole filter state, so /stories?tag=competition can be shared and plain navigation clears it.
@Component({
  selector: 'app-stories-index-page',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="gcv-section gcv-section--page" aria-labelledby="stories-title">
      <p class="gcv-section__label">Stories</p>
      <div class="intro">
        <h1 id="stories-title" class="gcv-heading gcv-heading--section">Stories</h1>
        <p class="gcv-lede">Stories Gustavo has published.</p>
      </div>
      <div class="controls">
        <div class="gcv-field search">
          <label for="story-search">Search stories</label>
          <input id="story-search" type="search" [ngModel]="query()" (ngModelChange)="query.set($event)" placeholder="Search by title or tag" />
        </div>
        <div class="categories" role="group" aria-label="Filter by category">
          @for (item of categories(); track item) {
            <button type="button" class="filter" [class.active]="category() === item" [attr.aria-pressed]="category() === item" (click)="category.set(item)">{{ item }}</button>
          }
        </div>
      </div>
      @if (tag(); as t) {
        <p class="tag-filter" role="status">Showing stories tagged <b>#{{ t }}</b>. <a class="gcv-btn" routerLink="/stories">Show all stories</a></p>
      }

      @if (status() === 'loading') {
        <p class="notice" role="status">Loading stories...</p>
      } @else if (status() === 'failed') {
        <p class="notice" role="status">Stories could not be loaded right now. Please try again later.</p>
      } @else if (stories().length === 0) {
        <p class="notice">Stories will appear here as soon as Gustavo publishes them.</p>
      } @else if (shown().length === 0) {
        <p class="notice">{{ tag() ? 'No stories tagged #' + tag() + ' yet.' : 'No story found. Try another search.' }}</p>
      } @else {
        <ul class="list">
          @for (story of shown(); track story.slug) {
            <li class="entry">
              <p class="gcv-label meta">{{ story.category }} / <time [attr.datetime]="moment(story)">{{ date(story) }}</time></p>
              <h2 class="gcv-heading gcv-heading--card"><a class="gcv-link" [routerLink]="['/stories', story.slug]">{{ story.title }}</a></h2>
              @if (story.excerpt) {
                <p class="excerpt">{{ story.excerpt }}</p>
              }
              @if (story.tags.length > 0) {
                <ul class="tags" aria-label="Tags">
                  @for (item of story.tags; track item) {
                    <li class="gcv-chip">#{{ item }}</li>
                  }
                </ul>
              }
            </li>
          }
        </ul>
      }
    </main>
  `,
  styles: `
    :host { display: block; }
    .intro { display: grid; gap: var(--gcv-space-4); margin-bottom: var(--gcv-space-6); }
    .controls { align-items: end; display: flex; flex-wrap: wrap; gap: var(--gcv-space-5); justify-content: space-between; margin-bottom: var(--gcv-space-6); }
    .search { width: min(420px, 100%); }
    .categories { display: flex; flex-wrap: wrap; gap: var(--gcv-space-2); }
    .filter { background: transparent; border: 1px solid var(--gcv-line-strong); color: var(--gcv-text-muted); cursor: pointer; font: 500 var(--gcv-text-xs) / 1 var(--gcv-font-mono); letter-spacing: var(--gcv-track-label); min-height: var(--gcv-control-height); padding: var(--gcv-space-2) var(--gcv-space-4); text-transform: uppercase; transition: color var(--gcv-motion-base) var(--gcv-ease), border-color var(--gcv-motion-base) var(--gcv-ease); }
    .filter:hover { color: var(--gcv-text); }
    .filter.active { border-color: var(--gcv-cyan); color: var(--gcv-cyan); }
    .tag-filter { align-items: center; color: var(--gcv-text-muted); display: flex; flex-wrap: wrap; gap: var(--gcv-space-3); margin: 0 0 var(--gcv-space-5); }
    .tag-filter b { color: var(--gcv-text); }
    .list { display: grid; list-style: none; margin: 0; padding: 0; }
    .entry { border: 0; border-top: 1px solid var(--gcv-line); display: grid; gap: var(--gcv-space-3); padding: var(--gcv-space-5) 0; }
    .gcv-heading, .meta { margin: 0; }
    .excerpt { color: var(--gcv-text-muted); line-height: var(--gcv-leading-body); margin: 0; max-width: 70ch; overflow-wrap: anywhere; }
    .tags { display: flex; flex-wrap: wrap; gap: var(--gcv-space-2); list-style: none; margin: 0; padding: 0; }
    .notice { background: var(--gcv-bg-1); border: 1px dashed var(--gcv-line-strong); color: var(--gcv-text-muted); font: var(--gcv-text-sm) / 1.6 var(--gcv-font-mono); letter-spacing: 0.06em; margin: 0; padding: var(--gcv-space-6) var(--gcv-space-5); }
  `,
})
export class StoriesIndexPage implements OnInit {
  private readonly api = inject(PublicStoryApi);
  protected readonly stories = signal<Story[]>([]);
  protected readonly status = signal<'loading' | 'ready' | 'failed'>('loading');
  protected readonly query = signal('');
  protected readonly category = signal('All');
  protected readonly tag = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map((params) => (params.get('tag') ?? '').trim())), { initialValue: '' });
  protected readonly moment = storyMoment;
  protected readonly date = (story: Story) => storyDate(storyMoment(story));

  // "All" plus the categories present in the data, so the buttons never offer an empty category.
  protected readonly categories = computed(() => ['All', ...Array.from(new Set(this.stories().map((s) => s.category).filter((c) => !!c))).sort()]);

  protected readonly shown = computed(() => {
    const needle = this.query().toLowerCase();
    const tag = this.tag().toLowerCase();
    return this.stories().filter((story) => {
      const matchesQuery = !needle || [story.title, story.excerpt, ...story.tags].some((value) => value.toLowerCase().includes(needle));
      const matchesTag = !tag || story.tags.some((value) => value.toLowerCase() === tag);
      return matchesQuery && matchesTag && (this.category() === 'All' || story.category === this.category());
    });
  });

  ngOnInit(): void {
    this.api.list().subscribe({
      next: (stories) => {
        this.stories.set(stories);
        this.status.set('ready');
      },
      error: () => this.status.set('failed'),
    });
  }
}
