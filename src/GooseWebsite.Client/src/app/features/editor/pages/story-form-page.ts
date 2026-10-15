import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, switchMap, tap } from 'rxjs';
import { ImageControls } from '../components/image-controls';
import { StoryApi } from '../api/story-api';
import { renderMarkdown } from '../markdown';
import { Story, StoryDraft } from '../models/story';

// Gustavo's story form. Saving never publishes: Publish and Return to draft are their own actions, and the status
// is always written out in words. The body is Markdown, with a safe preview.
@Component({
  selector: 'app-story-form-page',
  imports: [FormsModule, ImageControls, RouterLink],
  template: `
    <main class="story-form">
      <a class="back-link" routerLink="/editor">Back to the editor</a>
      <header>
        <p class="eyebrow">Story</p>
        <h1>{{ id() === null ? 'New story' : 'Edit story' }}</h1>
        <p class="status" role="status">Status: {{ statusText() }}</p>
        @if (message()) {
          <p class="notice" role="status">{{ message() }}</p>
        }
        @if (problems().length > 0) {
          <ul class="problems" role="alert">
            @for (problem of problems(); track problem) {
              <li>{{ problem }}</li>
            }
          </ul>
        }
      </header>

      @if (loadFailed()) {
        <p class="problems" role="alert">This story could not be opened. It may have been deleted.</p>
      } @else if (previewing()) {
        <section class="preview-area" aria-label="Preview">
          <p class="banner">{{ isPublished() ? 'Preview of the published story' : 'Draft preview: this story is not public' }}</p>
          @if (coverImage) {
            <img class="preview-cover" [src]="coverImage" [alt]="coverImageAlt" />
          }
          <h2 class="preview-title">{{ title || 'Untitled story' }}</h2>
          @if (excerpt) {
            <p class="preview-excerpt">{{ excerpt }}</p>
          }
          <div class="preview" [innerHTML]="previewHtml()"></div>
        </section>
      } @else {
        <form (ngSubmit)="saveDraft()" #storyForm="ngForm">
          <label for="story-title">Title</label>
          <input id="story-title" name="title" type="text" [(ngModel)]="title" />

          <label for="story-excerpt">Excerpt (a short summary shown in lists)</label>
          <textarea id="story-excerpt" name="excerpt" rows="3" [(ngModel)]="excerpt"></textarea>

          <label for="story-category">Category</label>
          <input id="story-category" name="category" type="text" [(ngModel)]="category" />

          <label for="story-tags">Tags (separate with commas)</label>
          <input id="story-tags" name="tags" type="text" [(ngModel)]="tagsText" />

          <label for="story-date">Publication date (optional)</label>
          <input id="story-date" name="publishedDate" type="date" [(ngModel)]="publishedDate" />

          <label for="story-content">Story text (Markdown)</label>
          <textarea id="story-content" name="content" class="body" rows="18" [(ngModel)]="content"></textarea>
        </form>
        <app-image-controls [(coverImage)]="coverImage" [(coverImageAlt)]="coverImageAlt" (insert)="addToText($event)" />
      }

      @if (!loadFailed()) {
        <div class="actions">
          <button type="button" [disabled]="isBusy()" (click)="saveDraft()">{{ isPublished() ? 'Save changes' : 'Save draft' }}</button>
          <button type="button" class="secondary" [disabled]="isBusy()" (click)="togglePreview()">
            {{ previewing() ? 'Back to editing' : 'Preview' }}
          </button>
          @if (isPublished()) {
            <button type="button" class="secondary" [disabled]="isBusy()" (click)="returnToDraft()">Return to draft</button>
          } @else {
            <button type="button" class="publish" [disabled]="isBusy()" (click)="publish()">Publish</button>
          }
        </div>
      }
    </main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--paper); color: var(--ink); }
    .story-form { box-sizing: border-box; margin: 0 auto; max-width: 860px; padding: 40px 24px 80px; }
    .back-link { color: var(--accent-soft); font: 12px var(--mono); }
    .eyebrow { color: var(--accent-soft); font: 11px var(--mono); letter-spacing: .12em; margin: 28px 0 0; text-transform: uppercase; }
    h1 { font: 500 clamp(1.8rem, 6vw, 2.8rem)/1.1 var(--display); margin: 8px 0 12px; }
    .status, .notice { color: var(--muted); font: 13px var(--mono); margin: 4px 0; }
    .problems { border: 1px solid var(--accent); color: var(--ink); margin: 12px 0; padding: 12px 12px 12px 32px; }
    form { display: grid; gap: 10px; margin-top: 20px; }
    label { color: var(--muted); font: 11px var(--mono); letter-spacing: .08em; text-transform: uppercase; }
    input, textarea { background: #eef1e8; border: 1px solid var(--line); border-radius: 0; box-sizing: border-box; color: #172622; font: 16px var(--body); padding: 10px 12px; width: 100%; }
    input { min-height: 44px; }
    textarea.body { font-family: var(--mono); font-size: 15px; line-height: 1.5; }
    .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 24px; }
    button { background: var(--accent); border: 0; color: #071013; cursor: pointer; font: 600 14px var(--body); min-height: 44px; padding: 10px 20px; }
    button.secondary { background: transparent; border: 1px solid var(--line); color: var(--ink); }
    button.publish { background: var(--accent-soft); }
    button:disabled { cursor: wait; opacity: .65; }
    .banner { border: 1px dashed var(--line); color: var(--accent-soft); font: 12px var(--mono); margin: 20px 0 8px; padding: 8px 12px; }
    .preview-title { font: 500 clamp(1.6rem, 5vw, 2.4rem)/1.15 var(--display); margin: 12px 0 6px; }
    .preview-cover { display: block; max-height: 320px; max-width: 100%; object-fit: contain; }
    .preview img { max-width: 100%; }
    .preview-excerpt { color: var(--muted); font-style: italic; line-height: 1.6; }
    .preview { line-height: 1.7; max-width: 70ch; }
    .preview :is(h1, h2, h3) { font-family: var(--display); }
    .preview a { color: var(--accent-soft); }
    a:focus-visible, input:focus-visible, textarea:focus-visible, button:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 3px; }
  `,
})
export class StoryFormPage implements OnInit {
  protected title = '';
  protected excerpt = '';
  protected content = '';
  protected category = 'Field notes';
  protected tagsText = '';
  protected coverImage = '';
  protected coverImageAlt = '';
  protected publishedDate = '';
  private savedDate = '';

  protected readonly id = signal<number | null>(null);
  protected readonly isPublished = signal(false);
  protected readonly publishedAt = signal<string | null>(null);
  protected readonly previewing = signal(false);
  protected readonly previewHtml = signal('');
  protected readonly isBusy = signal(false);
  protected readonly message = signal('');
  protected readonly problems = signal<string[]>([]);
  protected readonly loadFailed = signal(false);
  protected readonly statusText = computed(() =>
    this.isPublished() ? `Published on ${(this.publishedAt() ?? '').slice(0, 10)}` : 'Draft',
  );

  constructor(
    private readonly api: StoryApi,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const raw = this.route.snapshot.paramMap.get('id');
    if (raw === null) {
      return;
    }

    const id = Number(raw);
    if (!Number.isInteger(id)) {
      this.loadFailed.set(true);
      return;
    }

    this.api.get(id).subscribe({
      next: (story) => this.fill(story),
      error: () => this.loadFailed.set(true),
    });
  }

  protected saveDraft(): void {
    this.run(this.persist(), 'Saved.');
  }

  protected publish(): void {
    // Save first, so what is published is what is on screen; the saved story is adopted before publishing,
    // so a refusal to publish never leaves a second copy behind.
    this.run(this.persist().pipe(switchMap((saved) => this.api.publish(saved.id))), 'Published.');
  }

  protected returnToDraft(): void {
    const id = this.id();
    if (id !== null) {
      this.run(this.api.unpublish(id), 'Returned to draft.');
    }
  }

  // An image uploaded for the text goes at the end, on its own line; Gustavo moves it where it belongs.
  protected addToText(markdown: string): void {
    this.content = this.content.trimEnd() + (this.content.trim() ? '\n\n' : '') + markdown + '\n';
  }

  protected togglePreview(): void {
    if (!this.previewing()) {
      this.previewHtml.set(renderMarkdown(this.content));
    }

    this.previewing.set(!this.previewing());
  }

  private persist(): Observable<Story> {
    const draft = this.toDraft();
    const id = this.id();
    return (id === null ? this.api.create(draft) : this.api.update(id, draft)).pipe(tap((saved) => this.adopt(saved)));
  }

  private run(work: Observable<Story>, done: string): void {
    this.isBusy.set(true);
    this.problems.set([]);
    this.message.set('');
    work.subscribe({
      next: (story) => {
        this.adopt(story);
        this.message.set(done);
        this.isBusy.set(false);
      },
      error: (error: unknown) => {
        this.problems.set(this.describe(error));
        this.isBusy.set(false);
      },
    });
  }

  // Takes the saved story as the current one; a story saved for the first time moves to its own address.
  private adopt(story: Story): void {
    const wasNew = this.id() === null;
    this.fill(story);
    if (wasNew) {
      void this.router.navigate(['/editor/stories', story.id], { replaceUrl: true });
    }
  }

  private fill(story: Story): void {
    this.id.set(story.id);
    this.title = story.title;
    this.excerpt = story.excerpt;
    this.content = story.content;
    this.category = story.category;
    this.tagsText = story.tags.join(', ');
    this.coverImage = story.coverImage;
    this.coverImageAlt = story.coverImageAlt;
    this.publishedDate = story.publishedAtUtc ? story.publishedAtUtc.slice(0, 10) : '';
    this.savedDate = this.publishedDate;
    this.isPublished.set(story.isPublished);
    this.publishedAt.set(story.publishedAtUtc);
  }

  private toDraft(): StoryDraft {
    return {
      title: this.title,
      excerpt: this.excerpt,
      content: this.content,
      category: this.category,
      coverImage: this.coverImage,
      coverImageAlt: this.coverImageAlt,
      tags: this.tagsText.split(',').map((tag) => tag.trim()).filter((tag) => tag.length > 0),
      // Only a date Gustavo changed is sent, so saving a typo fix keeps the exact time of publication.
      publishedAtUtc: this.publishedDate && this.publishedDate !== this.savedDate ? `${this.publishedDate}T00:00:00Z` : null,
    };
  }

  // What the API refused, in its own words; anything else gets a plain message.
  private describe(error: unknown): string[] {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const errors = (error.error as { errors?: Record<string, string[]> } | null)?.errors;
      const messages = errors ? Object.values(errors).flat() : [];
      if (messages.length > 0) {
        return messages;
      }
    }

    return ['The story could not be saved. Try again.'];
  }
}
