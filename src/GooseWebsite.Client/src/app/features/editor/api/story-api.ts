import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Story, StoryDraft, StorySummary } from '../models/story';

// The editor's view of the stories API. Writes get their CSRF token from the shared session interceptor.
@Injectable({ providedIn: 'root' })
export class StoryApi {
  private readonly root = '/api/editor/stories';

  constructor(private readonly http: HttpClient) {}

  list(): Observable<StorySummary[]> {
    return this.http.get<StorySummary[]>(this.root);
  }

  get(id: number): Observable<Story> {
    return this.http.get<Story>(`${this.root}/${id}`);
  }

  create(draft: StoryDraft): Observable<Story> {
    return this.http.post<Story>(this.root, draft);
  }

  update(id: number, draft: StoryDraft): Observable<Story> {
    return this.http.put<Story>(`${this.root}/${id}`, draft);
  }

  publish(id: number): Observable<Story> {
    return this.http.post<Story>(`${this.root}/${id}/publish`, {});
  }

  unpublish(id: number): Observable<Story> {
    return this.http.post<Story>(`${this.root}/${id}/unpublish`, {});
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.root}/${id}`);
  }
}
