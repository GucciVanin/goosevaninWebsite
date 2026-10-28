import { Routes } from '@angular/router';
import { editorGuard } from './features/editor/editor.guard';
import { ContactVerifyPage } from './features/contact/contact-verify-page';
import { SignInPage } from './features/auth/pages/sign-in-page';
import { VerifyEmailPage } from './features/auth/pages/verify-email-page';

// Centralizes URL ownership so feature pages remain independently maintainable.
// The editor is loaded on demand, so visitors never download the editor or its Markdown renderer.
export const routes: Routes = [
  { path: 'sign-in', component: SignInPage },
  { path: 'verify-email', component: VerifyEmailPage },
  { path: 'contact/verify', component: ContactVerifyPage },
  {
    path: 'editor',
    canActivate: [editorGuard],
    canActivateChild: [editorGuard],
    children: [
      { path: '', loadComponent: () => import('./features/editor/editor-page').then((m) => m.EditorPage) },
      { path: 'stories/new', loadComponent: () => import('./features/editor/pages/story-form-page').then((m) => m.StoryFormPage) },
      { path: 'stories/:id', loadComponent: () => import('./features/editor/pages/story-form-page').then((m) => m.StoryFormPage) },
      { path: 'projects/new', loadComponent: () => import('./features/editor/pages/project-form-page').then((m) => m.ProjectFormPage) },
      { path: 'projects/:id', loadComponent: () => import('./features/editor/pages/project-form-page').then((m) => m.ProjectFormPage) },
    ],
  },
  { path: 'access-denied', loadComponent: () => import('./features/editor/access-denied-page').then((m) => m.AccessDeniedPage) },
];
