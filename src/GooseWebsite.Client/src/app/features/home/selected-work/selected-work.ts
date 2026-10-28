import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProjectPanel } from '../../../shared/brand/project-panel';
import { plainSummary } from '../../../shared/plain-summary';

interface PublishedProject {
  title: string;
  slug: string;
  summary: string;
  technologies: string[];
  coverImage: string;
  coverImageAlt: string;
  demoUrl: string | null;
  sourceUrl: string | null;
}

// How many projects the home page previews; /projects lists them all.
const previewCount = 3;

// Shows a preview of Gustavo's published projects, with a link to all of them. There is no sample content: with none published, the section says so.
@Component({
  selector: 'app-selected-work',
  imports: [ProjectPanel, RouterLink],
  templateUrl: './selected-work.html',
  styleUrl: './selected-work.scss',
})
export class SelectedWork implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly projects = signal<PublishedProject[]>([]);
  protected readonly loaded = signal(false);
  protected readonly failed = signal(false);

  ngOnInit(): void {
    this.http
      .get<Array<{ title: string; slug: string; overview: string; technologies: string[]; coverImage: string; coverImageAlt: string; demoUrl: string | null; sourceUrl: string | null }>>('/api/projects')
      .subscribe({
        next: (items) => {
          this.projects.set(items.slice(0, previewCount).map((item) => ({ ...item, summary: plainSummary(item.overview) })));
          this.loaded.set(true);
        },
        error: () => this.failed.set(true),
      });
  }
}
