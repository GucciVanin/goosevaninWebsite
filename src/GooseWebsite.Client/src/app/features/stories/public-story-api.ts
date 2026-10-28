import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** A published story as the public API sends it. Drafts are never sent. */
export interface Story {
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  category: string;
  tags: string[];
  coverImage: string;
  coverImageAlt: string;
  publishedAtUtc: string | null;
  createdAtUtc: string;
}

const everyStory = 2147483647;

// The visitor's view of the stories API (the API calls them blog posts): reads only. It answers 404 for an unknown or draft slug.
@Injectable({ providedIn: 'root' })
export class PublicStoryApi {
  private readonly http = inject(HttpClient);

  /** Published stories, newest first. By default every story: the API has no upper limit on `pageSize`. */
  list(pageSize = everyStory): Observable<Story[]> {
    return this.http.get<Story[]>('/api/blog', { params: new HttpParams().set('pageSize', pageSize) });
  }

  get(slug: string): Observable<Story> {
    return this.http.get<Story>(`/api/blog/${encodeURIComponent(slug)}`);
  }
}
