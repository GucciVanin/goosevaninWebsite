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

  async function open(query: Record<string, string>, arrange?: (api: jasmine.SpyObj<AuthApi>) => void): Promise<void> {
    auth = jasmine.createSpyObj<AuthApi>('AuthApi', ['getReaderRegistrationAvailability', 'getCurrentUser', 'login', 'logout']);
    auth.getReaderRegistrationAvailability.and.returnValue(of({ enabled: false }));
    auth.getCurrentUser.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    auth.login.and.returnValue(of({ displayName: 'Site administrator', isAdmin: true }));
    auth.logout.and.returnValue(of(undefined));
    arrange?.(auth);

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

  // F3-US1-AC5: failure feedback is one generic message, whatever the reason, and says nothing about which email exists.
  it('shows the same generic message for an unknown email and for a wrong password, and keeps no password', async () => {
    const shown: string[] = [];
    for (const detail of ['No account with that email', 'The password is wrong']) {
      TestBed.resetTestingModule();
      await open({}, (api) => api.login.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401, error: { error: detail } }))));
      const page = fixture.componentInstance as unknown as SignInInternals;
      page.email = shown.length === 0 ? 'nobody@example.test' : 'admin@example.test';
      page.password = 'wrong-password';
      page.signIn();
      fixture.detectChanges();
      shown.push(fixture.nativeElement.querySelector('[role="alert"]')?.textContent.trim() ?? '');
      expect(page.password).toBe('');
    }

    expect(shown[0]).toBe('Unable to sign in with those credentials.');
    expect(shown[1]).toBe(shown[0]);
  });

  it('shows the same generic message when the server answers with any other failure', async () => {
    await open({}, (api) => api.login.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 }))));

    signIn();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent.trim()).toBe('Unable to sign in with those credentials.');
  });

  it('labels the Email and Password fields and ties each label to its input', async () => {
    await open({});

    for (const [id, text] of [['email', 'Email'], ['password', 'Password']] as const) {
      const label = fixture.nativeElement.querySelector(`label[for="${id}"]`) as HTMLLabelElement | null;
      expect(label?.textContent?.trim()).toBe(text);
      expect(fixture.nativeElement.querySelector(`input#${id}`)).not.toBeNull();
    }
  });

  it('shows a Sign out button when signed in, signs out through the API and says so', async () => {
    await open({}, (api) => api.getCurrentUser.and.returnValue(of({ displayName: 'Site administrator', isAdmin: true })));
    const button = (): HTMLButtonElement | undefined =>
      Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Sign out');

    expect(button()).toBeDefined();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();

    button()!.click();
    fixture.detectChanges();

    expect(auth.logout).toHaveBeenCalledTimes(1);
    expect(button()).toBeUndefined();
    expect(fixture.nativeElement.textContent).toContain('You have signed out.');
  });

  it('offers no Sign out button to a visitor who is not signed in', async () => {
    await open({});

    const labels = Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(labels).not.toContain('Sign out');
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
