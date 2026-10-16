import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

/** An uploaded image: `url` is what a story or project stores, and what Markdown uses. */
export interface UploadedImage {
  id: number;
  url: string;
  contentType: string;
  sizeBytes: number;
  uploadedAtUtc: string;
}

// The editor's view of the image upload API. The CSRF token comes from the shared session interceptor.
@Injectable({ providedIn: 'root' })
export class ImageApi {
  constructor(private readonly http: HttpClient) {}

  upload(file: File): Observable<UploadedImage> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<UploadedImage>('/api/editor/images', form);
  }
}
