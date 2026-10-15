import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable } from 'rxjs';
import { editorGuard } from './editor.guard';

// The editor is for administrators only. The API enforces it too; the guard keeps everyone else off the page.
describe('editorGuard', () => {
  let controller: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    controller = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => controller.verify());

  function visit(): Promise<boolean | UrlTree> {
    const result = TestBed.runInInjectionContext(() =>
      editorGuard({} as ActivatedRouteSnapshot, { url: '/editor' } as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>;
    return new Promise((resolve) => result.subscribe(resolve));
  }

  it('lets an administrator in', async () => {
    const outcome = visit();
    controller.expectOne('/api/auth/me').flush({ displayName: 'Site administrator', isAdmin: true });

    expect(await outcome).toBeTrue();
  });

  it('keeps a signed-in reader out and says so', async () => {
    const outcome = visit();
    controller.expectOne('/api/auth/me').flush({ displayName: 'A reader', isAdmin: false });

    expect(router.serializeUrl((await outcome) as UrlTree)).toBe('/access-denied');
  });

  it('sends a visitor who is not signed in to sign in, and back to the editor afterwards', async () => {
    const outcome = visit();
    controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(router.serializeUrl((await outcome) as UrlTree)).toBe('/sign-in?returnUrl=%2Feditor');
  });

  it('does not open the editor when the session cannot be checked', async () => {
    const outcome = visit();
    controller.expectOne('/api/auth/me').flush({}, { status: 500, statusText: 'Server Error' });

    expect(await outcome).not.toBeTrue();
  });
});
