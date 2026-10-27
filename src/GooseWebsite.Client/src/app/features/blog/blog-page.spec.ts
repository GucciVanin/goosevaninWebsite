import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
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
