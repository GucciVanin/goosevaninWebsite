import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Project, ProjectDraft, ProjectSummary } from '../models/project';

// The editor's view of the projects API. Writes get their CSRF token from the shared session interceptor.
@Injectable({ providedIn: 'root' })
export class ProjectApi {
  private readonly root = '/api/editor/projects';

  constructor(private readonly http: HttpClient) {}

  list(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(this.root);
  }

  get(id: number): Observable<Project> {
    return this.http.get<Project>(`${this.root}/${id}`);
  }

  create(draft: ProjectDraft): Observable<Project> {
    return this.http.post<Project>(this.root, draft);
  }

  update(id: number, draft: ProjectDraft): Observable<Project> {
    return this.http.put<Project>(`${this.root}/${id}`, draft);
  }

  publish(id: number): Observable<Project> {
    return this.http.post<Project>(`${this.root}/${id}/publish`, {});
  }

  unpublish(id: number): Observable<Project> {
    return this.http.post<Project>(`${this.root}/${id}/unpublish`, {});
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.root}/${id}`);
  }
}
