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
  protected readonly categories = ['All', 'Security', 'Field notes', 'Build log'];
  protected readonly posts = signal<BlogPost[]>([
    {
      slug: 'the-first-signal',
      title: 'The first signal',
      excerpt: 'Notes from building a quieter, sharper security practice.',
      category: 'Field notes',
      tags: ['security', 'systems', 'life'],
      date: '22 Sep 2026',
      readTime: '4 min',
      content:
        'A daily field note about curiosity, systems, and the small signals that make software safer.',
    },
    {
      slug: 'threat-modeling-my-morning',
      title: 'Threat modeling my morning',
      excerpt: 'What a commute, a coffee, and a curious brain have in common.',
      category: 'Security',
      tags: ['threat-modeling', 'habits'],
      date: '18 Sep 2026',
      readTime: '6 min',
      content:
        'The best threat models begin with observing what is actually happening, not what we wish were happening.',
    },
    {
      slug: 'shipping-a-tiny-tool',
      title: 'Shipping a tiny tool',
      excerpt: 'A build log for a terminal utility that stayed deliberately small.',
      category: 'Build log',
      tags: ['dotnet', 'tools'],
      date: '09 Sep 2026',
      readTime: '5 min',
      content:
        'Small tools are a useful constraint: every command earns its place and every sharp edge becomes visible.',
    },
  ]);

  ngOnInit(): void {
    this.http
      .get<Array<Partial<BlogPost> & { createdAtUtc?: string; tags?: string[] }>>('/api/blog')
      .subscribe({
        next: (posts) =>
          this.posts.set(
            posts.map((post) => ({
              slug: post.slug ?? '',
              title: post.title ?? 'Untitled signal',
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
          ),
      });
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
