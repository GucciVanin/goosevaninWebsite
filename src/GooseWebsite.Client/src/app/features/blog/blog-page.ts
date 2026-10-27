import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface BlogPost {
  slug: string;
  title: string;
  excerpt: string;
  category: string;
  tags: string[];
  date: string;
  readTime: string;
  content: string;
}

// Keeps blog loading and reading state separate from shared site presentation.
@Component({
  selector: 'app-blog-page',
  imports: [FormsModule],
  templateUrl: './blog-page.html',
  styleUrl: './blog-page.scss',
})
export class BlogPage implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly query = signal('');
  protected readonly category = signal('All');
  protected readonly selected = signal<BlogPost | null>(null);
  protected readonly status = signal<'loading' | 'loaded' | 'failed'>('loading');
  protected readonly posts = signal<BlogPost[]>([]);
  protected readonly detailHtml = signal('');
  // "All" plus the categories present in the data, so the buttons never offer an empty category.
  protected readonly categories = computed(() => [
    'All',
    ...Array.from(new Set(this.posts().map((post) => post.category).filter((value) => !!value))).sort(),
  ]);

  ngOnInit(): void {
    this.http
      .get<Array<Partial<BlogPost> & { createdAtUtc?: string; tags?: string[] }>>('/api/blog')
      .subscribe({
        next: (posts) => {
          this.posts.set(
            posts.map((post) => ({
              slug: post.slug ?? '',
              title: post.title ?? 'Untitled story',
              excerpt: post.excerpt ?? '',
              category: post.category ?? 'Field notes',
              tags: post.tags ?? [],
              date: post.createdAtUtc
                ? new Date(post.createdAtUtc).toLocaleDateString('en-GB', {
                    day: '2-digit',
                    month: 'short',
                    year: 'numeric',
                  })
                : 'Recent',
              readTime: '5 min',
              content: post.content ?? post.excerpt ?? '',
            })),
          );
          this.status.set('loaded');
        },
        error: () => this.status.set('failed'),
      });
  }

  // The body is Markdown. The renderer (raw HTML off, unsafe links refused) is loaded on first reading, so visitors
  // who only browse the list never download it; Angular sanitizes the result again when it is bound.
  protected async open(post: BlogPost): Promise<void> {
    this.detailHtml.set('');
    this.selected.set(post);
    const { renderMarkdown } = await import('../editor/markdown');
    if (this.selected() === post) {
      this.detailHtml.set(renderMarkdown(post.content));
    }
  }

  protected readonly filteredPosts = computed(() =>
    this.posts().filter((post) => {
      const needle = this.query().toLowerCase();
      const matchesQuery =
        !needle ||
        [post.title, post.excerpt, ...post.tags].some((value) =>
          value.toLowerCase().includes(needle),
        );
      return matchesQuery && (this.category() === 'All' || post.category === this.category());
    }),
  );
}
