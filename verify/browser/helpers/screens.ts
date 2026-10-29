// The screen list (TE5). Every check kind runs on every screen. Add a screen here and all checks cover it.
export interface Screen {
  id: string;
  path: string;
  /** Opened with the seeded test administrator's session. */
  admin?: boolean;
}

export const screens: Screen[] = [
  { id: 'home', path: '/' },
  { id: 'projects', path: '/projects' },
  { id: 'stories', path: '/stories' },
  { id: 'sign-in', path: '/sign-in' },
  { id: 'email-confirmation', path: '/verify-email' },
  { id: 'contact-confirmation', path: '/contact/verify' },
  { id: 'subscribe-confirmation', path: '/subscribe/confirm' },
  { id: 'unsubscribe', path: '/unsubscribe' },
  { id: 'access-denied', path: '/access-denied' },
  { id: 'editor-workspace', path: '/editor', admin: true },
  { id: 'story-form', path: '/editor/stories/new', admin: true },
  { id: 'project-form', path: '/editor/projects/new', admin: true },
];
