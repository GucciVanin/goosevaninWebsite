import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Categories } from './categories/categories';
import { Hero } from './hero/hero';

// The landing page follows the mockup: headline, three-line intro, one action, and three category cards.
describe('Landing page', () => {
  it('shows the headline, the eyebrow, the three-line intro and the Explore action', () => {
    const fixture = TestBed.createComponent(Hero);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h1')?.textContent?.replace(/\s+/g, ' ').trim()).toBe('Software Engineer');
    expect(element.textContent).toContain('Build · Break · Improve');
    expect(element.querySelector('.intro')?.textContent?.replace(/\s+/g, ' ').trim()).toBe('Curious mind. Practical solutions. Real impact.');

  });

  it('takes the visitor to the work section from Explore my work', () => {
    const fixture = TestBed.createComponent(Hero);
    const targets: string[] = [];
    fixture.componentInstance.navigate.subscribe((id) => targets.push(id));
    fixture.detectChanges();

    (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('a.gcv-btn')!.click();

    expect(targets).toEqual(['work']);
  });

  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'projects', children: [] }, { path: 'stories', children: [] }])] }));

  it('shows the three category cards, each with an image strip and a link to its own page', () => {
    const fixture = TestBed.createComponent(Categories);
    fixture.detectChanges();
    const cards = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li'));

    expect(cards.map((card) => card.querySelector('.gcv-heading')?.textContent?.trim())).toEqual(['Projects', 'Life stories', 'Competitions']);
    expect(cards.every((card) => card.querySelector('img')?.getAttribute('alt'))).toBeTrue();
    expect(cards.every((card) => card.querySelector('svg'))).toBeTrue();
    expect(cards.map((card) => card.getAttribute('style'))).toEqual([
      jasmine.stringContaining('--gcv-magenta'), jasmine.stringContaining('--gcv-cyan'), jasmine.stringContaining('--gcv-orange'),
    ]);
    expect(cards.map((card) => card.querySelector('img')?.getAttribute('src'))).toEqual([
      '/brand/cards/projects.webp', '/brand/cards/life-stories.webp', '/brand/cards/competitions.webp',
    ]);

  });

  it('opens the projects page from the Projects card', async () => {
    const fixture = TestBed.createComponent(Categories);
    fixture.detectChanges();
    const router = TestBed.inject(Router);

    (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('a')!.click();
    await fixture.whenStable();

    expect(router.url).toBe('/projects');
  });

  it('never links to a removed section', () => {
    const fixture = TestBed.createComponent(Categories);
    fixture.detectChanges();
    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a')).map((a) => a.getAttribute('href'));

    expect(hrefs).toEqual(['/projects', '/stories', '/stories?tag=competition']);
  });

  it('opens the stories page from the Life stories card and the tagged stories from the Competitions card', async () => {
    const fixture = TestBed.createComponent(Categories);
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a'));

    links[2].click();
    await fixture.whenStable();
    expect(router.url).toBe('/stories?tag=competition');
    links[1].click();
    await fixture.whenStable();
    expect(router.url).toBe('/stories');
  });
});
