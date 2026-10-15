import { HttpErrorResponse } from '@angular/common/http';
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
