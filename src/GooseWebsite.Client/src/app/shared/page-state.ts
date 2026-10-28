import { Injectable, signal } from '@angular/core';

// What the shell needs to know about the page in the outlet. The not-found page can appear inside a project or story
// page (the API did not know the slug), so the header cannot tell from the address alone that no section is current.
@Injectable({ providedIn: 'root' })
export class PageState {
  readonly notFound = signal(false);
}
