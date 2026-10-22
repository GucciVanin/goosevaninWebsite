import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SiteHeader } from './site-header';

// Guards the shared header route contract used by every page.
describe('SiteHeader', () => {
  let fixture: ComponentFixture<SiteHeader>;
  let controller: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SiteHeader],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    controller = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(SiteHeader);
    fixture.componentRef.setInput('navItems', []);
    fixture.detectChanges();
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
