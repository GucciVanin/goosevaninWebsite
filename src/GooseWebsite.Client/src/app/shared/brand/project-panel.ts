import { Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

// A project as a "system diagnostic" panel: a screenshot or GIF, the technology stack, and a small VIEW CASE control
// that opens the case in place. The case offers the demo and source links and a link to the full project post
// (/projects/<slug>, built by F5-US1). The panel is closed by default and works by keyboard.
@Component({
  selector: 'app-project-panel',
  imports: [RouterLink],
  template: `
    <article class="panel" [class.open]="open()">
      <header class="bar">
        <span class="lamp" aria-hidden="true"></span>
        <span class="gcv-label">PROJECT / {{ slug() }}</span>
        <span class="gcv-label status">{{ open() ? 'CASE OPEN' : 'READY' }}</span>
      </header>

      @if (image()) {
        <img class="shot" [src]="image()" [alt]="imageAlt()" loading="lazy" />
      }

      <div class="body">
        <h3 class="gcv-heading gcv-heading--card">{{ title() }}</h3>
        <ul class="stack" aria-label="Technology stack">
          @for (item of technologies(); track item) {
            <li class="gcv-chip">{{ item }}</li>
          }
        </ul>

        <button type="button" class="view" [attr.aria-expanded]="open()" [attr.aria-controls]="'case-' + slug()" (click)="open.set(!open())">
          {{ open() ? 'CLOSE CASE' : 'VIEW CASE' }} <span aria-hidden="true">{{ open() ? '↑' : '→' }}</span>
        </button>

        @if (open()) {
          <div class="case" [id]="'case-' + slug()">
            <p>{{ summary() }}</p>
            <div class="links">
              <a class="gcv-link" [routerLink]="['/projects', slug()]">Open the full project post →</a>
              @if (demoUrl()) {
                <a class="gcv-link" [href]="demoUrl()" rel="noopener noreferrer" target="_blank">Demo</a>
              }
              @if (sourceUrl()) {
                <a class="gcv-link" [href]="sourceUrl()" rel="noopener noreferrer" target="_blank">Source code</a>
              }
            </div>
          </div>
        }
      </div>
    </article>
  `,
  styles: `
    .panel { background: var(--gcv-bg-1); border: 1px solid var(--gcv-line); border-radius: var(--gcv-radius-card); overflow: hidden; transition: border-color var(--gcv-motion-base) var(--gcv-ease); }
    .panel:hover, .panel.open { border-color: var(--gcv-cyan); }
    .bar { align-items: center; border-bottom: 1px solid var(--gcv-line); display: flex; gap: var(--gcv-space-3); padding: var(--gcv-space-2) var(--gcv-space-4); }
    .lamp { background: var(--gcv-cyan); border-radius: 50%; box-shadow: 0 0 8px var(--gcv-cyan); height: 6px; width: 6px; }
    .status { margin-left: auto; }
    .shot { aspect-ratio: 16 / 9; background: var(--gcv-bg-0); display: block; object-fit: cover; width: 100%; }
    .body { display: grid; gap: var(--gcv-space-4); padding: var(--gcv-space-4); }
    .stack { display: flex; flex-wrap: wrap; gap: var(--gcv-space-2); list-style: none; margin: 0; padding: 0; }
    .view { background: none; border: 0; color: var(--gcv-cyan); cursor: pointer; font: 500 var(--gcv-text-xs) / 1 var(--gcv-font-mono); justify-self: start; letter-spacing: var(--gcv-track-label); min-height: var(--gcv-control-height); padding: 0; }
    .view:hover { color: var(--gcv-cyan-soft); }
    .case { border-top: 1px dashed var(--gcv-line-strong); color: var(--gcv-text-muted); line-height: var(--gcv-leading-body); padding-top: var(--gcv-space-4); }
    .case p { margin: 0 0 var(--gcv-space-4); }
    .links { display: flex; flex-wrap: wrap; gap: var(--gcv-space-3) var(--gcv-space-5); }
  `,
})
export class ProjectPanel {
  readonly title = input.required<string>();
  readonly slug = input.required<string>();
  readonly summary = input('');
  readonly technologies = input<string[]>([]);
  readonly image = input('');
  readonly imageAlt = input('');
  readonly demoUrl = input<string | null>(null);
  readonly sourceUrl = input<string | null>(null);

  protected readonly open = signal(false);
}
