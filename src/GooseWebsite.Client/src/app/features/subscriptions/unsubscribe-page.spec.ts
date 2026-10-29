import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { UnsubscribePage } from './unsubscribe-page';

// F10-US2: the link in every subscription email opens this page; the subscriber is removed only when the person
// presses the button, so a mail scanner that opens the link (even one that runs scripts) cannot unsubscribe anyone.
describe('UnsubscribePage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'unsubscribe', component: UnsubscribePage }]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('waits for the button before posting the token from the address fragment', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unsubscribe#token=abc123', UnsubscribePage);
    harness.detectChanges();

    http.expectNone('/api/subscriptions/unsubscribe');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Unsubscribe');
    harness.routeNativeElement?.querySelector('button')?.click();
    harness.detectChanges();

    const request = http.expectOne('/api/subscriptions/unsubscribe');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ token: 'abc123' });
    expect(harness.routeNativeElement?.querySelector('button')?.textContent).toContain('Unsubscribing');
    request.flush({ unsubscribed: true, message: 'You are unsubscribed and your name and email address are deleted.' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain('You are unsubscribed');
    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
  });

  it('removes the token from the address bar before using it', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unsubscribe#token=abc123', UnsubscribePage);
    harness.detectChanges();

    harness.routeNativeElement?.querySelector('button')?.click();
    http.expectOne('/api/subscriptions/unsubscribe').flush({ unsubscribed: true, message: 'ok' });
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).not.toContain('abc123');
  });

  it('gives an invalid or already used link a neutral message that says nothing about the address', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unsubscribe#token=old', UnsubscribePage);
    harness.detectChanges();

    harness.routeNativeElement?.querySelector('button')?.click();
    http.expectOne('/api/subscriptions/unsubscribe').flush({ error: 'This unsubscribe link is no longer valid.' }, { status: 400, statusText: 'Bad Request' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('This unsubscribe link is no longer valid.');
    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
  });

  it('reports a link without a token and offers no button', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unsubscribe', UnsubscribePage);
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('no longer valid');
    http.expectNone('/api/subscriptions/unsubscribe');
  });

  it('is a route with its own document title', () => {
    expect(routes.find((candidate) => candidate.path === 'unsubscribe')?.title).toBe('Unsubscribe');
  });
});
