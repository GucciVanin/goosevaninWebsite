// What the editor shows about a story. The API still calls them blog posts internally; the product name is "story".

/** One row of the workspace list: no body. */
export interface StorySummary {
  id: number;
  title: string;
  slug: string;
  category: string;
  isPublished: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  publishedAtUtc: string | null;
}

/** A story opened for editing or preview. */
export interface Story extends StorySummary {
  excerpt: string;
  content: string;
  tags: string[];
  coverImage: string;
  coverImageAlt: string;
  authorId: string;
  authorName: string;
}

/** What Gustavo can save. Saving never publishes; that is a separate action. */
export interface StoryDraft {
  title: string;
  excerpt: string;
  content: string;
  category: string;
  tags: string[];
  coverImage: string;
  coverImageAlt: string;
  publishedAtUtc: string | null;
}
