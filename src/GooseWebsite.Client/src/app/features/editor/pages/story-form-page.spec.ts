import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ImageApi } from '../api/image-api';
import { StoryApi } from '../api/story-api';
import { Story } from '../models/story';
import { StoryFormPage } from './story-form-page';

type Form = {
  title: string;
  excerpt: string;
  content: string;
  category: string;
  tagsText: string;
  saveDraft(): void;
  publish(): void;
  returnToDraft(): void;
  togglePreview(): void;
};

function story(overrides: Partial<Story> = {}): Story {
  return {
    id: 5,
    title: 'A first story',
    slug: 'a-first-story',
    excerpt: 'Why it matters.',
    content: '# Heading\n\nSome *text*.',
    category: 'Notes',
    tags: ['life', 'work'],
    coverImage: '',
    coverImageAlt: '',
    isPublished: false,
    createdAtUtc: '2026-10-15T10:00:00Z',
    updatedAtUtc: '2026-10-15T10:00:00Z',
    publishedAtUtc: null,
    authorId: 'a',
    authorName: 'Site administrator',
    ...overrides,
  };
}

// The story form: saving a draft never publishes, publishing is its own action, and what the API refuses is shown.
describe('StoryFormPage', () => {
  let api: jasmine.SpyObj<StoryApi>;
  let router: Router;
  let fixture: ComponentFixture<StoryFormPage>;

  async function open(id: string | null, loaded: Story = story()): Promise<Form> {
    api = jasmine.createSpyObj<StoryApi>('StoryApi', ['get', 'create', 'update', 'publish', 'unpublish']);
    api.get.and.returnValue(of(loaded));
    await TestBed.configureTestingModule({
      imports: [StoryFormPage],
      providers: [
        provideRouter([]),
        { provide: StoryApi, useValue: api },
        { provide: ImageApi, useValue: {} },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } },
      ],
    }).compileComponents();
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(StoryFormPage);
    fixture.detectChanges();
    return fixture.componentInstance as unknown as Form;
  }

  function text(): string {
    fixture.detectChanges();
    return fixture.nativeElement.textContent as string;
  }

  it('opens an existing story with its saved values and says whether it is a draft', async () => {
    const form = await open('5');

    expect(api.get).toHaveBeenCalledWith(5);
    expect(form.title).toBe('A first story');
    expect(form.excerpt).toBe('Why it matters.');
    expect(form.tagsText).toBe('life, work');
    expect(text()).toContain('Status: Draft');
  });

  it('saves a new story as a draft, with the tags split on commas, and moves to its own address', async () => {
    const form = await open(null);
    api.create.and.returnValue(of(story({ id: 9 })));
    form.title = 'A first story';
    form.excerpt = 'Why it matters.';
    form.content = 'Body';
    form.category = 'Notes';
    form.tagsText = ' life ,work,, ';

    form.saveDraft();

    expect(api.create).toHaveBeenCalledWith(jasmine.objectContaining({ title: 'A first story', tags: ['life', 'work'] }));
    expect(api.publish).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/editor/stories', 9], { replaceUrl: true });
    expect(text()).toContain('Saved');
  });

  it('saving an existing story updates it and does not publish', async () => {
    const form = await open('5');
    api.update.and.returnValue(of(story({ title: 'Renamed' })));
    form.title = 'Renamed';

    form.saveDraft();

    expect(api.update).toHaveBeenCalledWith(5, jasmine.objectContaining({ title: 'Renamed' }));
    expect(api.publish).not.toHaveBeenCalled();
    expect(text()).toContain('Status: Draft');
  });

  it('does not resend the publication date unless Gustavo changed it', async () => {
    const form = await open('5', story({ isPublished: true, publishedAtUtc: '2026-10-15T14:22:00Z' }));
    api.update.and.returnValue(of(story({ isPublished: true, publishedAtUtc: '2026-10-15T14:22:00Z' })));

    form.saveDraft();

    expect(api.update).toHaveBeenCalledWith(5, jasmine.objectContaining({ publishedAtUtc: null }));
  });

  it('publishes by saving first and then publishing, and then says it is published', async () => {
    const form = await open('5');
    api.update.and.returnValue(of(story()));
    api.publish.and.returnValue(of(story({ isPublished: true, publishedAtUtc: '2026-10-15T12:00:00Z' })));

    form.publish();

    expect(api.update).toHaveBeenCalled();
    expect(api.publish).toHaveBeenCalledWith(5);
    expect(text()).toContain('Status: Published on 2026-10-15');
  });

  it('shows what the API refused and does not claim the story was published', async () => {
    const form = await open('5');
    api.update.and.returnValue(of(story()));
    api.publish.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { excerpt: ['Add an excerpt before publishing.'] } } })),
    );

    form.publish();

    expect(text()).toContain('Add an excerpt before publishing.');
    expect(text()).toContain('Status: Draft');
    expect(text()).not.toContain('Published on');
  });

  it('shows a refused save with the field it concerns', async () => {
    const form = await open(null);
    api.create.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { Title: ['A story needs a title.'] } } })),
    );

    form.saveDraft();

    expect(text()).toContain('A story needs a title.');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('previews the rendered story and labels a draft as not public', async () => {
    const form = await open('5');

    form.togglePreview();
    fixture.detectChanges();

    const preview = fixture.nativeElement.querySelector('.preview') as HTMLElement;
    expect(preview.querySelector('h1')?.textContent).toBe('Heading');
    expect(preview.querySelector('em')?.textContent).toBe('text');
    expect(text()).toContain('Draft preview');
    expect(text()).toContain('not public');
  });

  it('offers to return a published story to draft, and does so on request', async () => {
    const form = await open('5', story({ isPublished: true, publishedAtUtc: '2026-10-15T12:00:00Z' }));
    api.unpublish.and.returnValue(of(story({ publishedAtUtc: '2026-10-15T12:00:00Z' })));

    expect(text()).toContain('Return to draft');
    form.returnToDraft();

    expect(api.unpublish).toHaveBeenCalledWith(5);
    expect(text()).toContain('Status: Draft');
  });
});