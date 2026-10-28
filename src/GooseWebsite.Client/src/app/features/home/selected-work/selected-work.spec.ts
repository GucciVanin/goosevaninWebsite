import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SelectedWork } from './selected-work';

// The projects section shows what Gustavo has published, and never sample content.
describe('SelectedWork', () => {
  function open() {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(SelectedWork);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController) };
  }

  it('says projects will appear when none are published', () => {
    const { fixture, http } = open();

    http.expectOne('/api/projects').flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Projects will appear here');
    expect(fixture.nativeElement.querySelectorAll('app-project-panel').length).toBe(0);
  });

  it('shows each published project as a panel with its stack and a plain-text summary', () => {
    const { fixture, http } = open();

    http.expectOne('/api/projects').flush([
      { title: 'Goose website', slug: 'goose-website', overview: '# About\n\nA *personal* site with a [link](https://example.test).', technologies: ['C#', 'Angular'], coverImage: '', coverImageAlt: '', demoUrl: null, sourceUrl: null },
    ]);
    fixture.detectChanges();
    const panel = fixture.nativeElement.querySelector('app-project-panel') as HTMLElement;

    expect(panel.textContent).toContain('Goose website');
    expect(panel.querySelectorAll('.stack li').length).toBe(2);
    (panel.querySelector('button.view') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(panel.querySelector('.case p')?.textContent).toBe('About A personal site with a link.');
  });

  it('previews at most three projects and links to all of them on the projects page', () => {
    const { fixture, http } = open();
    const project = (n: number) => ({ title: `Project ${n}`, slug: `project-${n}`, overview: 'x', technologies: [], coverImage: '', coverImageAlt: '', demoUrl: null, sourceUrl: null });

    http.expectOne('/api/projects').flush([1, 2, 3, 4].map(project));
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('app-project-panel').length).toBe(3);
    expect(element.querySelector('a.all-projects')?.getAttribute('href')).toBe('/projects');
    expect(element.querySelector('a.all-projects')?.textContent).toContain('All projects');
  });

  it('tells the visitor when the projects cannot be loaded', () => {
    const { fixture, http } = open();

    http.expectOne('/api/projects').flush('x', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('could not be loaded');
  });
});
