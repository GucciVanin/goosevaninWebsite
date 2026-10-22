import { Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthApi } from '../auth/api/auth-api';
import { AuthenticatedUser } from '../auth/models/authenticated-user';
import { ImageApi } from './api/image-api';
import { ProjectApi } from './api/project-api';
import { StoryApi } from './api/story-api';
import { ImageControls } from './components/image-controls';
import { EditorPage } from './editor-page';
import { Project } from './models/project';
import { Story } from './models/story';
import { ProjectFormPage } from './pages/project-form-page';
import { StoryFormPage } from './pages/story-form-page';

// F9-US5-AC2 evidence: the editor is built from the design tokens only, shows every state in words, and gives every
// control a name. Component source is checked through the compiled component definition (its inline styles and its
// template function and constants), because Karma does not serve the .ts files as text.

const legacyAlias = /(?<![\w-])--(paper|accent|accent-soft|ink|muted|line|line-strong|mono|display|body|sage|sand)(?![\w-])/;
const colourLiteral = /#[0-9a-f]{3,8}\b|\brgba?\(|\bhsla?\(/i;

type Definition = { styles?: string[]; template?: { toString(): string }; consts?: unknown };

function definitionOf(component: Type<unknown>): Definition {
  return (component as unknown as { ɵcmp: Definition }).ɵcmp;
}

function story(overrides: Partial<Story> = {}): Story {
  return {
    id: 5,
    title: 'Trip',
    slug: 'trip',
    excerpt: 'Short.',
    content: 'Text',
    category: 'Notes',
    tags: [],
    coverImage: '',
    coverImageAlt: '',
    isPublished: false,
    createdAtUtc: '2026-10-15T10:00:00Z',
    updatedAtUtc: '2026-10-15T10:00:00Z',
    publishedAtUtc: null,
    ...overrides,
  } as Story;
}

function project(overrides: Partial<Project> = {}): Project {
  return {
    id: 5,
    title: 'Goose website',
    slug: 'goose-website',
    overview: 'About',
    contribution: 'Everything.',
    technologies: [],
    demoUrl: null,
    sourceUrl: null,
    coverImage: '',
    coverImageAlt: '',
    isPublished: false,
    createdAtUtc: '2026-10-15T10:00:00Z',
    updatedAtUtc: '2026-10-15T10:00:00Z',
    publishedAtUtc: null,
    ...overrides,
  } as Project;
}

async function mount<T>(component: Type<T>, id: string | null, loadedStory?: Story, loadedProject?: Project): Promise<ComponentFixture<T>> {
  const storyApi = jasmine.createSpyObj<StoryApi>('StoryApi', ['get', 'list']);
  storyApi.get.and.returnValue(of(loadedStory ?? story()));
  storyApi.list.and.returnValue(
    of([
      { id: 1, title: 'Alpha', slug: 'a', category: 'N', isPublished: true, createdAtUtc: '2026-10-01T10:00:00Z', updatedAtUtc: '2026-10-01T10:00:00Z', publishedAtUtc: '2026-10-02T10:00:00Z' },
      { id: 2, title: 'Beta', slug: 'b', category: 'N', isPublished: false, createdAtUtc: '2026-10-01T10:00:00Z', updatedAtUtc: '2026-10-02T10:00:00Z', publishedAtUtc: null },
    ]),
  );
  const projectApi = jasmine.createSpyObj<ProjectApi>('ProjectApi', ['get', 'list']);
  projectApi.get.and.returnValue(of(loadedProject ?? project()));
  projectApi.list.and.returnValue(of([]));
  const auth = jasmine.createSpyObj<AuthApi>('AuthApi', ['getCurrentUser']);
  auth.getCurrentUser.and.returnValue(of({ displayName: 'Site administrator' } as unknown as AuthenticatedUser));
  await TestBed.configureTestingModule({
    imports: [component],
    providers: [
      provideRouter([]),
      { provide: StoryApi, useValue: storyApi },
      { provide: ProjectApi, useValue: projectApi },
      { provide: AuthApi, useValue: auth },
      { provide: ImageApi, useValue: {} },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } },
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();
  return fixture;
}

describe('Editor design (F9-US5-AC2)', () => {
  const components: [string, Type<unknown>][] = [
    ['EditorPage', EditorPage],
    ['StoryFormPage', StoryFormPage],
    ['ProjectFormPage', ProjectFormPage],
    ['ImageControls', ImageControls],
  ];

  for (const [name, component] of components) {
    it(`${name} uses no legacy alias and no colour literal in its styles or template`, () => {
      const definition = definitionOf(component);
      const styles = (definition.styles ?? []).join('\n');
      const templateSource = `${definition.template?.toString() ?? ''}\n${JSON.stringify(definition.consts ?? '')}`;

      expect(definition.template).toBeDefined();
      expect(styles).not.toMatch(legacyAlias);
      expect(styles).not.toMatch(colourLiteral);
      expect(templateSource).not.toMatch(legacyAlias);
      expect(templateSource).not.toMatch(/style\\?"?[^]{0,3}(#[0-9a-f]{3,8}\b|rgba?\()/i);
    });
  }

  it('the rendered editor markup carries no inline style, legacy alias or colour literal', async () => {
    for (const [component, id] of [
      [EditorPage, null],
      [StoryFormPage, '5'],
      [ProjectFormPage, '5'],
    ] as [Type<unknown>, string | null][]) {
      TestBed.resetTestingModule();
      const fixture = await mount(component, id);
      const html = (fixture.nativeElement as HTMLElement).outerHTML;
      expect(html).not.toMatch(legacyAlias);
      expect(html).not.toMatch(/\sstyle="[^"]*(#[0-9a-f]{3,8}\b|rgba?\()/i);
    }
  });

  it('the global .gcv-editor and .gcv-auth rules carry no colour literal (glows come from tokens)', () => {
    const offenders: string[] = [];
    const visit = (rules: CSSRuleList) => {
      for (const rule of Array.from(rules)) {
        // Chrome also gives style rules a cssRules list (CSS nesting), so test for a selector first.
        const selector = (rule as CSSStyleRule).selectorText;
        if (selector !== undefined) {
          if (/\.gcv-(editor|auth)\b/.test(selector) && colourLiteral.test(rule.cssText)) {
            offenders.push(rule.cssText);
          }
        } else if ((rule as CSSGroupingRule).cssRules) {
          visit((rule as CSSGroupingRule).cssRules);
        }
      }
    };
    let seen = 0;
    for (const sheet of Array.from(document.styleSheets)) {
      let rules: CSSRuleList;
      try {
        rules = sheet.cssRules;
      } catch {
        continue; // cross-origin sheet (web fonts): not ours
      }
      for (const rule of Array.from(rules)) {
        if (/\.gcv-editor\b/.test((rule as CSSStyleRule).selectorText ?? '')) {
          seen++;
        }
      }
      visit(rules);
    }
    expect(seen).toBeGreaterThan(0); // the global stylesheet is loaded in the test run, so the check is not vacuous
    expect(offenders).toEqual([]);
  });

  it('the workspace writes both row states in words', async () => {
    const fixture = await mount(EditorPage, null);
    const chips = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('.story-list .gcv-chip')).map((chip) => chip.textContent?.trim());
    expect(chips).toEqual(['State: Published', 'State: Draft']);
  });

  it('the story form writes the Draft and the Published status in words', async () => {
    const draft = await mount(StoryFormPage, '5');
    expect((draft.nativeElement as HTMLElement).querySelector('.gcv-chip[role="status"]')?.textContent).toContain('Status: Draft');

    TestBed.resetTestingModule();
    const published = await mount(StoryFormPage, '5', story({ isPublished: true, publishedAtUtc: '2026-10-15T10:00:00Z' }));
    expect((published.nativeElement as HTMLElement).querySelector('.gcv-chip[role="status"]')?.textContent).toContain('Status: Published on 2026-10-15');
  });

  it('the project form writes the Draft and the Published status in words', async () => {
    const draft = await mount(ProjectFormPage, '5');
    expect((draft.nativeElement as HTMLElement).querySelector('.gcv-chip[role="status"]')?.textContent).toContain('Status: Draft');

    TestBed.resetTestingModule();
    const published = await mount(ProjectFormPage, '5', undefined, project({ isPublished: true, publishedAtUtc: '2026-10-15T10:00:00Z' }));
    expect((published.nativeElement as HTMLElement).querySelector('.gcv-chip[role="status"]')?.textContent).toContain('Status: Published on 2026-10-15');
  });

  it('every button, link and form control on the workspace and both forms has an accessible name', async () => {
    for (const [component, id] of [
      [EditorPage, null],
      [StoryFormPage, '5'],
      [ProjectFormPage, '5'],
    ] as [Type<unknown>, string | null][]) {
      TestBed.resetTestingModule();
      const fixture = await mount(component, id);
      const root = fixture.nativeElement as HTMLElement;
      const unnamed: string[] = [];

      for (const control of Array.from(root.querySelectorAll<HTMLElement>('button, a'))) {
        const named = (control.textContent ?? '').trim().length > 0 || (control.getAttribute('aria-label') ?? '').trim().length > 0;
        if (!named) {
          unnamed.push(control.outerHTML);
        }
      }

      for (const field of Array.from(root.querySelectorAll<HTMLInputElement>('input, textarea, select'))) {
        const label = field.id ? root.querySelector(`label[for="${field.id}"]`) : null;
        if (!label && !field.getAttribute('aria-label')) {
          unnamed.push(field.outerHTML);
        }
      }

      expect(unnamed).toEqual([]);
    }
  });

  it('the Stories and Projects switch exposes aria-pressed on both buttons and moves it on choosing', async () => {
    const fixture = await mount(EditorPage, null);
    const root = fixture.nativeElement as HTMLElement;
    const pressed = () => Array.from(root.querySelectorAll<HTMLButtonElement>('.gcv-toggle button')).map((button) => button.getAttribute('aria-pressed'));

    expect(pressed()).toEqual(['true', 'false']);
    root.querySelectorAll<HTMLButtonElement>('.gcv-toggle button')[1].click();
    fixture.detectChanges();
    expect(pressed()).toEqual(['false', 'true']);
  });
});
