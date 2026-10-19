import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ProjectPanel } from '../../../shared/brand/project-panel';

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

// Markdown to a short plain summary for the panel: no markup characters, cut at a word boundary.
export function plainSummary(markdown: string, limit = 220): string {
  const text = markdown
    .replace(/!\[[^\]]*\]\([^)]*\)/g, '')
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/[#>*_`~]/g, '')
    .replace(/\s+/g, ' ')
    .trim();
  if (text.length <= limit) {
    return text;
  }

  return text.slice(0, text.lastIndexOf(' ', limit) > 0 ? text.lastIndexOf(' ', limit) : limit) + '…';
}

// Shows Gustavo's published projects. There is no sample content: with none published, the section says so.
@Component({
  selector: 'app-selected-work',
  imports: [ProjectPanel],
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
          this.projects.set(items.map((item) => ({ ...item, summary: plainSummary(item.overview) })));
          this.loaded.set(true);
        },
        error: () => this.failed.set(true),
      });
  }
}
