import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ImageApi } from '../api/image-api';
import { ProjectApi } from '../api/project-api';
import { Project } from '../models/project';
import { ProjectFormPage } from './project-form-page';

type Form = {
  title: string;
  overview: string;
  technologiesText: string;
  demoUrl: string;
  saveDraft(): void;
  publish(): void;
  returnToDraft(): void;
  togglePreview(): void;
};

function project(overrides: Partial<Project> = {}): Project {
  return {
    id: 5,
    title: 'Goose website',
    slug: 'goose-website',
    overview: '# About\n\nA *personal* site.',
    contribution: 'Everything.',
    technologies: ['C#', 'Angular'],
    demoUrl: 'https://example.test/demo',
    sourceUrl: null,
    coverImage: '',
    coverImageAlt: '',
    isPublished: false,
    createdAtUtc: '2026-10-15T10:00:00Z',
    updatedAtUtc: '2026-10-15T10:00:00Z',
    publishedAtUtc: null,
    ...overrides,
  };
}

// The project form: saving a draft never publishes, publishing is its own action, and what the API refuses is shown.
describe('ProjectFormPage', () => {
  let api: jasmine.SpyObj<ProjectApi>;
  let router: Router;
  let fixture: ComponentFixture<ProjectFormPage>;

  async function open(id: string | null, loaded: Project = project()): Promise<Form> {
    api = jasmine.createSpyObj<ProjectApi>('ProjectApi', ['get', 'create', 'update', 'publish', 'unpublish']);
    api.get.and.returnValue(of(loaded));
    await TestBed.configureTestingModule({
      imports: [ProjectFormPage],
      providers: [
        provideRouter([]),
        { provide: ProjectApi, useValue: api },
        { provide: ImageApi, useValue: {} },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } },
      ],
    }).compileComponents();
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(ProjectFormPage);
    fixture.detectChanges();
    return fixture.componentInstance as unknown as Form;
  }

  function text(): string {
    fixture.detectChanges();
    return fixture.nativeElement.textContent as string;
  }

  it('opens an existing project with its saved values and says it is a draft', async () => {
    const form = await open('5');

    expect(api.get).toHaveBeenCalledWith(5);
    expect(form.title).toBe('Goose website');
    expect(form.technologiesText).toBe('C#, Angular');
    expect(form.demoUrl).toBe('https://example.test/demo');
    expect(text()).toContain('Status: Draft');
  });

  it('saves a new project as a draft with technologies split on commas and empty links sent as null', async () => {
    const form = await open(null);
    api.create.and.returnValue(of(project({ id: 9 })));
    form.title = 'Goose website';
    form.technologiesText = ' C# ,Angular,, ';

    form.saveDraft();

    expect(api.create).toHaveBeenCalledWith(
      jasmine.objectContaining({ title: 'Goose website', technologies: ['C#', 'Angular'], demoUrl: null, sourceUrl: null }),
    );
    expect(api.publish).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/editor/projects', 9], { replaceUrl: true });
  });

  it('publishes by saving first and then publishing', async () => {
    const form = await open('5');
    api.update.and.returnValue(of(project()));
    api.publish.and.returnValue(of(project({ isPublished: true, publishedAtUtc: '2026-10-15T12:00:00Z' })));

    form.publish();

    expect(api.update).toHaveBeenCalledWith(5, jasmine.objectContaining({ publishedAtUtc: null }));
    expect(api.publish).toHaveBeenCalledWith(5);
    expect(text()).toContain('Status: Published on 2026-10-15');
  });

  it('shows what the API refused, such as a bad link, and stays a draft', async () => {
    const form = await open('5');
    api.update.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { demoUrl: ['The demo link must be a web address.'] } } })),
    );

    form.saveDraft();

    expect(text()).toContain('The demo link must be a web address.');
    expect(text()).toContain('Status: Draft');
  });

  it('previews the rendered overview, the technologies and the draft label', async () => {
    const form = await open('5');

    form.togglePreview();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.preview h1')?.textContent).toBe('About');
    expect(text()).toContain('not public');
    expect(element.querySelectorAll('.tech li').length).toBe(2);
    expect(element.querySelector('a[target="_blank"]')?.getAttribute('rel')).toContain('noopener');
  });

  it('returns a published project to draft on request', async () => {
    const form = await open('5', project({ isPublished: true, publishedAtUtc: '2026-10-15T12:00:00Z' }));
    api.unpublish.and.returnValue(of(project({ publishedAtUtc: '2026-10-15T12:00:00Z' })));

    form.returnToDraft();

    expect(api.unpublish).toHaveBeenCalledWith(5);
    expect(text()).toContain('Status: Draft');
  });

  it('shows the status as a labelled badge and the actions as shared buttons', async () => {
    await open('5');

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('main.gcv-editor')).not.toBeNull();
    expect(element.querySelector('.gcv-chip[role="status"]')?.textContent?.trim()).toBe('Status: Draft');
    const buttons = Array.from(element.querySelectorAll<HTMLButtonElement>('.gcv-actions button'));
    expect(buttons.map((button) => button.textContent?.trim())).toEqual(['Save draft', 'Preview', 'Publish']);
    expect(buttons.every((button) => button.classList.contains('gcv-btn'))).toBeTrue();
    expect(buttons[0].classList).toContain('gcv-btn--solid');
    expect(element.querySelectorAll('.gcv-field input, .gcv-field textarea').length).toBeGreaterThanOrEqual(7);
  });
});
