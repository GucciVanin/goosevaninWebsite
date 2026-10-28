import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BlogPage } from './blog-page';

// F5-US2: the home Stories section is a preview (the latest three and a link to all of them); reading, search and the
// filters live on /stories. These specs use a stubbed API with invented titles of their own.
const apiStory = (over: Record<string, unknown>) => ({
  id: 1, title: 'Alpha', slug: 'alpha', excerpt: 'First excerpt', content: 'First body', category: 'Security',
  tags: ['systems'], isPublished: true, createdAtUtc: '2026-09-22T10:00:00Z', publishedAtUtc: '2026-09-22T10:00:00Z', ...over,
});

describe('BlogPage (home Stories preview)', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<BlogPage>;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const el = () => fixture.nativeElement as HTMLElement;
  const flush = (items: unknown[]) => {
    http.expectOne((request) => request.url === '/api/blog').flush(items);
    fixture.detectChanges();
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(BlogPage);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('uses a neutral heading and intro that claim nothing', () => {
    flush([]);
    expect(el().querySelector('h2')?.textContent?.trim()).toBe('Stories');
    expect(el().querySelector('.intro .gcv-lede')?.textContent?.trim()).toBe('Stories Gustavo has published.');
    expect(text()).not.toContain('Signals from');
  });

  it('asks the API for the latest three only', () => {
    const request = http.expectOne((r) => r.url === '/api/blog');
    expect(request.request.params.get('pageSize')).toBe('3');
    request.flush([]);
  });

  it('shows a loading state and no story before the API answers', () => {
    expect(text()).toContain('Loading stories');
    expect(el().querySelectorAll('.post').length).toBe(0);
    flush([]);
  });

  it('says in words that nothing is published when the API returns none, and offers no link to an empty page', () => {
    flush([]);
    expect(text()).toContain('Stories will appear here as soon as Gustavo publishes them.');
    expect(el().querySelector('a.all-stories')).toBeNull();
  });

  it('says in words that loading failed', () => {
    http.expectOne((r) => r.url === '/api/blog').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(text()).toContain('Stories could not be loaded');
  });

  it('previews at most three stories as links to their own pages, with category, date and tags', () => {
    const story = (n: number) => apiStory({ id: n, title: `Story ${n}`, slug: `story-${n}` });
    flush([1, 2, 3, 4].map(story));

    const posts = Array.from(el().querySelectorAll('.post'));
    expect(posts.length).toBe(3);
    expect(posts[0].tagName).toBe('A');
    expect(posts[0].getAttribute('href')).toBe('/stories/story-1');
    expect(posts[0].textContent).toContain('Story 1');
    expect(posts[0].textContent).toContain('Security');
    expect(posts[0].textContent).toContain('22 Sep 2026');
    expect(posts[0].textContent).toContain('#systems');
  });

  it('links to all stories', () => {
    flush([apiStory({})]);
    const link = el().querySelector('a.all-stories') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toBe('/stories');
    expect(link.textContent).toContain('All stories');
  });

  it('has no in-page reading view, search or filter any more', () => {
    flush([apiStory({})]);
    expect(el().querySelector('.detail')).toBeNull();
    expect(el().querySelector('input[type="search"]')).toBeNull();
    expect(el().querySelector('.filter')).toBeNull();
    expect(el().querySelector('button.post')).toBeNull();
  });
});
