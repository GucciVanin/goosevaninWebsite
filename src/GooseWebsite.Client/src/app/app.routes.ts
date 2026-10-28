import { Routes } from '@angular/router';
import { editorGuard } from './features/editor/editor.guard';
import { ContactVerifyPage } from './features/contact/contact-verify-page';
import { SignInPage } from './features/auth/pages/sign-in-page';
import { VerifyEmailPage } from './features/auth/pages/verify-email-page';

// Centralizes URL ownership so feature pages remain independently maintainable.
// Every page is loaded on demand. The editor in particular stays out of the visitor's download.
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
  { path: '', pathMatch: 'full', children: [] }, // the one-page home is rendered by the shell itself
  { path: 'projects', title: 'Projects', loadComponent: () => import('./features/projects/projects-index-page').then((m) => m.ProjectsIndexPage) },
  { path: 'projects/:slug', title: 'Project', loadComponent: () => import('./features/projects/project-page').then((m) => m.ProjectPage) },
  { path: 'stories', title: 'Stories', loadComponent: () => import('./features/stories/stories-index-page').then((m) => m.StoriesIndexPage) },
  { path: 'stories/:slug', title: 'Story', loadComponent: () => import('./features/stories/story-page').then((m) => m.StoryPage) },
  // Last: any other URL.
  { path: '**', title: 'Page not found', loadComponent: () => import('./features/not-found/not-found-page').then((m) => m.NotFoundPage) },
];
