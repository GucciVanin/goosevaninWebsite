import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthApi } from './auth-api';

const MARKER = 'goosewebsite.signedin';

function setMarker(): void {
  document.cookie = `${MARKER}=1; path=/`;
}

function clearMarker(): void {
  document.cookie = `${MARKER}=; path=/; max-age=0`;
}

function markerPresent(): boolean {
  return document.cookie.split(';').some((part) => part.trim().startsWith(`${MARKER}=`));
}

// F9-US7: the app asks GET /api/auth/me only when the sign-in marker cookie is present, so a signed-out visitor
// never triggers a 401. The marker holds no identity; the API still decides who is signed in.
describe('AuthApi current user', () => {
  let api: AuthApi;
  let controller: HttpTestingController;

  beforeEach(() => {
    clearMarker();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AuthApi);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
    clearMarker();
  });

  it('makes no request without the marker and reports "not signed in" as a 401', () => {
    let status = 0;
    api.getCurrentUser().subscribe({ error: (error: HttpErrorResponse) => (status = error.status) });

    controller.expectNone('/api/auth/me');
    expect(status).toBe(401);
  });

  it('asks the API when the marker is present and returns the user', () => {
    setMarker();
    let name = '';
    api.getCurrentUser().subscribe((user) => (name = user.displayName));

    controller.expectOne('/api/auth/me').flush({ displayName: 'Site administrator', isAdmin: true });

    expect(name).toBe('Site administrator');
    expect(markerPresent()).toBeTrue();
  });

  it('clears the marker when the API answers 401', () => {
    setMarker();
    api.getCurrentUser().subscribe({ error: () => undefined });

    controller.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(markerPresent()).toBeFalse();
  });

  it('keeps the marker when the check fails for another reason', () => {
    setMarker();
    api.getCurrentUser().subscribe({ error: () => undefined });

    controller.expectOne('/api/auth/me').flush({}, { status: 500, statusText: 'Server Error' });

    expect(markerPresent()).toBeTrue();
  });

  it('clears the marker after signing out', () => {
    setMarker();
    api.logout().subscribe();

    controller.expectOne('/api/auth/csrf').flush({ requestToken: 't' });
    controller.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });

    expect(markerPresent()).toBeFalse();
  });
});
