import { TestBed } from '@angular/core/testing';
import { profile } from '../../../shared/models/profile';
import { Approach } from './approach';

// F9-US7-AC6 (F1-US1 tests 1 and 2): About shows who Gustavo is in visible text, from the shared profile.
describe('About (Approach)', () => {
  function render(): HTMLElement {
    const fixture = TestBed.createComponent(Approach);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the name as a caption beside the portrait, visibly', () => {
    const caption = render().querySelector('figure.portrait figcaption');

    expect(caption?.textContent).toContain(profile.name);
    expect(caption?.closest('.sr-only')).toBeNull();
  });

  it('shows the positioning line, the short bio and the professional summary in visible text', () => {
    const element = render();
    const text = element.textContent!.replace(/\s+/g, ' ');

    expect(text).toContain(profile.positioning);
    expect(text).toContain(profile.bio);
    expect(text).toContain(profile.professionalSummary);
    expect(element.querySelectorAll('.sr-only').length).toBe(0);
  });

  it('shows at most the broad location and no age or street address', () => {
    const text = render().textContent!;

    expect(text).not.toMatch(/\b\d{1,3}\s*years?\s*old\b/i);
    expect(text).not.toMatch(/\b\d{1,5}\s+\w+\s+(Street|St|Avenue|Ave|Road|Rd|Drive|Dr)\b/);
    expect(profile.location).toBe('Southern California');
  });

  it('keeps a descriptive alt text on the portrait', () => {
    expect(render().querySelector('.portrait img')?.getAttribute('alt')).toContain('portrait of Gustavo Couto Vanin');
  });
});
