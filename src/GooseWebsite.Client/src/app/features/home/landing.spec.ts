import { TestBed } from '@angular/core/testing';
import { Categories } from './categories/categories';
import { Hero } from './hero/hero';

// The landing page follows the mockup: headline, three-line intro, one action, and four category cards.
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

  it('shows the four category cards, each with an image strip and a link to its section', () => {
    const fixture = TestBed.createComponent(Categories);
    const targets: string[] = [];
    fixture.componentInstance.navigate.subscribe((id) => targets.push(id));
    fixture.detectChanges();
    const cards = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li'));

    expect(cards.map((card) => card.querySelector('.gcv-heading')?.textContent?.trim())).toEqual(['Applications', 'Cloud & DevOps', 'Security', 'Systems & Tools']);
    expect(cards.every((card) => card.querySelector('img')?.getAttribute('alt'))).toBeTrue();

    cards[0].querySelector('a')!.click();
    cards[2].querySelector('a')!.click();
    expect(targets).toEqual(['work', 'skills']);
  });
});
