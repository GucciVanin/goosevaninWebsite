import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { storyDate, storyMoment } from '../../shared/story-date';
import { PublicStoryApi, Story } from '../stories/public-story-api';

// How many stories the home page previews; /stories lists them all and carries the search and the filters.
const previewCount = 3;

// The home Stories section: a preview of the latest published stories, each linking to its own page.
@Component({
  selector: 'app-blog-page',
  imports: [RouterLink],
  templateUrl: './blog-page.html',
  styleUrl: './blog-page.scss',
})
export class BlogPage implements OnInit {
  private readonly api = inject(PublicStoryApi);
  protected readonly posts = signal<Story[]>([]);
  protected readonly status = signal<'loading' | 'loaded' | 'failed'>('loading');
  protected readonly date = (story: Story) => storyDate(storyMoment(story));

  ngOnInit(): void {
    this.api.list(previewCount).subscribe({
      next: (stories) => {
        this.posts.set(stories.slice(0, previewCount));
        this.status.set('loaded');
      },
      error: () => this.status.set('failed'),
    });
  }
}
