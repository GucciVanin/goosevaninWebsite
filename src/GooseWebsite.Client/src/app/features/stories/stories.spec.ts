import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, TitleStrategy, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { SiteTitleStrategy } from '../../shared/site-title-strategy';
import { NotFoundPage } from '../not-found/not-found-page';
import { StoriesIndexPage } from './stories-index-page';
import { StoryPage } from './story-page';

// F5-US2: the visitor browses the published stories and reads one in full. The API is stubbed; it only ever sends
// published stories, so the pages never filter drafts. The stories here are invented for the tests.
const apiStory = (over: Record<string, unknown> = {}) => ({
  id: 1, title: 'Alpha', slug: 'alpha', excerpt: 'First excerpt', content: 'First **body**.\n\nSecond paragraph.', category: 'Security',
  tags: ['systems'], coverImage: '', coverImageAlt: '', isPublished: true,
  createdAtUtc: '2026-09-20T10:00:00Z', updatedAtUtc: '2026-09-20T10:00:00Z', publishedAtUtc: '2026-09-22T10:00:00Z', authorId: 'a', authorName: 'Gustavo', ...over,
});
const alpha = apiStory();
const beta = apiStory({ id: 2, title: 'Beta', slug: 'beta', excerpt: 'Second excerpt', category: 'Build log', tags: ['Competition', 'tools'], publishedAtUtc: null, createdAtUtc: '2026-08-01T10:00:00Z' });
const gamma = apiStory({ id: 3, title: 'Gamma', slug: 'gamma', excerpt: 'Third excerpt', tags: ['competitions-not'] });

function setup() {
  TestBed.configureTestingModule({ providers: [provideRouter(routes), { provide: TitleStrategy, useClass: SiteTitleStrategy }, provideHttpClient(), provideHttpClientTesting()] });
  return { http: TestBed.inject(HttpTestingController) };
}

describe('StoriesIndexPage', () => {
  async function open(url = '/stories') {
    const { http } = setup();
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl(url, StoriesIndexPage);
    const el = () => harness.routeNativeElement as HTMLElement;
    const flush = (items: unknown[]) => {
      http.expectOne((request) => request.url === '/api/blog').flush(items);
      harness.detectChanges();
    };
    return { harness, http, page, el, flush };
  }
  const entries = (el: HTMLElement) => Array.from(el.querySelectorAll('li.entry'));

  it('lists every story it receives as a link to its page, with title, date and tags, in compact unframed entries (AC1)', async () => {
    const { el, flush } = await open();
    flush([alpha, beta]);

    expect(el().querySelector('h1')?.textContent).toContain('Stories');
    const list = entries(el());
    expect(list.length).toBe(2);
    expect(list[0].querySelector('a')?.getAttribute('href')).toBe('/stories/alpha');
    expect(list[0].textContent).toContain('Alpha');
    expect(list[0].textContent).toContain('22 Sep 2026');
    expect(list[0].querySelector('time')?.getAttribute('datetime')).toBe('2026-09-22T10:00:00Z');
    expect(list[0].textContent).toContain('First excerpt');
    expect(list[0].textContent).toContain('Security');
    expect(Array.from(list[0].querySelectorAll('.gcv-chip')).map((c) => c.textContent?.trim())).toEqual(['#systems']);
    // A story with no publication date shows the date it was created.
    expect(list[1].textContent).toContain('1 Aug 2026');
    const style = getComputedStyle(list[0] as HTMLElement);
    expect(style.borderLeftWidth).toBe('0px');
    expect(style.borderRightWidth).toBe('0px');
    expect(style.borderBottomWidth).toBe('0px');
    expect(style.borderTopLeftRadius).toBe('0px');
    expect(style.backgroundColor).toBe('rgba(0, 0, 0, 0)');
  });

  it('has a neutral search placeholder that suggests no topics', async () => {
    const { el, flush } = await open();
    flush([alpha]);

    expect((el().querySelector('input[type="search"]') as HTMLInputElement).placeholder).toBe('Search by title or tag');
  });

  it('says in words that nothing is published yet', async () => {
    const { el, flush } = await open();
    flush([]);

    expect(el().textContent).toContain('Stories will appear here as soon as Gustavo publishes them.');
    expect(entries(el()).length).toBe(0);
  });

  it('says in words that it is loading, then that loading failed, and keeps the search usable', async () => {
    const { harness, http, el } = await open();
    harness.detectChanges();
    expect(el().textContent).toContain('Loading stories');

    http.expectOne((request) => request.url === '/api/blog').flush('x', { status: 500, statusText: 'Server Error' });
    harness.detectChanges();
    expect(el().textContent).toContain('Stories could not be loaded');
    expect(el().querySelector('input[type="search"]')).not.toBeNull();
  });

  describe('search and category', () => {
    it('builds the category buttons from the data and filters by one', async () => {
      const { harness, el, flush } = await open();
      flush([alpha, beta]);

      expect(Array.from(el().querySelectorAll('.filter')).map((b) => b.textContent?.trim())).toEqual(['All', 'Build log', 'Security']);
      (Array.from(el().querySelectorAll('.filter')).find((b) => b.textContent?.trim() === 'Security') as HTMLButtonElement).click();
      harness.detectChanges();
      expect(entries(el()).length).toBe(1);
      expect(el().textContent).toContain('Alpha');
    });

    it('filters by search text and says when nothing matches', async () => {
      const { harness, el, flush } = await open();
      flush([alpha, beta]);
      const input = el().querySelector('input[type="search"]') as HTMLInputElement;

      input.value = 'tools';
      input.dispatchEvent(new Event('input'));
      harness.detectChanges();
      expect(entries(el()).length).toBe(1);
      expect(el().textContent).toContain('Beta');

      input.value = 'nomatch';
      input.dispatchEvent(new Event('input'));
      harness.detectChanges();
      expect(el().textContent).toContain('No story found. Try another search.');
    });
  });

  // F5-US2 / decision D2: Competitions is /stories?tag=competition; the tag is the whole filter state.
  describe('the tag query parameter', () => {
    it('shows every story and no tag line without a tag', async () => {
      const { el, flush } = await open();
      flush([alpha, beta, gamma]);

      expect(entries(el()).length).toBe(3);
      expect(el().querySelector('.tag-filter')).toBeNull();
    });

    it('shows only the stories with that exact tag, ignoring case, and says so', async () => {
      const { el, flush } = await open('/stories?tag=competition');
      flush([alpha, beta, gamma]);

      expect(entries(el()).length).toBe(1);
      expect(el().textContent).toContain('Beta');
      expect(el().querySelector('.tag-filter')?.textContent).toContain('Showing stories tagged #competition');
    });

    it('offers "Show all stories", which drops the tag from the address and lists everything', async () => {
      const { harness, el, flush } = await open('/stories?tag=competition');
      flush([alpha, beta, gamma]);

      (el().querySelector('.tag-filter a, .tag-filter button') as HTMLElement).click();
      await harness.fixture.whenStable();
      harness.detectChanges();

      expect(TestBed.inject(Router).url).toBe('/stories');
      expect(entries(el()).length).toBe(3);
      expect(el().querySelector('.tag-filter')).toBeNull();
    });

    it('names the tag when no story carries it, and keeps the way back', async () => {
      const { el, flush } = await open('/stories?tag=nothing-here');
      flush([alpha, beta]);

      expect(entries(el()).length).toBe(0);
      expect(el().textContent).toContain('No stories tagged #nothing-here yet.');
      expect(el().textContent).not.toContain('No story found');
      expect(el().querySelector('.tag-filter a, .tag-filter button')).not.toBeNull();
    });

    it('follows a change of the tag in the address without reloading the list', async () => {
      const { harness, el, flush } = await open('/stories');
      flush([alpha, beta, gamma]);

      await harness.navigateByUrl('/stories?tag=tools');
      harness.detectChanges();

      expect(entries(el()).length).toBe(1);
      expect(el().textContent).toContain('Beta');
    });
  });
});

describe('StoryPage', () => {
  async function open(url = '/stories/alpha') {
    const { http } = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, StoryPage);
    return { harness, http, el: () => harness.routeNativeElement as HTMLElement };
  }

  it('shows the title first, then the body, with the date and tags after it (AC2)', async () => {
    const { harness, http, el } = await open();
    http.expectOne('/api/blog/alpha').flush(alpha);
    harness.detectChanges();
    const main = el().querySelector('main') as HTMLElement;

    const markers = ['h1', '.body', '.meta time', '.tags'];
    const positions = markers.map((selector) => {
      const found = main.querySelector(selector);
      expect(found).withContext(selector).not.toBeNull();
      return Array.from(main.querySelectorAll('*')).indexOf(found!);
    });
    expect(main.querySelector('h1')?.textContent).toContain('Alpha');
    // The metadata sits after the story, not above it.
    expect(positions[0]).toBeLessThan(positions[1]);
    expect(positions[1]).toBeLessThan(positions[2]);
    expect(main.querySelector('.body')?.innerHTML).toContain('<strong>body</strong>');
    expect(main.querySelector('.meta time')?.textContent).toContain('22 Sep 2026');
    expect(Array.from(main.querySelectorAll('.tags .gcv-chip')).map((c) => c.textContent?.trim())).toEqual(['#systems']);
    expect(main.querySelector('img.cover')).toBeNull();
    expect(el().querySelector('a[href="/stories"]')?.textContent).toContain('All stories');
  });

  it('shows the cover image with its alt text when the story has one', async () => {
    const { harness, http, el } = await open();
    http.expectOne('/api/blog/alpha').flush(apiStory({ coverImage: '/media/cover.webp', coverImageAlt: 'A desk at dusk' }));
    harness.detectChanges();
    const cover = el().querySelector('img.cover') as HTMLImageElement;

    expect(cover.getAttribute('src')).toBe('/media/cover.webp');
    expect(cover.getAttribute('alt')).toBe('A desk at dusk');
  });

  it('renders the Markdown body safely', async () => {
    const { harness, http, el } = await open();
    http.expectOne('/api/blog/alpha').flush(apiStory({ content: '# Heading\n\nSome **bold** <script>window.__pwned = 1</script>\n\n[bad](javascript:alert(1))' }));
    harness.detectChanges();
    const body = el().querySelector('.body') as HTMLElement;

    // A "#" in the body must not add a second h1 to the page: it becomes an h2.
    // A "#" in the body must not add a second h1 to the page: it becomes an h2.
    expect(body.querySelector('h2')?.textContent).toBe('Heading');
    expect(el().querySelectorAll('h1').length).toBe(1);
    expect(el().querySelectorAll('h1').length).toBe(1);
    expect(body.querySelector('strong')?.textContent).toBe('bold');
    expect(body.querySelector('script')).toBeNull();
    expect((window as unknown as Record<string, unknown>)['__pwned']).toBeUndefined();
    expect(body.querySelector('a[href^="javascript"]')).toBeNull();
  });

  it('shows the not-found page when the API does not know the slug, which covers a draft (AC3)', async () => {
    const { harness, http, el } = await open('/stories/nope');
    http.expectOne('/api/blog/nope').flush('', { status: 404, statusText: 'Not Found' });
    harness.detectChanges();

    expect(harness.routeDebugElement?.query((d) => d.componentInstance instanceof NotFoundPage)).toBeTruthy();
    expect(el().querySelector('h1')?.textContent).toContain('Page not found');
    expect(el().querySelector('a[href="/"]')).not.toBeNull();
  });

  it('says so when loading fails for another reason, and does not call that not found', async () => {
    const { harness, http, el } = await open();
    http.expectOne('/api/blog/alpha').flush('x', { status: 500, statusText: 'Server Error' });
    harness.detectChanges();

    expect(el().textContent).toContain('could not be loaded');
    expect(el().textContent).not.toContain('Page not found');
  });

  it('sets the document title to the story title once it has loaded', async () => {
    const { harness, http } = await open();
    http.expectOne('/api/blog/alpha').flush(alpha);
    harness.detectChanges();

    expect(TestBed.inject(Title).getTitle()).toBe('Alpha - Gustavo Couto Vanin');
  });
});

describe('story routes', () => {
  it('declares lazy /stories and /stories/:slug routes that carry a title, ahead of the not-found wildcard', () => {
    const paths = routes.map((route) => route.path);
    const index = routes.find((route) => route.path === 'stories');
    const detail = routes.find((route) => route.path === 'stories/:slug');

    expect(index?.loadComponent).toBeDefined();
    expect(detail?.loadComponent).toBeDefined();
    expect(index?.title).toBe('Stories');
    expect(detail?.title).toBe('Story');
    expect(paths.indexOf('stories')).toBeLessThan(paths.indexOf('**'));
    expect(paths.indexOf('stories/:slug')).toBeLessThan(paths.indexOf('**'));
  });

  it('titles the index page', async () => {
    const { http } = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/stories', StoriesIndexPage);
    http.expectOne((request) => request.url === '/api/blog').flush([]);

    expect(TestBed.inject(Title).getTitle()).toBe('Stories - Gustavo Couto Vanin');
  });
});
