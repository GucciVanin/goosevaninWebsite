import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, switchMap, tap } from 'rxjs';
import { BrandLogo } from '../../../shared/brand/brand-logo';
import { ImageControls } from '../components/image-controls';
import { StoryApi } from '../api/story-api';
import { renderMarkdown } from '../markdown';
import { Story, StoryDraft } from '../models/story';

// Gustavo's story form. Saving never publishes: Publish and Return to draft are their own actions, and the status
// is always written out in words. The body is Markdown, with a safe preview.
@Component({
  selector: 'app-story-form-page',
  imports: [BrandLogo, FormsModule, ImageControls, RouterLink],
  template: `
    <main class="gcv-editor">
      <header class="gcv-editor__top">
        <a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a>
        <a class="gcv-link" routerLink="/editor">Back to the editor</a>
      </header>

      <div class="gcv-editor__body gcv-editor__body--form">
        <header class="gcv-editor__group">
          <p class="gcv-eyebrow">Story</p>
          <h1 class="gcv-heading gcv-heading--section">{{ id() === null ? 'New story' : 'Edit story' }}</h1>
          <p class="gcv-chip gcv-chip--state" [class.gcv-chip--published]="isPublished()" role="status">Status: {{ statusText() }}</p>
          @if (message()) {
            <p class="gcv-status" role="status">{{ message() }}</p>
          }
          @if (problems().length > 0) {
            <ul class="gcv-status gcv-status--alert gcv-problems" role="alert">
              @for (problem of problems(); track problem) {
                <li>{{ problem }}</li>
              }
            </ul>
          }
        </header>

        @if (loadFailed()) {
          <p class="gcv-status gcv-status--alert" role="alert">This story could not be opened. It may have been deleted.</p>
        } @else if (previewing()) {
          <section class="gcv-editor__group" aria-label="Preview">
            <p class="gcv-banner">{{ isPublished() ? 'Preview of the published story' : 'Draft preview: this story is not public' }}</p>
            @if (coverImage) {
              <img class="preview-cover" [src]="coverImage" [alt]="coverImageAlt" />
            }
            <h2>{{ title || 'Untitled story' }}</h2>
            @if (excerpt) {
              <p class="gcv-lede">{{ excerpt }}</p>
            }
            <div class="preview gcv-preview" [innerHTML]="previewHtml()"></div>
          </section>
        } @else {
          <form (ngSubmit)="saveDraft()" #storyForm="ngForm">
            <div class="gcv-field"><label for="story-title">Title</label>
            <input id="story-title" name="title" type="text" [(ngModel)]="title" /></div>

            <div class="gcv-field"><label for="story-excerpt">Excerpt (a short summary shown in lists)</label>
            <textarea id="story-excerpt" name="excerpt" rows="3" [(ngModel)]="excerpt"></textarea></div>

            <div class="gcv-field"><label for="story-category">Category</label>
            <input id="story-category" name="category" type="text" [(ngModel)]="category" /></div>

            <div class="gcv-field"><label for="story-tags">Tags (separate with commas)</label>
            <input id="story-tags" name="tags" type="text" [(ngModel)]="tagsText" /></div>

            <div class="gcv-field"><label for="story-date">Publication date (optional)</label>
            <input id="story-date" name="publishedDate" type="date" [(ngModel)]="publishedDate" /></div>

            <div class="gcv-field"><label for="story-content">Story text (Markdown)</label>
            <textarea id="story-content" name="content" class="gcv-body" rows="18" [(ngModel)]="content"></textarea></div>
          </form>
          <app-image-controls [(coverImage)]="coverImage" [(coverImageAlt)]="coverImageAlt" (insert)="addToText($event)" />
        }

        @if (!loadFailed() && !isPublished()) {
          <p class="publish-reminder gcv-status">Before you publish: read the story once more for personal or sensitive details. Publishing makes it public; saving a draft does not.</p>
        }
        @if (!loadFailed()) {
          <div class="gcv-actions">
            <button type="button" class="gcv-btn gcv-btn--solid" [disabled]="isBusy()" (click)="saveDraft()">{{ isPublished() ? 'Save changes' : 'Save draft' }}</button>
            <button type="button" class="gcv-btn" [disabled]="isBusy()" (click)="togglePreview()">
              {{ previewing() ? 'Back to editing' : 'Preview' }}
            </button>
            @if (isPublished()) {
              <button type="button" class="gcv-btn" [disabled]="isBusy()" (click)="returnToDraft()">Return to draft</button>
            } @else {
              <button type="button" class="gcv-btn" [disabled]="isBusy()" (click)="publish()">Publish</button>
            }
          </div>
        }
      </div>
    </main>
  `,
  styles: `
    :host { display: block; }
    .preview-cover { display: block; max-height: 320px; max-width: 100%; object-fit: contain; }
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
