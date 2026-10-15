import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
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
  imports: [FormsModule, RouterLink],
  template: `
    <main class="editor-page">
      <a class="back-link" routerLink="/">Back to Gustavo's site</a>
      <header>
        <p class="eyebrow">Private</p>
        <h1 id="editor-title">Editor</h1>
        @if (name(); as administrator) {
          <p class="status" role="status">Signed in as {{ administrator }} (Administrator).</p>
        }
        <div class="top-actions">
          <a class="primary" [routerLink]="['/editor', kind(), 'new']">{{ kind() === 'stories' ? 'New story' : 'New project' }}</a>
          <a class="secondary" routerLink="/sign-in">Account and sign out</a>
        </div>
      </header>

      <section aria-labelledby="stories-title">
        <div class="switch" role="group" aria-label="Content type">
          <button type="button" [attr.aria-pressed]="kind() === 'stories'" (click)="choose('stories')">Stories</button>
          <button type="button" [attr.aria-pressed]="kind() === 'projects'" (click)="choose('projects')">Projects</button>
        </div>
        <h2 id="stories-title">{{ kind() === 'stories' ? 'Stories' : 'Projects' }}</h2>

        <div class="filters">
          <label for="story-search">Search by title</label>
          <input id="story-search" type="search" [ngModel]="search()" (ngModelChange)="search.set($event)" />

          <label for="story-status">Show</label>
          <select id="story-status" [ngModel]="filter()" (ngModelChange)="filter.set($event)">
            <option value="all">All {{ kind() }}</option>
            <option value="draft">Drafts</option>
            <option value="published">Published</option>
          </select>
        </div>

        @if (message()) {
          <p class="notice" role="status">{{ message() }}</p>
        }

        @if (loadFailed()) {
          <p class="notice" role="alert">The list could not be loaded. Reload the page to try again.</p>
        } @else if (!loaded()) {
          <p class="notice" role="status">Loading...</p>
        } @else if (stories().length === 0) {
          <p class="notice">There are no {{ kind() }} yet. Use "{{ kind() === 'stories' ? 'New story' : 'New project' }}" to add the first one.</p>
        } @else if (visible().length === 0) {
          <p class="notice">Nothing matches. Clear the search or show everything.</p>
        } @else {
          <ul class="story-list">
            @for (story of visible(); track story.id) {
              <li>
                <div class="row">
                  <a class="title" [routerLink]="['/editor', kind(), story.id]">{{ story.title }}</a>
                  <span class="state" [class.published]="story.isPublished">{{ story.isPublished ? 'Published' : 'Draft' }}</span>
                  <span class="meta">Updated {{ story.updatedAtUtc.slice(0, 10) }}</span>
                </div>
                <div class="row-actions">
                  <a [routerLink]="['/editor', kind(), story.id]">Edit</a>
                  @if (pendingDelete() === story.id) {
                    <span class="confirm" role="alert">Delete "{{ story.title }}" for good?</span>
                    <button type="button" class="danger" (click)="confirmDelete(story)">Yes, delete</button>
                    <button type="button" (click)="pendingDelete.set(null)">Keep it</button>
                  } @else {
                    <button type="button" (click)="pendingDelete.set(story.id)">Delete</button>
                  }
                </div>
              </li>
            }
          </ul>
        }
      </section>
    </main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--paper); color: var(--ink); }
    .editor-page { box-sizing: border-box; margin: 0 auto; max-width: 960px; min-height: 100vh; padding: 40px 24px 80px; }
    .back-link { color: var(--accent-soft); font: 12px var(--mono); }
    .eyebrow { color: var(--accent-soft); font: 11px var(--mono); letter-spacing: .12em; margin: 28px 0 0; text-transform: uppercase; }
    h1 { font: 500 clamp(2rem, 7vw, 3.5rem)/1.1 var(--display); margin: 10px 0; }
    h2 { font: 500 1.5rem var(--display); margin: 36px 0 12px; }
    .switch { display: flex; margin-top: 28px; }
    .switch button { flex: 1; max-width: 200px; }
    .switch button[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: #071013; }
    .status, .notice, .meta { color: var(--muted); font: 13px var(--mono); line-height: 1.6; }
    .top-actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
    .primary { background: var(--accent); color: #071013; display: inline-block; font-weight: 600; padding: 12px 20px; text-decoration: none; }
    .secondary { border: 1px solid var(--line); color: var(--ink); display: inline-block; padding: 12px 20px; text-decoration: none; }
    .filters { display: grid; gap: 8px; grid-template-columns: 1fr; }
    @media (min-width: 640px) { .filters { align-items: end; grid-template-columns: 2fr 1fr; } .filters label { grid-row: 1; } .filters label[for="story-status"] { grid-column: 2; } .filters input { grid-row: 2; } .filters select { grid-column: 2; grid-row: 2; } }
    label { color: var(--muted); font: 11px var(--mono); letter-spacing: .08em; text-transform: uppercase; }
    input, select { background: #eef1e8; border: 1px solid var(--line); border-radius: 0; box-sizing: border-box; color: #172622; font: 16px var(--body); min-height: 44px; padding: 8px 12px; width: 100%; }
    .story-list { list-style: none; margin: 20px 0 0; padding: 0; }
    .story-list li { border-top: 1px solid var(--line); padding: 14px 0; }
    .row { align-items: baseline; display: flex; flex-wrap: wrap; gap: 6px 16px; }
    .title { color: var(--ink); font: 500 1.2rem var(--display); overflow-wrap: anywhere; }
    .state { border: 1px solid var(--line); font: 11px var(--mono); letter-spacing: .08em; padding: 2px 8px; text-transform: uppercase; }
    .state.published { border-color: var(--accent-soft); color: var(--accent-soft); }
    .row-actions { align-items: center; display: flex; flex-wrap: wrap; gap: 8px 16px; margin-top: 8px; }
    .row-actions a { color: var(--accent-soft); padding: 10px 0; }
    button { background: transparent; border: 1px solid var(--line); color: var(--ink); cursor: pointer; font: 600 14px var(--body); min-height: 44px; padding: 8px 16px; }
    button.danger { background: var(--accent); border-color: var(--accent); color: #071013; }
    .confirm { font-size: 14px; overflow-wrap: anywhere; }
    a:focus-visible, input:focus-visible, select:focus-visible, button:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 3px; }
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
