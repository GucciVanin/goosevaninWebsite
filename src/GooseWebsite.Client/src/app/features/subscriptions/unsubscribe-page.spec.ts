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

  // Security O2 S6: only an invalid link (400) is final; a busy server, a failure or a lost connection must let the
  // person try again from the same page, because the token left the address bar and cannot be recovered from it.
  for (const failure of [
    { name: 'a rate limit (429)', status: 429, statusText: 'Too Many Requests' },
    { name: 'a server error (500)', status: 500, statusText: 'Server Error' },
    { name: 'a network error', status: 0, statusText: 'Unknown Error' },
  ]) {
    it(`keeps the token and the button for a retry after ${failure.name}`, async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/unsubscribe#token=keepme', UnsubscribePage);
      harness.detectChanges();

      harness.routeNativeElement?.querySelector('button')?.click();
      const first = http.expectOne('/api/subscriptions/unsubscribe');
      if (failure.status === 0) {
        first.error(new ProgressEvent('error'));
      } else {
        first.flush({ error: 'ignored' }, { status: failure.status, statusText: failure.statusText });
      }
      harness.detectChanges();

      const text = harness.routeNativeElement?.textContent ?? '';
      expect(text).toContain('Something went wrong');
      expect(text).not.toContain('no longer valid');
      const retry = harness.routeNativeElement?.querySelector('button') as HTMLButtonElement | null;
      expect(retry).not.toBeNull();
      expect(retry?.disabled).toBeFalse();
      await harness.fixture.whenStable();
      expect(TestBed.inject(Router).url).not.toContain('keepme');

      retry?.click();
      const second = http.expectOne('/api/subscriptions/unsubscribe');
      expect(second.request.body).toEqual({ token: 'keepme' });
      second.flush({ unsubscribed: true, message: 'You are unsubscribed and your name and email address are deleted.' });
      harness.detectChanges();

      expect(harness.routeNativeElement?.textContent).toContain('You are unsubscribed');
      expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
    });
  }

  it('treats only an invalid token (400) as final and drops the token', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unsubscribe#token=bad', UnsubscribePage);
    harness.detectChanges();

    harness.routeNativeElement?.querySelector('button')?.click();
    http.expectOne('/api/subscriptions/unsubscribe').flush({ error: 'This unsubscribe link is no longer valid.' }, { status: 400, statusText: 'Bad Request' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('no longer valid');
    expect(harness.routeNativeElement?.textContent).not.toContain('Something went wrong');
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
