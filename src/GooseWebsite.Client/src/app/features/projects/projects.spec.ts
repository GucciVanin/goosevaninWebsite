import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { TitleStrategy, provideRouter } from '@angular/router';
import { SiteTitleStrategy } from '../../shared/site-title-strategy';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { NotFoundPage } from '../not-found/not-found-page';
import { ProjectPage } from './project-page';
import { ProjectsIndexPage } from './projects-index-page';

// F5-US1: the visitor browses the published projects and reads one in full. The API is stubbed; it only ever sends
// published projects, so the pages never filter.
const goose = {
  id: 1, title: 'Goose website', slug: 'goose-website', overview: 'A **personal** site.\n\nIt has a [blog](https://example.test).',
  contribution: 'I built all of it.', technologies: ['C#', 'Angular'], demoUrl: 'https://demo.example.test', sourceUrl: 'https://source.example.test',
  coverImage: '/media/cover.webp', coverImageAlt: 'The home page of the site', isPublished: true,
  createdAtUtc: '2026-10-01T00:00:00Z', updatedAtUtc: '2026-10-01T00:00:00Z', publishedAtUtc: '2026-10-02T00:00:00Z',
};
const second = { ...goose, id: 2, title: 'Second project', slug: 'second-project', overview: 'x '.repeat(200), coverImage: '', coverImageAlt: '', demoUrl: null, sourceUrl: null };

function setup() {
  TestBed.configureTestingModule({ providers: [provideRouter(routes), { provide: TitleStrategy, useClass: SiteTitleStrategy }, provideHttpClient(), provideHttpClientTesting()] });
  return { http: TestBed.inject(HttpTestingController) };
}

describe('ProjectsIndexPage', () => {
  async function open() {
    const { http } = setup();
    const harness = await RouterTestingHarness.create();
    const page = harness.navigateByUrl('/projects', ProjectsIndexPage);
    return { harness, http, page };
  }

  it('lists every project it receives as a link to its page, with title, one-line overview, technologies and a thumbnail (AC1)', async () => {
    const { harness, http, page } = await open();
    await page;
    http.expectOne('/api/projects').flush([goose, second]);
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;
    const entries = Array.from(element.querySelectorAll('li.entry'));

    expect(element.querySelector('h1')?.textContent).toContain('Projects');
    expect(entries.length).toBe(2);
    expect(entries[0].querySelector('a')?.getAttribute('href')).toBe('/projects/goose-website');
    expect(entries[0].textContent).toContain('Goose website');
    expect(entries[0].textContent).toContain('A personal site. It has a blog.');
    expect(entries[0].querySelectorAll('.gcv-chip').length).toBe(2);
    expect(entries[0].querySelector('img')?.getAttribute('src')).toBe('/media/cover.webp');
    expect(entries[1].querySelector('img')).toBeNull();
    expect((entries[1].querySelector('.overview') as HTMLElement).textContent!.length).toBeLessThan(200);
  });

  it('does not cut an emoji in half when it shortens the overview', async () => {
    const { http, harness, page } = await open();
    await page;
    http.expectOne('/api/projects').flush([{ ...goose, overview: '👨‍👩‍👧'.repeat(100) }]);
    harness.detectChanges();
    const text = (harness.routeNativeElement as HTMLElement).querySelector('.overview')?.textContent ?? '';

    expect(text).not.toMatch(/[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]/);
  });

  it('keeps the entries compact and unframed (AC1)', async () => {
    const { harness, http, page } = await open();
    await page;
    http.expectOne('/api/projects').flush([goose]);
    harness.detectChanges();
    const entry = (harness.routeNativeElement as HTMLElement).querySelector('li.entry') as HTMLElement;
    const style = getComputedStyle(entry);

    // No box: no side or bottom border, no radius, no fill. A hairline above separates one entry from the next.
    expect(style.borderLeftWidth).toBe('0px');
    expect(style.borderRightWidth).toBe('0px');
    expect(style.borderBottomWidth).toBe('0px');
    expect(style.borderTopLeftRadius).toBe('0px');
    expect(style.backgroundColor).toBe('rgba(0, 0, 0, 0)');
  });

  it('says in words that nothing is published yet', async () => {
    const { harness, http, page } = await open();
    await page;
    http.expectOne('/api/projects').flush([]);
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.textContent).toContain('Projects will appear here');
    expect(element.querySelectorAll('li.entry').length).toBe(0);
  });

  it('says in words that it is loading, then that loading failed', async () => {
    const { harness, http, page } = await open();
    await page;
    harness.detectChanges();
    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('Loading projects');

    http.expectOne('/api/projects').flush('x', { status: 500, statusText: 'Server Error' });
    harness.detectChanges();
    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('could not be loaded');
  });
});

describe('ProjectPage', () => {
  async function open(url = '/projects/goose-website') {
    const { http } = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, ProjectPage);
    return { harness, http };
  }

  it('shows title, overview, contribution, technologies, cover image and links, in that order (AC2)', async () => {
    const { harness, http } = await open();
    http.expectOne('/api/projects/goose-website').flush(goose);
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;
    const main = element.querySelector('main') as HTMLElement;

    expect(main.querySelector('h1')?.textContent).toContain('Goose website');
    const markers = ['h1', '.overview', '.contribution', '.technologies', 'img.cover', '.links'];
    const positions = markers.map((selector) => {
      const found = main.querySelector(selector);
      expect(found).withContext(selector).not.toBeNull();
      return Array.from(main.querySelectorAll('*')).indexOf(found!);
    });
    expect(positions).toEqual([...positions].sort((a, b) => a - b));
    expect(main.querySelector('.overview')?.innerHTML).toContain('<strong>personal</strong>');
    expect(main.querySelector('.contribution')?.textContent).toContain('I built all of it.');
    expect(main.querySelectorAll('.technologies .gcv-chip').length).toBe(2);
    expect(main.querySelector('img.cover')?.getAttribute('alt')).toBe('The home page of the site');
  });

  it('gives the overview the same reading styles as a story body', async () => {
    const { harness, http } = await open();
    http.expectOne('/api/projects/goose-website').flush({ ...goose, overview: 'Quote:\n\n> wise words\n\n`code`' });
    harness.detectChanges();
    const overview = (harness.routeNativeElement as HTMLElement).querySelector('.overview') as HTMLElement;

    expect(overview.classList).toContain('gcv-reading');
    const quote = overview.querySelector('blockquote') as HTMLElement;
    expect(getComputedStyle(quote).borderLeftWidth).toBe('2px');
    expect(getComputedStyle(overview.querySelector('code') as HTMLElement).fontFamily).toContain('mono');
  });

  it('shows an animated GIF as the cover and inline in the overview, each with its alt text, and keeps emojis whole', async () => {
    const emojis = '👨‍👩‍👧 🇧🇷 👍🏽 1️⃣ ❤️';
    const { harness, http } = await open();
    http.expectOne('/api/projects/goose-website').flush({ ...goose, title: `P ${emojis}`, overview: `O ${emojis}\n\n![A demo loop](/uploads/fedcba9876543210fedcba9876543210.gif)`, coverImage: '/uploads/0123456789abcdef0123456789abcdef.gif', coverImageAlt: 'A looping demo' });
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.querySelector('img.cover')?.getAttribute('src')).toBe('/uploads/0123456789abcdef0123456789abcdef.gif');
    expect(element.querySelector('img.cover')?.getAttribute('alt')).toBe('A looping demo');
    expect(element.querySelector('.overview img')?.getAttribute('alt')).toBe('A demo loop');
    expect(element.querySelector('h1')?.textContent).toBe(`P ${emojis}`);
    expect(element.querySelector('.overview')?.textContent).toContain(`O ${emojis}`);
    expect(TestBed.inject(Title).getTitle()).toBe(`P ${emojis} - Gustavo Couto Vanin`);
  });

  it('keeps a single h1 when the overview has a "#" heading', async () => {
    const { harness, http } = await open();
    http.expectOne('/api/projects/goose-website').flush({ ...goose, overview: '# Big heading\n\ntext' });
    harness.detectChanges();
    const main = (harness.routeNativeElement as HTMLElement).querySelector('main') as HTMLElement;

    expect(main.querySelectorAll('h1').length).toBe(1);
    expect(main.querySelector('.overview h2')?.textContent).toBe('Big heading');
  });

  it('marks the demo and source as external links that say so (AC2)', async () => {
    const { harness, http } = await open();
    http.expectOne('/api/projects/goose-website').flush(goose);
    harness.detectChanges();
    const links = Array.from((harness.routeNativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('.links a'));

    expect(links.map((a) => a.getAttribute('href'))).toEqual(['https://demo.example.test', 'https://source.example.test']);
    for (const link of links) {
      expect(link.getAttribute('rel')).toContain('noopener');
      expect(link.getAttribute('target')).toBe('_blank');
      expect(link.textContent).toContain('opens in a new tab');
      expect(link.classList).toContain('gcv-link');
    }
  });

  it('leaves out the cover image and the links a project does not have', async () => {
    const { harness, http } = await open('/projects/second-project');
    http.expectOne('/api/projects/second-project').flush(second);
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.querySelector('img.cover')).toBeNull();
    expect(element.querySelector('.links')).toBeNull();
  });

  it('shows the not-found page when the API answers 404 (AC2)', async () => {
    const { harness, http } = await open('/projects/nope');
    http.expectOne('/api/projects/nope').flush(null, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.querySelector('app-not-found-page')).not.toBeNull();
    expect(element.querySelector('h1')?.textContent).toContain('Page not found');
  });

  it('says in words when the project cannot be loaded for another reason', async () => {
    const { harness, http } = await open();
    http.expectOne('/api/projects/goose-website').flush('x', { status: 500, statusText: 'Server Error' });
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.textContent).toContain('could not be loaded');
    expect(element.querySelector('app-not-found-page')).toBeNull();
  });
});

describe('NotFoundPage', () => {
  it('has a main landmark, one heading and a link home', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(NotFoundPage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('main h1')?.textContent).toContain('Page not found');
    expect(element.querySelectorAll('h1').length).toBe(1);
    expect(element.querySelector('a[href="/"]')?.textContent).toContain('home');
  });
});

describe('routes', () => {
  it('opens the lazy project pages and falls back to the not-found page for any unknown URL', async () => {
    const { http } = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/projects');
    http.expectOne('/api/projects').flush([]);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Projects');

    await harness.navigateByUrl('/projects/goose-website');
    http.expectOne('/api/projects/goose-website').flush(goose);
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Goose website');

    await harness.navigateByUrl('/definitely/not/a/page');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Page not found');
  });
});

// WCAG 2.4.2: each page has its own title, so the tab and a screen reader say where the visitor is.
describe('page titles', () => {
  it('titles the projects index, a project page (by the project title) and the not-found page', async () => {
    const { http } = setup();
    const title = TestBed.inject(Title);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/projects');
    http.expectOne('/api/projects').flush([]);
    expect(title.getTitle()).toBe('Projects - Gustavo Couto Vanin');

    await harness.navigateByUrl('/projects/goose-website');
    expect(title.getTitle()).toBe('Project - Gustavo Couto Vanin');
    http.expectOne('/api/projects/goose-website').flush(goose);
    harness.detectChanges();
    expect(title.getTitle()).toBe('Goose website - Gustavo Couto Vanin');

    await harness.navigateByUrl('/projects/nope');
    http.expectOne('/api/projects/nope').flush(null, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();
    expect(title.getTitle()).toBe('Page not found - Gustavo Couto Vanin');

    await harness.navigateByUrl('/definitely/not/a/page');
    expect(title.getTitle()).toBe('Page not found - Gustavo Couto Vanin');
  });

  it('restores the site title on the home page', async () => {
    const { http } = setup();
    const title = TestBed.inject(Title);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/projects');
    http.expectOne('/api/projects').flush([]);

    await harness.navigateByUrl('/');

    expect(title.getTitle()).toBe('Gustavo Couto Vanin');
  });
});
