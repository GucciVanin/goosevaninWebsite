import { Routes } from '@angular/router';
import { ContactVerifyPage } from './features/contact/contact-verify-page';
import { SignInPage } from './features/auth/pages/sign-in-page';
import { VerifyEmailPage } from './features/auth/pages/verify-email-page';

// Centralizes URL ownership so feature pages remain independently maintainable.
export const routes: Routes = [
  { path: 'sign-in', component: SignInPage },
  { path: 'verify-email', component: VerifyEmailPage },
  { path: 'contact/verify', component: ContactVerifyPage },
];
