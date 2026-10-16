import { Component } from '@angular/core';
import { ArtScene } from '../../shared/brand/art-scene';
import { BrandLogo } from '../../shared/brand/brand-logo';
import { ProjectPanel } from '../../shared/brand/project-panel';

interface Swatch {
  name: string;
  token: string;
  note: string;
}

// The F9-US1 review page: every token and base component in one place, with the animated scene and a sample project
// panel, so Gustavo can approve the foundation before the shell and home page are rebuilt on it. It holds sample
// content only and is removed before R1 ships (tracked with TE4).
@Component({
  selector: 'app-design-page',
  imports: [ArtScene, BrandLogo, ProjectPanel],
  template: `
    <main class="design">
      <section class="hero" aria-labelledby="design-title">
        <app-art-scene class="scene" />
        <div class="veil"></div>
        <div class="hero-copy">
          <app-brand-logo [size]="112" />
          <p class="gcv-eyebrow">Build · Break · Improve</p>
          <h1 id="design-title" class="gcv-heading gcv-heading--hero">Software <em>Engineer</em></h1>
          <hr class="gcv-rule rule" />
          <p class="intro">Curious mind. Practical solutions. Real impact.</p>
          <a class="gcv-btn" href="#tokens">Explore the system <span aria-hidden="true">→</span></a>
        </div>
      </section>

      <div class="page">
        <p class="note gcv-label">Sample page for F9-US1 review. It holds sample content and is removed before release.</p>

        <section id="tokens" aria-labelledby="colors-title">
          <h2 id="colors-title" class="gcv-heading gcv-heading--section">Colour</h2>
          <ul class="swatches">
            @for (swatch of swatches; track swatch.token) {
              <li>
                <span class="chip" [style.background]="'var(' + swatch.token + ')'"></span>
                <span class="gcv-label">{{ swatch.name }}</span>
                <code>{{ swatch.token }}</code>
                <span class="hint">{{ swatch.note }}</span>
              </li>
            }
          </ul>
        </section>

        <section aria-labelledby="type-title">
          <h2 id="type-title" class="gcv-heading gcv-heading--section">Type</h2>
          <p class="gcv-eyebrow">Eyebrow, mono, tracked</p>
          <h3 class="gcv-heading gcv-heading--card">Display heading, Michroma</h3>
          <p class="body">Body text is DM Sans at a comfortable reading size and line height, so long stories and project posts stay easy to read on the dark surface.</p>
          <p class="gcv-label">Label, JetBrains Mono, uppercase, tracked</p>
        </section>

        <section aria-labelledby="components-title">
          <h2 id="components-title" class="gcv-heading gcv-heading--section">Components</h2>
          <div class="row">
            <a class="gcv-btn" href="#tokens">Corner-bracket button <span aria-hidden="true">→</span></a>
            <button type="button" class="gcv-btn gcv-btn--solid">Solid button</button>
            <button type="button" class="gcv-btn" disabled>Disabled</button>
            <a class="gcv-link" href="#tokens">A text link</a>
          </div>
          <div class="fields">
            <div class="gcv-field">
              <label for="sample-name">Name</label>
              <input id="sample-name" type="text" placeholder="Your name" />
            </div>
            <div class="gcv-field">
              <label for="sample-message">Message</label>
              <textarea id="sample-message" rows="3" placeholder="Say something"></textarea>
            </div>
          </div>
          <div class="row">
            <span class="gcv-chip">C#</span><span class="gcv-chip">Angular</span><span class="gcv-chip">SQLite</span>
          </div>
        </section>

        <section aria-labelledby="cards-title">
          <h2 id="cards-title" class="gcv-heading gcv-heading--section">Category cards</h2>
          <ul class="cards">
            @for (card of cards; track card.title) {
              <li class="card" [style.--card]="'var(' + card.token + ')'">
                <h3 class="gcv-heading gcv-heading--card">{{ card.title }}</h3>
                <span class="arrow" aria-hidden="true">→</span>
              </li>
            }
          </ul>
        </section>

        <section aria-labelledby="panel-title">
          <h2 id="panel-title" class="gcv-heading gcv-heading--section">Project panel</h2>
          <div class="panel-grid">
            <app-project-panel
              title="Sample project"
              slug="sample-project"
              summary="A short case: what it is, what Gustavo did on it, and what it taught him. Select the link below for the full post."
              [technologies]="['C#', 'Angular', 'Docker']"
              image="/brand/hero-city-960.webp"
              imageAlt="A cyberpunk city at dusk, standing in for a project screenshot"
              demoUrl="https://example.test/demo"
              sourceUrl="https://example.test/source"
            />
          </div>
        </section>
      </div>
    </main>
  `,
  styles: `
    :host { background: var(--gcv-bg-0); color: var(--gcv-text); display: block; min-height: 100vh; }
    .hero { isolation: isolate; min-height: clamp(520px, 82vh, 860px); overflow: hidden; position: relative; }
    .scene { inset: 0; position: absolute; z-index: -2; }
    .veil { background: linear-gradient(90deg, var(--gcv-bg-0) 0%, rgba(5, 6, 13, 0.75) 38%, transparent 70%), linear-gradient(0deg, var(--gcv-bg-0), transparent 30%); inset: 0; position: absolute; z-index: -1; }
    .hero-copy { display: grid; gap: var(--gcv-space-4); justify-items: start; max-width: 560px; padding: var(--gcv-space-6) var(--gcv-gutter) var(--gcv-space-8); }
    .rule { width: min(360px, 80%); }
    .intro { color: var(--gcv-text); font: var(--gcv-text-md) / 1.8 var(--gcv-font-mono); letter-spacing: 0.12em; margin: 0; text-transform: uppercase; }
    .page { display: grid; gap: var(--gcv-space-8); margin: 0 auto; max-width: var(--gcv-content-width); padding: var(--gcv-space-7) var(--gcv-gutter) var(--gcv-space-8); }
    section { display: grid; gap: var(--gcv-space-5); }
    .swatches { display: grid; gap: var(--gcv-space-4); grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); list-style: none; margin: 0; padding: 0; }
    .swatches li { display: grid; gap: var(--gcv-space-1); }
    .chip { border: 1px solid var(--gcv-line-strong); height: 48px; }
    code { color: var(--gcv-cyan-soft); font: var(--gcv-text-xs) var(--gcv-font-mono); }
    .hint, .body { color: var(--gcv-text-muted); font-size: var(--gcv-text-sm); line-height: var(--gcv-leading-body); margin: 0; max-width: 64ch; }
    .row { align-items: center; display: flex; flex-wrap: wrap; gap: var(--gcv-space-4) var(--gcv-space-5); }
    .fields { display: grid; gap: var(--gcv-space-4); grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); }
    .cards { display: grid; gap: var(--gcv-space-4); grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); list-style: none; margin: 0; padding: 0; }
    .card { align-items: center; background: var(--gcv-bg-1); border: 1px solid var(--card); border-radius: var(--gcv-radius-card); display: flex; justify-content: space-between; padding: var(--gcv-space-5) var(--gcv-space-4); }
    .arrow { color: var(--card); }
    .panel-grid { max-width: 480px; }
  `,
})
export class DesignPage {
  protected readonly swatches: Swatch[] = [
    { name: 'Background', token: '--gcv-bg-0', note: 'Page' },
    { name: 'Surface', token: '--gcv-bg-1', note: 'Cards, fields' },
    { name: 'Raised', token: '--gcv-bg-2', note: 'Hover, nested' },
    { name: 'Line', token: '--gcv-line', note: 'Hairlines' },
    { name: 'Text', token: '--gcv-text', note: 'Body and headings' },
    { name: 'Muted text', token: '--gcv-text-muted', note: 'Secondary text' },
    { name: 'Cyan', token: '--gcv-cyan', note: 'Primary accent, links, focus' },
    { name: 'Magenta', token: '--gcv-magenta', note: 'Secondary accent, eyebrow' },
    { name: 'Violet', token: '--gcv-violet', note: 'Button brackets' },
    { name: 'Green', token: '--gcv-green', note: 'Security card only' },
    { name: 'Orange', token: '--gcv-orange', note: 'Systems & Tools card only' },
  ];

  protected readonly cards = [
    { title: 'Applications', token: '--gcv-magenta' },
    { title: 'Cloud & DevOps', token: '--gcv-cyan' },
    { title: 'Security', token: '--gcv-green' },
    { title: 'Systems & Tools', token: '--gcv-orange' },
  ];
}
