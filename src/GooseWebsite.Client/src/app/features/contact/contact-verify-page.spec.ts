import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ContactVerifyPage } from './contact-verify-page';

// Ensures link scanners cannot deliver a contact message without a deliberate sender action.
describe('ContactVerifyPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'contact/verify', component: ContactVerifyPage }]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('waits for explicit confirmation before posting the token', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/contact/verify#token=abc123', ContactVerifyPage);
    harness.detectChanges();

    http.expectNone('/api/contact/verify');
    harness.routeNativeElement?.querySelector('button')?.click();
    harness.detectChanges();

    const request = http.expectOne('/api/contact/verify');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ token: 'abc123' });
    request.flush({ verified: true, message: 'Your email has been verified and the message was sent to Gustavo.' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('the message was sent to Gustavo');
  });

  it('shows the failure message for an expired link', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/contact/verify#token=old', ContactVerifyPage);
    harness.detectChanges();

    harness.routeNativeElement?.querySelector('button')?.click();
    http.expectOne('/api/contact/verify').flush({ error: 'This verification link has expired.' }, { status: 410, statusText: 'Gone' });
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('This verification link has expired.');
  });

  it('reports an incomplete link without a button', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/contact/verify', ContactVerifyPage);
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('invalid or incomplete');
  });
});
