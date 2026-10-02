import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SiteHeader } from './site-header';

// Guards the shared header route contract used by every page.
describe('SiteHeader', () => {
  let fixture: ComponentFixture<SiteHeader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SiteHeader],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(SiteHeader);
    fixture.componentRef.setInput('navItems', []);
    fixture.detectChanges();
  });

  it('shows a shared sign-in link on the right side that targets the sign-in route', () => {
    const link = fixture.nativeElement.querySelector('.topbar-login') as HTMLAnchorElement;

    expect(link).not.toBeNull();
    expect(link.textContent.trim()).toBe('Sign in');
    expect(link.getAttribute('href')).toBe('/sign-in');
  });
});