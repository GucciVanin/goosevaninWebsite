import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StoryFilter } from '../../shared/story-filter';
import { BlogPage } from './blog-page';

// TE4: the Stories section shows only what /api/blog returns. These specs use a stubbed API with invented titles of
// their own; nothing here is shown by the application.
const apiStory = (over: Record<string, unknown>) => ({
  id: 1, title: 'Alpha', slug: 'alpha', excerpt: 'First excerpt', content: 'First body', category: 'Security',
  tags: ['systems'], isPublished: true, createdAtUtc: '2026-09-22T10:00:00Z', ...over,
});

describe('BlogPage', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<BlogPage>;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const el = () => fixture.nativeElement as HTMLElement;
  // The Markdown renderer is loaded on first reading (dynamic import), so wait until the body has been rendered.
  const rendered = async () => {
    for (let i = 0; i < 100 && !el().querySelector('.content')?.children.length; i++) {
      await new Promise((resolve) => setTimeout(resolve, 20));
      fixture.detectChanges();
    }
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(BlogPage);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('uses a neutral heading and intro that claim nothing', () => {
    http.expectOne('/api/blog').flush([]);
    fixture.detectChanges();
    expect(el().querySelector('h2')?.textContent?.trim()).toBe('Stories');
    expect(el().querySelector('.intro .gcv-lede')?.textContent?.trim()).toBe('Stories Gustavo has published.');
    expect(text()).not.toContain('Signals from');
    expect(text()).not.toContain('keep me curious');
  });

  it('shows a loading state and no story before the API answers', () => {
    expect(text()).toContain('Loading stories');
    expect(el().querySelectorAll('.post').length).toBe(0);
    http.expectOne('/api/blog').flush([]);
  });

  it('says in words that nothing is published when the API returns none', () => {
    http.expectOne('/api/blog').flush([]);
    fixture.detectChanges();
    expect(text()).toContain('Stories will appear here as soon as Gustavo publishes them.');
    expect(text()).not.toContain('Loading stories');
  });

  it('says in words that loading failed and keeps the section usable', () => {
    http.expectOne('/api/blog').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(text()).toContain('Stories could not be loaded');
    expect(el().querySelector('input[type="search"]')).not.toBeNull();
  });

  describe('with two published stories', () => {
    beforeEach(() => {
      http.expectOne('/api/blog').flush([
        apiStory({}),
        apiStory({ id: 2, title: 'Beta', slug: 'beta', excerpt: 'Second excerpt', content: 'Second body', category: 'Build log', tags: ['tools'] }),
      ]);
      fixture.detectChanges();
    });

    it('lists both and builds the categories from the data', () => {
      expect(el().querySelectorAll('.post').length).toBe(2);
      const filters = Array.from(el().querySelectorAll('.filter')).map((b) => b.textContent?.trim());
      expect(filters).toEqual(['All', 'Build log', 'Security']);
    });

    it('filters by search text', () => {
      const input = el().querySelector('input[type="search"]') as HTMLInputElement;
      input.value = 'tools';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      expect(el().querySelectorAll('.post').length).toBe(1);
      expect(text()).toContain('Beta');
      input.value = 'nomatch';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      expect(text()).toContain('No story found');
    });

    it('filters by category', () => {
      const button = Array.from(el().querySelectorAll('.filter')).find((b) => b.textContent?.trim() === 'Security') as HTMLButtonElement;
      button.click();
      fixture.detectChanges();
      expect(el().querySelectorAll('.post').length).toBe(1);
      expect(text()).toContain('Alpha');
    });

    it('opens the reading view and goes back', async () => {
      (el().querySelector('.post') as HTMLButtonElement).click();
      fixture.detectChanges();
      await rendered();
      expect(el().querySelector('.detail')).not.toBeNull();
      expect(text()).toContain('First body');
      (el().querySelector('.back') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(el().querySelectorAll('.post').length).toBe(2);
    });
  });

  // TE4-AC7: the Competitions card pre-applies the tag `competition` through the shared StoryFilter.
  describe('tag filter from the Competitions card', () => {
    beforeEach(() => {
      http.expectOne('/api/blog').flush([
        apiStory({}),
        apiStory({ id: 2, title: 'Beta', slug: 'beta', tags: ['Competition', 'tools'] }),
        apiStory({ id: 3, title: 'Gamma', slug: 'gamma', tags: ['competitions-not'] }),
      ]);
      fixture.detectChanges();
    });

    it('shows every story while no tag is set', () => {
      expect(el().querySelectorAll('.post').length).toBe(3);
      expect(el().querySelector('.tag-filter')).toBeNull();
    });

    it('shows only the stories tagged competition (case-insensitive) once the tag is set, and says so', () => {
      TestBed.inject(StoryFilter).tag.set('competition');
      fixture.detectChanges();
      expect(el().querySelectorAll('.post').length).toBe(1);
      expect(text()).toContain('Beta');
      expect(el().querySelector('.tag-filter')?.textContent).toContain('competition');
    });

    it('clears the tag from the clear button', () => {
      TestBed.inject(StoryFilter).tag.set('competition');
      fixture.detectChanges();
      (el().querySelector('.tag-filter button') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(TestBed.inject(StoryFilter).tag()).toBe('');
      expect(el().querySelectorAll('.post').length).toBe(3);
    });

    // O6: with a tag set, an empty result names the tag instead of blaming the search.
    it('says no stories carry the tag when none matches, and keeps the clear button', () => {
      TestBed.inject(StoryFilter).tag.set('nothing-here');
      fixture.detectChanges();
      expect(el().querySelectorAll('.post').length).toBe(0);
      expect(text()).toContain('No stories tagged #nothing-here yet.');
      expect(text()).not.toContain('No story found');
      expect(el().querySelector('.tag-filter button')).not.toBeNull();
    });

    it('still says "No story found" for a search with no match while no tag is set', () => {
      const input = el().querySelector('input[type="search"]') as HTMLInputElement;
      input.value = 'nomatch';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      expect(text()).toContain('No story found. Try another search.');
      expect(text()).not.toContain('No stories tagged');
    });

    it('does not show the tag status line in the reading view, and shows it again on the list', async () => {
      TestBed.inject(StoryFilter).tag.set('competition');
      fixture.detectChanges();
      expect(el().querySelector('.tag-filter')).not.toBeNull();
      (el().querySelector('.post') as HTMLButtonElement).click();
      fixture.detectChanges();
      await rendered();
      expect(el().querySelector('.detail')).not.toBeNull();
      expect(el().querySelector('.tag-filter')).toBeNull();
      (el().querySelector('.back') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(el().querySelector('.tag-filter')).not.toBeNull();
    });
  });

  it('renders the Markdown body safely in the reading view', async () => {
    http.expectOne('/api/blog').flush([
      apiStory({ content: '## Heading\n\nSome **bold** text <script>window.__pwned = 1</script>\n\n[bad](javascript:alert(1))' }),
    ]);
    fixture.detectChanges();
    (el().querySelector('.post') as HTMLButtonElement).click();
    await rendered();
    const body = el().querySelector('.content') as HTMLElement;
    expect(body.querySelector('h2')?.textContent).toBe('Heading');
    expect(body.querySelector('strong')?.textContent).toBe('bold');
    expect(body.querySelector('script')).toBeNull();
    expect((window as unknown as Record<string, unknown>)['__pwned']).toBeUndefined();
    expect(body.querySelector('a[href^="javascript"]')).toBeNull();
  });
});
