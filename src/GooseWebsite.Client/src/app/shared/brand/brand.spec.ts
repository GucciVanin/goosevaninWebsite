import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ArtScene } from './art-scene';
import { BrandLogo } from './brand-logo';
import { ProjectPanel } from './project-panel';

describe('Brand components (F9-US1)', () => {
  it('shows the logo with descriptive alternative text and a width that keeps it crisp', () => {
    const fixture = TestBed.createComponent(BrandLogo);
    fixture.detectChanges();

    const img = (fixture.nativeElement as HTMLElement).querySelector('img') as HTMLImageElement;
    expect(img.getAttribute('alt')).toContain('Gustavo Couto Vanin');
    expect(img.getAttribute('srcset')).toContain('gcv-logo-512.webp 512w');
    expect(img.getAttribute('width')).toBe('96');
  });

  it('loads the art script once, however many scenes are on the page', () => {
    document.getElementById('gcv-art-script')?.remove();

    TestBed.createComponent(ArtScene).detectChanges();
    TestBed.createComponent(ArtScene).detectChanges();

    expect(document.querySelectorAll('#gcv-art-script').length).toBe(1);
    expect(document.getElementById('gcv-art-script')?.getAttribute('src')).toBe('/art/gcv-scene.js');
    document.getElementById('gcv-art-script')?.remove();
  });

  describe('ProjectPanel', () => {
    function open() {
      TestBed.configureTestingModule({ providers: [provideRouter([])] });
      const fixture = TestBed.createComponent(ProjectPanel);
      fixture.componentRef.setInput('title', 'Goose website');
      fixture.componentRef.setInput('slug', 'goose-website');
      fixture.componentRef.setInput('summary', 'A personal site.');
      fixture.componentRef.setInput('technologies', ['C#', 'Angular']);
      fixture.componentRef.setInput('image', '/brand/hero-city-960.webp');
      fixture.componentRef.setInput('imageAlt', 'A screenshot');
      fixture.componentRef.setInput('demoUrl', 'https://example.test/demo');
      fixture.detectChanges();
      return fixture;
    }

    it('starts closed, showing the picture, the stack and VIEW CASE', () => {
      const element = open().nativeElement as HTMLElement;

      expect(element.querySelector('img.shot')?.getAttribute('alt')).toBe('A screenshot');
      expect(element.querySelectorAll('.stack li').length).toBe(2);
      expect(element.querySelector('button.view')?.textContent).toContain('VIEW CASE');
      expect(element.querySelector('button.view')?.getAttribute('aria-expanded')).toBe('false');
      expect(element.querySelector('.case')).toBeNull();
    });

    it('opens the case in place with the link to the full project post, and closes again', () => {
      const fixture = open();
      const element = fixture.nativeElement as HTMLElement;
      const button = element.querySelector('button.view') as HTMLButtonElement;

      button.click();
      fixture.detectChanges();

      expect(button.getAttribute('aria-expanded')).toBe('true');
      expect(element.querySelector('.case')?.textContent).toContain('A personal site.');
      expect(element.querySelector('.case a[href="/projects/goose-website"]')).not.toBeNull();
      expect(element.querySelector('.case a[target="_blank"]')?.getAttribute('rel')).toContain('noopener');

      button.click();
      fixture.detectChanges();
      expect(element.querySelector('.case')).toBeNull();
    });
  });
});
