import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { SubscribeConfirmPage } from './subscribe-confirm-page';

// F10-US1 test 3: the emailed link opens a page, and the subscriber is confirmed only by an explicit action, so a
// mail scanner that opens the link (even one that runs scripts) cannot confirm an address by itself.
describe('SubscribeConfirmPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'subscribe/confirm', component: SubscribeConfirmPage }]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('waits for the button before posting the token from the address fragment', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/subscribe/confirm#token=abc123', SubscribeConfirmPage);
    harness.detectChanges();

    http.expectNone('/api/subscriptions/confirm');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Confirm your subscription');
    harness.routeNativeElement?.querySelector('button')?.click();
    harness.detectChanges();

    const request = http.expectOne('/api/subscriptions/confirm');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ token: 'abc123' });
    expect(harness.routeNativeElement?.querySelector('button')?.textContent).toContain('Confirming');
    request.flush({ confirmed: true, message: 'Your subscription is confirmed. Thank you.' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain('Your subscription is confirmed');
    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
  });

  it('removes the token from the address bar before using it', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/subscribe/confirm#token=abc123', SubscribeConfirmPage);
    harness.detectChanges();

    harness.routeNativeElement?.querySelector('button')?.click();
    http.expectOne('/api/subscriptions/confirm').flush({ confirmed: true, message: 'ok' });
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).not.toContain('abc123');
  });

  it('says in words that an invalid, expired or used link is no longer valid', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/subscribe/confirm#token=old', SubscribeConfirmPage);
    harness.detectChanges();

    harness.routeNativeElement?.querySelector('button')?.click();
    http.expectOne('/api/subscriptions/confirm').flush({ error: 'This confirmation link is no longer valid.' }, { status: 400, statusText: 'Bad Request' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('This confirmation link is no longer valid.');
    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
  });

  it('reports a link without a token and offers no button', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/subscribe/confirm', SubscribeConfirmPage);
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('no longer valid');
    http.expectNone('/api/subscriptions/confirm');
  });

  it('is a route with its own document title', () => {
    const route = routes.find((candidate) => candidate.path === 'subscribe/confirm');

    expect(route?.title).toBe('Confirm subscription');
  });
});
