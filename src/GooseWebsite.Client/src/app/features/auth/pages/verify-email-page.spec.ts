import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { VerifyEmailPage } from './verify-email-page';

// Ensures link scanners cannot confirm accounts without a deliberate reader action.
describe('VerifyEmailPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'verify-email', component: VerifyEmailPage }]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('waits for explicit confirmation before posting the verification token', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/verify-email#userId=reader-1&token=confirmation-token', VerifyEmailPage);
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('Confirm email address');
    http.expectNone('/api/auth/csrf');
    http.expectNone('/api/auth/verify-email');

    const confirmButton = harness.routeNativeElement?.querySelector('button');
    expect(confirmButton).not.toBeNull();
    confirmButton?.click();
    harness.detectChanges();

    http.expectOne('/api/auth/csrf').flush({ requestToken: 'csrf-request-token' });
    http.expectOne('/api/auth/verify-email').flush({ message: 'Your email address has been verified.' });
    await TestBed.inject(Router).navigateByUrl('/verify-email');
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Your email address has been verified.');
  });
});
