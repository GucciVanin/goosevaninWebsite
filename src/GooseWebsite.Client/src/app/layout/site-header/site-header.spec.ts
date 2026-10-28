import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { StoryFilter } from '../../shared/story-filter';
import { SiteHeader } from './site-header';

// Guards the shared header route contract used by every page.
describe('SiteHeader', () => {
  let fixture: ComponentFixture<SiteHeader>;
  let controller: HttpTestingController;

  beforeEach(async () => {
    // The sign-in marker is present, so the header asks the API who is signed in (see the describe below for none).
    document.cookie = 'goosewebsite.signedin=1; path=/';
    await TestBed.configureTestingModule({
      imports: [SiteHeader],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    controller = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(SiteHeader);
    fixture.componentRef.setInput('navItems', []);
    fixture.detectChanges();
  });

  afterEach(() => {
    document.cookie = 'goosewebsite.signedin=; path=/; max-age=0';
  });

  function editorLink(): HTMLAnchorElement | null {
    return fixture.nativeElement.querySelector('.topbar-editor') as HTMLAnchorElement | null;
  }

  it('shows a shared sign-in link on the right side that targets the sign-in route', () => {
    controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('.topbar-login') as HTMLAnchorElement;

    expect(link).not.toBeNull();
    expect(link.textContent.trim()).toBe('Sign in');
    expect(link.getAttribute('href')).toBe('/sign-in');
  });

  it('shows the Editor link to a signed-in administrator', () => {
    controller.expectOne('/api/auth/me').flush({ displayName: 'Site administrator', isAdmin: true });
    fixture.detectChanges();

    expect(editorLink()).not.toBeNull();
    expect(editorLink()!.textContent.trim()).toBe('Editor');
    expect(editorLink()!.getAttribute('href')).toBe('/editor');
  });

  it('hides the Editor link from a reader and from visitors who are not signed in', () => {
    controller.expectOne('/api/auth/me').flush({ displayName: 'A reader', isAdmin: false });
    fixture.detectChanges();
    expect(editorLink()).toBeNull();
  });

  it('hides the Editor link when nobody is signed in', () => {
    controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(editorLink()).toBeNull();
  });

  describe('mobile menu', () => {
    function toggle(): HTMLButtonElement {
      return fixture.nativeElement.querySelector('.gcv-menu') as HTMLButtonElement;
    }

    it('opens and closes a menu with the same links, and reports its state to assistive technology', () => {
      fixture.componentRef.setInput('navItems', [{ label: 'Projects', id: 'work' }]);
      fixture.detectChanges();
      controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
      expect(toggle().getAttribute('aria-expanded')).toBe('false');
      expect(fixture.nativeElement.querySelector('.gcv-drawer')).toBeNull();

      toggle().click();
      fixture.detectChanges();

      expect(toggle().getAttribute('aria-expanded')).toBe('true');
      const links = Array.from(fixture.nativeElement.querySelectorAll('.gcv-drawer a') as NodeListOf<HTMLAnchorElement>).map((a) => a.textContent.trim());
      expect(links).toEqual(['Projects', 'Sign in']);

      toggle().click();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.gcv-drawer')).toBeNull();
    });

    // TE4 O6: the Stories link shows all stories, so it clears a tag left by the Competitions card.
    it('clears the story tag from the Stories link in the header and in the menu, but not from other links', () => {
      const filter = TestBed.inject(StoryFilter);
      fixture.componentRef.setInput('navItems', [{ label: 'Projects', id: 'work' }, { label: 'Stories', id: 'blog' }]);
      fixture.detectChanges();
      controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
      const nav = () => Array.from(fixture.nativeElement.querySelectorAll('.gcv-nav a') as NodeListOf<HTMLAnchorElement>);

      filter.tag.set('competition');
      nav().find((a) => a.textContent.trim() === 'Projects')!.click();
      expect(filter.tag()).toBe('competition');
      nav().find((a) => a.textContent.trim() === 'Stories')!.click();
      expect(filter.tag()).toBe('');

      filter.tag.set('competition');
      toggle().click();
      fixture.detectChanges();
      const drawerLink = Array.from(fixture.nativeElement.querySelectorAll('.gcv-drawer a') as NodeListOf<HTMLAnchorElement>).find((a) => a.textContent.trim() === 'Stories')!;
      drawerLink.click();
      expect(filter.tag()).toBe('');
    });

    // F5-US1: an item with a route is a real page link (header and menu); the others keep scrolling home sections.
    it('sends a routed item to its page from the header and from the menu, and still scrolls the other items', async () => {
      TestBed.inject(Router).resetConfig([{ path: 'projects', children: [] }]);
      fixture.componentRef.setInput('navItems', [{ label: 'Projects', id: 'work', route: '/projects' }, { label: 'Stories', id: 'blog' }]);
      fixture.detectChanges();
      controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
      const emitted: string[] = [];
      fixture.componentInstance.navigate.subscribe((id) => emitted.push(id));
      const headerLink = (label: string) => Array.from(fixture.nativeElement.querySelectorAll('.gcv-nav a') as NodeListOf<HTMLAnchorElement>).find((a) => a.textContent.trim() === label)!;

      expect(headerLink('Projects').getAttribute('href')).toBe('/projects');
      expect(headerLink('Stories').getAttribute('href')).toBe('#blog');
      headerLink('Projects').click();
      await fixture.whenStable();
      expect(emitted).toEqual([]);
      expect(TestBed.inject(Router).url).toBe('/projects');

      headerLink('Stories').click();
      expect(emitted).toEqual(['blog']);

      toggle().click();
      fixture.detectChanges();
      const drawerLink = Array.from(fixture.nativeElement.querySelectorAll('.gcv-drawer a') as NodeListOf<HTMLAnchorElement>).find((a) => a.textContent.trim() === 'Projects')!;
      expect(drawerLink.getAttribute('href')).toBe('/projects');
      drawerLink.click();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.gcv-drawer')).toBeNull();
      expect(emitted).toEqual(['blog']);
    });

    it('closes the menu after choosing a section', () => {
      fixture.componentRef.setInput('navItems', [{ label: 'Projects', id: 'work' }]);
      fixture.detectChanges();
      controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
      toggle().click();
      fixture.detectChanges();

      (fixture.nativeElement.querySelector('.gcv-drawer a') as HTMLAnchorElement).click();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.gcv-drawer')).toBeNull();
    });
  });
});


// F9-US7: a visitor with no sign-in marker is signed out, so the header does not ask the API (which would answer 401).
describe('SiteHeader without a sign-in marker', () => {
  let controller: HttpTestingController;

  beforeEach(async () => {
    document.cookie = 'goosewebsite.signedin=; path=/; max-age=0';
    await TestBed.configureTestingModule({
      imports: [SiteHeader],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    document.cookie = 'goosewebsite.signedin=; path=/; max-age=0';
  });

  it('makes no request to /api/auth/me and shows the sign-in link', () => {
    const fixture = TestBed.createComponent(SiteHeader);
    fixture.componentRef.setInput('navItems', []);
    fixture.detectChanges();

    controller.expectNone('/api/auth/me');
    expect((fixture.nativeElement.querySelector('.topbar-login') as HTMLAnchorElement).getAttribute('href')).toBe('/sign-in');
    expect(fixture.nativeElement.querySelector('.topbar-editor')).toBeNull();
  });

  it('forgets a marker the API does not honour, so the next page does not ask again', () => {
    document.cookie = 'goosewebsite.signedin=1; path=/';
    const fixture = TestBed.createComponent(SiteHeader);
    fixture.componentRef.setInput('navItems', []);
    fixture.detectChanges();

    controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(document.cookie).not.toContain('goosewebsite.signedin');
  });
});
