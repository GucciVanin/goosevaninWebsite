import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { SignInPage } from './sign-in-page';

type SignInInternals = { email: string; password: string; signIn(): void };

// Signing in returns the person to where they were headed, but only inside this site, and explains an expired session.
describe('SignInPage', () => {
  let auth: jasmine.SpyObj<AuthApi>;
  let router: Router;
  let fixture: ComponentFixture<SignInPage>;

  async function open(query: Record<string, string>): Promise<void> {
    auth = jasmine.createSpyObj<AuthApi>('AuthApi', ['getReaderRegistrationAvailability', 'getCurrentUser', 'login']);
    auth.getReaderRegistrationAvailability.and.returnValue(of({ enabled: false }));
    auth.getCurrentUser.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    auth.login.and.returnValue(of({ displayName: 'Site administrator', isAdmin: true }));

    await TestBed.configureTestingModule({
      imports: [SignInPage],
      providers: [
        provideRouter([]),
        { provide: AuthApi, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(query) } } },
      ],
    }).compileComponents();
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
    fixture = TestBed.createComponent(SignInPage);
    fixture.detectChanges();
  }

  function signIn(): void {
    const page = fixture.componentInstance as unknown as SignInInternals;
    page.email = 'admin@example.test';
    page.password = 'a-password';
    page.signIn();
  }

  it('explains an expired session and keeps that message over the generic one', async () => {
    await open({ reason: 'session-expired', returnUrl: '/editor' });

    expect(fixture.nativeElement.textContent).toContain('Your session expired. Sign in again to continue.');
    expect(fixture.nativeElement.textContent).not.toContain('You are not currently signed in');
  });

  it('returns to the page the person was headed for after signing in', async () => {
    await open({ returnUrl: '/editor' });

    signIn();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/editor');
  });

  it('ignores a return address that is not a path on this site', async () => {
    await open({ returnUrl: 'https://evil.example/editor' });

    signIn();

    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('stays on the page when there is nowhere to return to', async () => {
    await open({});

    signIn();
    fixture.detectChanges();

    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Signed in as Site administrator');
  });
});

// F9-US7: with the real AuthApi, a visitor who has no sign-in marker is signed out and the page does not ask the API.
describe('SignInPage session probe', () => {
  let controller: HttpTestingController;

  async function open(): Promise<ComponentFixture<SignInPage>> {
    await TestBed.configureTestingModule({
      imports: [SignInPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();
    controller = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(SignInPage);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    document.cookie = 'goosewebsite.signedin=; path=/; max-age=0';
  });

  it('makes no request to /api/auth/me without the marker and says the visitor is not signed in', async () => {
    document.cookie = 'goosewebsite.signedin=; path=/; max-age=0';

    const fixture = await open();
    controller.expectOne('/api/auth/reader-registration').flush({ enabled: false });
    fixture.detectChanges();

    controller.expectNone('/api/auth/me');
    expect(fixture.nativeElement.textContent).toContain('You are not currently signed in');
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });

  it('asks the API with the marker and shows who is signed in', async () => {
    document.cookie = 'goosewebsite.signedin=1; path=/';

    const fixture = await open();
    controller.expectOne('/api/auth/reader-registration').flush({ enabled: false });
    controller.expectOne('/api/auth/me').flush({ displayName: 'Site administrator', isAdmin: true });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Signed in as Site administrator');
  });
});
