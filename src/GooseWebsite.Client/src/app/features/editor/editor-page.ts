import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BrandLogo } from '../../shared/brand/brand-logo';
import { AuthApi } from '../auth/api/auth-api';
import { ProjectApi } from './api/project-api';
import { StoryApi } from './api/story-api';
import { ProjectSummary } from './models/project';
import { StorySummary } from './models/story';

type StatusFilter = 'all' | 'draft' | 'published';
type Kind = 'stories' | 'projects';
type Row = StorySummary | ProjectSummary;

// Gustavo's workspace: stories or projects (a switch chooses), each with its state in words, filtered by status
// and searched by title.
@Component({
  selector: 'app-editor-page',
  imports: [BrandLogo, FormsModule, RouterLink],
  template: `
    <main class="gcv-editor">
      <header class="gcv-editor__top">
        <a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a>
        <a class="gcv-link" routerLink="/">Back to Gustavo's site</a>
      </header>

      <div class="gcv-editor__body">
        <header class="gcv-editor__group">
          <p class="gcv-eyebrow">Private</p>
          <h1 id="editor-title" class="gcv-heading gcv-heading--section">Editor</h1>
          @if (name(); as administrator) {
            <p class="gcv-status" role="status">Signed in as {{ administrator }} (Administrator).</p>
          }
          <div class="gcv-actions">
            <a class="gcv-btn gcv-btn--solid" [routerLink]="['/editor', kind(), 'new']">{{ kind() === 'stories' ? 'New story' : 'New project' }}</a>
            <a class="gcv-btn" routerLink="/sign-in">Account and sign out</a>
          </div>
        </header>

        <section class="gcv-editor__group" aria-labelledby="stories-title">
          <div class="gcv-toggle" role="group" aria-label="Content type">
            <button type="button" [attr.aria-pressed]="kind() === 'stories'" (click)="choose('stories')">Stories</button>
            <button type="button" [attr.aria-pressed]="kind() === 'projects'" (click)="choose('projects')">Projects</button>
          </div>
          <h2 id="stories-title">{{ kind() === 'stories' ? 'Stories' : 'Projects' }}</h2>

          <div class="gcv-filters">
            <div class="gcv-field">
              <label for="story-search">Search by title</label>
              <input id="story-search" type="search" [ngModel]="search()" (ngModelChange)="search.set($event)" />
            </div>
            <div class="gcv-field">
              <label for="story-status">Show</label>
              <select id="story-status" [ngModel]="filter()" (ngModelChange)="filter.set($event)">
                <option value="all">All {{ kind() }}</option>
                <option value="draft">Drafts</option>
                <option value="published">Published</option>
              </select>
            </div>
          </div>

          @if (message()) {
            <p class="gcv-status" role="status">{{ message() }}</p>
          }

          @if (loadFailed()) {
            <p class="gcv-status gcv-status--alert" role="alert">The list could not be loaded. Reload the page to try again.</p>
          } @else if (!loaded()) {
            <p class="gcv-status" role="status">Loading...</p>
          } @else if (stories().length === 0) {
            <p class="gcv-status">There are no {{ kind() }} yet. Use "{{ kind() === 'stories' ? 'New story' : 'New project' }}" to add the first one.</p>
          } @else if (visible().length === 0) {
            <p class="gcv-status">Nothing matches. Clear the search or show everything.</p>
          } @else {
            <ul class="story-list gcv-list">
              @for (story of visible(); track story.id) {
                <li class="gcv-card">
                  <div class="gcv-list__row">
                    <a class="gcv-list__title" [routerLink]="['/editor', kind(), story.id]">{{ story.title }}</a>
                    <span class="gcv-chip" [class.gcv-chip--published]="story.isPublished">State: {{ story.isPublished ? 'Published' : 'Draft' }}</span>
                    <span class="gcv-status">Updated {{ story.updatedAtUtc.slice(0, 10) }}</span>
                  </div>
                  <div class="gcv-list__actions">
                    <a class="gcv-btn" [routerLink]="['/editor', kind(), story.id]">Edit</a>
                    @if (pendingDelete() === story.id) {
                      <span class="gcv-status gcv-status--alert" role="alert">Delete "{{ story.title }}" for good?</span>
                      <button type="button" class="gcv-btn gcv-btn--danger" (click)="confirmDelete(story)">Yes, delete</button>
                      <button type="button" class="gcv-btn" (click)="pendingDelete.set(null)">Keep it</button>
                    } @else {
                      <button type="button" class="gcv-btn" (click)="pendingDelete.set(story.id)">Delete</button>
                    }
                  </div>
                </li>
              }
            </ul>
          }
        </section>
      </div>
    </main>
  `,
  styles: `
    :host { display: block; }
  `,
})
export class EditorPage implements OnInit {
  protected readonly name = signal('');
  protected readonly kind = signal<Kind>('stories');
  protected readonly stories = signal<Row[]>([]);
  protected readonly loaded = signal(false);
  protected readonly loadFailed = signal(false);
  protected readonly message = signal('');
  protected readonly search = signal('');
  protected readonly filter = signal<StatusFilter>('all');
  protected readonly pendingDelete = signal<number | null>(null);

  protected readonly visible = computed(() => {
    const term = this.search().trim().toLowerCase();
    const filter = this.filter();
    return this.stories().filter(
      (story) =>
        (filter === 'all' || (filter === 'published') === story.isPublished) && (term === '' || story.title.toLowerCase().includes(term)),
    );
  });

  constructor(
    private readonly auth: AuthApi,
    private readonly storyApi: StoryApi,
    private readonly projectApi: ProjectApi,
  ) {}

  ngOnInit(): void {
    this.auth.getCurrentUser().subscribe({ next: (user) => this.name.set(user.displayName) });
    this.load();
  }

  protected choose(kind: Kind): void {
    if (kind === this.kind()) {
      return;
    }

    this.kind.set(kind);
    this.search.set('');
    this.filter.set('all');
    this.pendingDelete.set(null);
    this.message.set('');
    this.load();
  }

  private load(): void {
    this.loaded.set(false);
    this.loadFailed.set(false);
    this.stories.set([]);
    const list = this.kind() === 'stories' ? this.storyApi.list() : this.projectApi.list();
    list.subscribe({
      next: (rows) => {
        this.stories.set(rows);
        this.loaded.set(true);
      },
      error: () => this.loadFailed.set(true),
    });
  }

  // Deleting needs the explicit second click; the item is removed only once the API says it is gone.
  protected confirmDelete(story: Row): void {
    this.message.set('');
    (this.kind() === 'stories' ? this.storyApi : this.projectApi).remove(story.id).subscribe({
      next: () => {
        this.stories.update((all) => all.filter((item) => item.id !== story.id));
        this.pendingDelete.set(null);
        this.message.set(`Deleted "${story.title}".`);
      },
      error: () => {
        this.pendingDelete.set(null);
        this.message.set(`"${story.title}" could not be deleted. Try again.`);
      },
    });
  }
}
