import { HttpErrorResponse } from '@angular/common/http';
import { Component, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ImageApi, UploadedImage } from '../api/image-api';

const MaxBytes = 5 * 1024 * 1024;
const Allowed = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];

// Image handling for the story and project forms: a cover image with its alternative text, and images placed inside
// the text (as Markdown). Files are checked here for a friendly message and again by the API, which is what enforces
// the rules (JPEG, PNG, WebP or GIF, up to 5 MB).
@Component({
  selector: 'app-image-controls',
  imports: [FormsModule],
  template: `
    <fieldset class="images">
      <legend>Images</legend>

      <p class="hint">JPEG, PNG, WebP or GIF, up to 5 MB.</p>

      @if (coverImage()) {
        <img class="cover" [src]="coverImage()" [alt]="coverImageAlt() || 'Cover image (no description yet)'" />
      }

      <label for="cover-file">{{ coverImage() ? 'Replace the cover image' : 'Cover image' }}</label>
      <input id="cover-file" type="file" accept="image/jpeg,image/png,image/webp,image/gif" [disabled]="busy()" (change)="chooseCover($event)" />

      @if (coverImage()) {
        <label for="cover-alt">Cover image description (for readers who cannot see it)</label>
        <input id="cover-alt" type="text" [ngModel]="coverImageAlt()" (ngModelChange)="coverImageAlt.set($event)" />
        <button type="button" class="secondary" (click)="removeCover()">Remove the cover image</button>
      }

      <label for="inline-alt">Description for an image inside the text</label>
      <input id="inline-alt" type="text" [ngModel]="inlineAlt()" (ngModelChange)="inlineAlt.set($event)" />
      <label for="inline-file">Add an image to the text</label>
      <input id="inline-file" type="file" accept="image/jpeg,image/png,image/webp,image/gif" [disabled]="busy()" (change)="chooseInline($event)" />

      @if (problem()) {
        <p class="problem" role="alert">{{ problem() }}</p>
      }
      @if (note()) {
        <p class="note" role="status">{{ note() }}</p>
      }
    </fieldset>
  `,
  styles: `
    .images { border: 1px solid var(--line); display: grid; gap: 8px; margin: 12px 0 0; padding: 12px; }
    legend { color: var(--muted); font: 11px var(--mono); letter-spacing: .08em; padding: 0 6px; text-transform: uppercase; }
    label { color: var(--muted); font: 11px var(--mono); letter-spacing: .08em; text-transform: uppercase; }
    input[type='text'] { background: #eef1e8; border: 1px solid var(--line); border-radius: 0; box-sizing: border-box; color: #172622; font: 16px var(--body); min-height: 44px; padding: 10px 12px; width: 100%; }
    input[type='file'] { color: var(--ink); max-width: 100%; }
    .cover { max-height: 200px; max-width: 100%; object-fit: contain; }
    .hint, .note { color: var(--muted); font: 12px var(--mono); margin: 0; }
    .problem { border: 1px solid var(--accent); margin: 0; padding: 8px 12px; }
    button { background: transparent; border: 1px solid var(--line); color: var(--ink); cursor: pointer; font: 600 14px var(--body); min-height: 44px; padding: 8px 16px; }
    input:focus-visible, button:focus-visible { outline: 2px solid var(--accent-soft); outline-offset: 3px; }
  `,
})
export class ImageControls {
  readonly coverImage = model('');
  readonly coverImageAlt = model('');

  /** Markdown for an image just uploaded, ready to be placed in the text. */
  readonly insert = output<string>();

  protected readonly inlineAlt = signal('');
  protected readonly busy = signal(false);
  protected readonly problem = signal('');
  protected readonly note = signal('');

  constructor(private readonly api: ImageApi) {}

  protected chooseCover(event: Event): void {
    this.upload(event, (image) => {
      this.coverImage.set(image.url);
      this.note.set('Cover image uploaded. Save the draft to keep it.');
    });
  }

  protected chooseInline(event: Event): void {
    const description = this.inlineAlt().trim();
    if (!description) {
      this.problem.set('Describe the image first, so readers who cannot see it know what it shows.');
      (event.target as HTMLInputElement).value = '';
      return;
    }

    this.upload(event, (image) => {
      this.insert.emit(`![${description.replace(/[[\]]/g, '')}](${image.url})`);
      this.inlineAlt.set('');
      this.note.set('Image added to the end of the text.');
    });
  }

  protected removeCover(): void {
    this.coverImage.set('');
    this.coverImageAlt.set('');
    this.note.set('Cover image removed. Save the draft to keep this.');
  }

  private upload(event: Event, done: (image: UploadedImage) => void): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);
    this.problem.set('');
    this.note.set('');
    if (!file) {
      return;
    }

    if (!Allowed.includes(file.type)) {
      this.problem.set('Only JPEG, PNG, WebP and GIF images can be uploaded.');
      input.value = '';
      return;
    }

    if (file.size > MaxBytes) {
      this.problem.set('The image is larger than 5 MB.');
      input.value = '';
      return;
    }

    this.busy.set(true);
    this.api.upload(file).subscribe({
      next: (image) => {
        done(image);
        this.busy.set(false);
        input.value = '';
      },
      error: (error: unknown) => {
        this.problem.set(this.describe(error));
        this.busy.set(false);
        input.value = '';
      },
    });
  }

  private describe(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 413) {
        return 'The image is larger than 5 MB.';
      }

      const detail = (error.error as { detail?: string } | null)?.detail;
      if (error.status === 400 && detail) {
        return detail;
      }
    }

    return 'The image could not be uploaded. Try again.';
  }
}
