import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, switchMap, tap } from 'rxjs';
import { BrandLogo } from '../../../shared/brand/brand-logo';
import { ImageControls } from '../components/image-controls';
import { ProjectApi } from '../api/project-api';
import { renderMarkdown } from '../markdown';
import { Project, ProjectDraft } from '../models/project';

// Gustavo's project form. Like the story form, saving never publishes: Publish and Return to draft are their own
// actions and the status is always written out in words. The overview is Markdown, with a safe preview.
@Component({
  selector: 'app-project-form-page',
  imports: [BrandLogo, FormsModule, ImageControls, RouterLink],
  template: `
    <main class="gcv-editor">
      <header class="gcv-editor__top">
        <a routerLink="/" aria-label="Gustavo Couto Vanin home"><app-brand-logo [size]="72" /></a>
        <a class="gcv-link" routerLink="/editor">Back to the editor</a>
      </header>

      <div class="gcv-editor__body gcv-editor__body--form">
        <header class="gcv-editor__group">
          <p class="gcv-eyebrow">Project</p>
          <h1 class="gcv-heading gcv-heading--section">{{ id() === null ? 'New project' : 'Edit project' }}</h1>
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
          <p class="gcv-status gcv-status--alert" role="alert">This project could not be opened. It may have been deleted.</p>
        } @else if (previewing()) {
          <section class="gcv-editor__group" aria-label="Preview">
            <p class="gcv-banner">{{ isPublished() ? 'Preview of the published project' : 'Draft preview: this project is not public' }}</p>
            @if (coverImage) {
              <img class="preview-cover" [src]="coverImage" [alt]="coverImageAlt" />
            }
            <h2>{{ title || 'Untitled project' }}</h2>
            <div class="preview gcv-preview" [innerHTML]="previewHtml()"></div>
            @if (contribution) {
              <h3>My contribution</h3>
              <p>{{ contribution }}</p>
            }
            @if (technologyList().length > 0) {
              <h3>Technologies</h3>
              <ul class="tech gcv-tech">
                @for (item of technologyList(); track item) {
                  <li class="gcv-chip">{{ item }}</li>
                }
              </ul>
            }
            @if (demoUrl) {
              <p><a class="gcv-link" [href]="demoUrl" rel="noopener noreferrer" target="_blank">Demo</a></p>
            }
            @if (sourceUrl) {
              <p><a class="gcv-link" [href]="sourceUrl" rel="noopener noreferrer" target="_blank">Source code</a></p>
            }
          </section>
        } @else {
          <form (ngSubmit)="saveDraft()">
            <div class="gcv-field"><label for="project-title">Title</label>
            <input id="project-title" name="title" type="text" [(ngModel)]="title" /></div>

            <div class="gcv-field"><label for="project-overview">Overview (Markdown)</label>
            <textarea id="project-overview" name="overview" class="gcv-body" rows="12" [(ngModel)]="overview"></textarea></div>

            <div class="gcv-field"><label for="project-contribution">My contribution</label>
            <textarea id="project-contribution" name="contribution" rows="4" [(ngModel)]="contribution"></textarea></div>

            <div class="gcv-field"><label for="project-technologies">Technologies (separate with commas)</label>
            <input id="project-technologies" name="technologies" type="text" [(ngModel)]="technologiesText" /></div>

            <div class="gcv-field"><label for="project-demo">Demo link (optional, starts with https://)</label>
            <input id="project-demo" name="demoUrl" type="url" [(ngModel)]="demoUrl" /></div>

            <div class="gcv-field"><label for="project-source">Source link (optional, starts with https://)</label>
            <input id="project-source" name="sourceUrl" type="url" [(ngModel)]="sourceUrl" /></div>

            <div class="gcv-field"><label for="project-date">Publication date (optional)</label>
            <input id="project-date" name="publishedDate" type="date" [(ngModel)]="publishedDate" /></div>
          </form>
          <app-image-controls [(coverImage)]="coverImage" [(coverImageAlt)]="coverImageAlt" (insert)="addToText($event)" />
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
    .tech, .gcv-tech { margin: 0; }
  `,
})
export class ProjectFormPage implements OnInit {
  protected title = '';
  protected overview = '';
  protected contribution = '';
  protected technologiesText = '';
  protected demoUrl = '';
  protected sourceUrl = '';
  protected coverImage = '';
  protected coverImageAlt = '';
  protected publishedDate = '';
  private savedDate = '';

  protected readonly id = signal<number | null>(null);
  protected readonly isPublished = signal(false);
  protected readonly publishedAt = signal<string | null>(null);
  protected readonly previewing = signal(false);
  protected readonly previewHtml = signal('');
  protected readonly technologyList = signal<string[]>([]);
  protected readonly isBusy = signal(false);
  protected readonly message = signal('');
  protected readonly problems = signal<string[]>([]);
  protected readonly loadFailed = signal(false);
  protected readonly statusText = computed(() =>
    this.isPublished() ? `Published on ${(this.publishedAt() ?? '').slice(0, 10)}` : 'Draft',
  );

  constructor(
    private readonly api: ProjectApi,
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
      next: (project) => this.fill(project),
      error: () => this.loadFailed.set(true),
    });
  }

  protected saveDraft(): void {
    this.run(this.persist(), 'Saved.');
  }

  protected publish(): void {
    // Save first, so what is published is what is on screen; the saved project is adopted before publishing.
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
    this.overview = this.overview.trimEnd() + (this.overview.trim() ? '\n\n' : '') + markdown + '\n';
  }

  protected togglePreview(): void {
    if (!this.previewing()) {
      this.previewHtml.set(renderMarkdown(this.overview));
      this.technologyList.set(this.splitTechnologies());
    }

    this.previewing.set(!this.previewing());
  }

  private persist(): Observable<Project> {
    const draft = this.toDraft();
    const id = this.id();
    return (id === null ? this.api.create(draft) : this.api.update(id, draft)).pipe(tap((saved) => this.adopt(saved)));
  }

  private run(work: Observable<Project>, done: string): void {
    this.isBusy.set(true);
    this.problems.set([]);
    this.message.set('');
    work.subscribe({
      next: (project) => {
        this.adopt(project);
        this.message.set(done);
        this.isBusy.set(false);
      },
      error: (error: unknown) => {
        this.problems.set(this.describe(error));
        this.isBusy.set(false);
      },
    });
  }

  // Takes the saved project as the current one; a project saved for the first time moves to its own address.
  private adopt(project: Project): void {
    const wasNew = this.id() === null;
    this.fill(project);
    if (wasNew) {
      void this.router.navigate(['/editor/projects', project.id], { replaceUrl: true });
    }
  }

  private fill(project: Project): void {
    this.id.set(project.id);
    this.title = project.title;
    this.overview = project.overview;
    this.contribution = project.contribution;
    this.technologiesText = project.technologies.join(', ');
    this.demoUrl = project.demoUrl ?? '';
    this.sourceUrl = project.sourceUrl ?? '';
    this.coverImage = project.coverImage;
    this.coverImageAlt = project.coverImageAlt;
    this.publishedDate = project.publishedAtUtc ? project.publishedAtUtc.slice(0, 10) : '';
    this.savedDate = this.publishedDate;
    this.isPublished.set(project.isPublished);
    this.publishedAt.set(project.publishedAtUtc);
  }

  private splitTechnologies(): string[] {
    return this.technologiesText.split(',').map((item) => item.trim()).filter((item) => item.length > 0);
  }

  private toDraft(): ProjectDraft {
    return {
      title: this.title,
      overview: this.overview,
      contribution: this.contribution,
      technologies: this.splitTechnologies(),
      demoUrl: this.demoUrl.trim() || null,
      sourceUrl: this.sourceUrl.trim() || null,
      coverImage: this.coverImage,
      coverImageAlt: this.coverImageAlt,
      // Only a date Gustavo changed is sent, so saving a fix keeps the exact time of publication.
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

    return ['The project could not be saved. Try again.'];
  }
}
