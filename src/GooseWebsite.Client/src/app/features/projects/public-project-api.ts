import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** A published project as the public API sends it. Drafts are never sent. */
export interface PublicProject {
  title: string;
  slug: string;
  overview: string;
  contribution: string;
  technologies: string[];
  demoUrl: string | null;
  sourceUrl: string | null;
  coverImage: string;
  coverImageAlt: string;
  publishedAtUtc: string | null;
}

// The visitor's view of the projects API: reads only. The API answers 404 for an unknown or draft slug.
@Injectable({ providedIn: 'root' })
export class PublicProjectApi {
  private readonly http = inject(HttpClient);

  list(): Observable<PublicProject[]> {
    return this.http.get<PublicProject[]>('/api/projects');
  }

  get(slug: string): Observable<PublicProject> {
    return this.http.get<PublicProject>(`/api/projects/${encodeURIComponent(slug)}`);
  }
}
