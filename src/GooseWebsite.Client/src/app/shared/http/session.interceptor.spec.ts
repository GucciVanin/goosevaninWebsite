import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { sessionInterceptor } from './session.interceptor';

// Every write to the API carries the CSRF token, and a request that finds the session gone sends the person back to sign in.
describe('sessionInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate'], { url: '/editor' });
    router.navigate.and.resolveTo(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([sessionInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('fetches a CSRF token and sends it with a write to the API', () => {
    http.post('/api/blog', { title: 'x' }).subscribe();

    controller.expectOne('/api/auth/csrf').flush({ requestToken: 'token-1' });
    const write = controller.expectOne('/api/blog');

    expect(write.request.headers.get('X-CSRF-TOKEN')).toBe('token-1');
    write.flush({});
  });

  it('also covers updates and deletes', () => {
    http.put('/api/blog/1', {}).subscribe();
    controller.expectOne('/api/auth/csrf').flush({ requestToken: 'a' });
    expect(controller.expectOne('/api/blog/1').request.headers.get('X-CSRF-TOKEN')).toBe('a');

    http.delete('/api/blog/1').subscribe();
    controller.expectOne('/api/auth/csrf').flush({ requestToken: 'b' });
    expect(controller.expectOne('/api/blog/1').request.headers.get('X-CSRF-TOKEN')).toBe('b');
  });

  it('does not ask for a token for a read', () => {
    http.get('/api/blog').subscribe();

    controller.expectOne('/api/blog').flush([]);
  });

  it('keeps a token that the caller already attached', () => {
    http.post('/api/auth/login', {}, { headers: { 'X-CSRF-TOKEN': 'own' } }).subscribe({ error: () => undefined });

    const request = controller.expectOne('/api/auth/login');
    expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('own');
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
  });

  it('leaves requests to other places alone', () => {
    http.post('/somewhere-else', {}).subscribe();

    controller.expectOne('/somewhere-else').flush({});
  });

  it('sends the person to sign in, remembering where they were, when a write finds the session gone', () => {
    let failed = false;
    http.post('/api/blog', {}).subscribe({ error: () => (failed = true) });
    controller.expectOne('/api/auth/csrf').flush({ requestToken: 't' });

    controller.expectOne('/api/blog').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBeTrue();
    expect(router.navigate).toHaveBeenCalledWith(['/sign-in'], {
      queryParams: { returnUrl: '/editor', reason: 'session-expired' },
    });
  });

  it('does not send the person to sign in again from the sign-in page itself', () => {
    (Object.getOwnPropertyDescriptor(router, 'url')!.get as jasmine.Spy).and.returnValue('/sign-in?returnUrl=%2Feditor');
    http.post('/api/blog', {}).subscribe({ error: () => undefined });
    controller.expectOne('/api/auth/csrf').flush({ requestToken: 't' });

    controller.expectOne('/api/blog').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not treat a wrong password as an expired session', () => {
    http.post('/api/auth/login', {}, { headers: { 'X-CSRF-TOKEN': 'own' } }).subscribe({ error: () => undefined });

    controller.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not redirect when the answer is a refusal rather than a missing session', () => {
    http.post('/api/blog', {}).subscribe({ error: () => undefined });
    controller.expectOne('/api/auth/csrf').flush({ requestToken: 't' });

    controller.expectOne('/api/blog').flush({}, { status: 403, statusText: 'Forbidden' });

    expect(router.navigate).not.toHaveBeenCalled();
  });
});
