import { Injectable, signal } from '@angular/core';

// The tag the Stories section starts filtered by. The home category cards set it (Competitions sets `competition`,
// the other cards clear it) and the Stories section reads it; an empty tag means no tag filter.
@Injectable({ providedIn: 'root' })
export class StoryFilter {
  readonly tag = signal('');
}
