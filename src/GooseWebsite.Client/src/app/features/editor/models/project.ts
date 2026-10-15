// What the editor shows about a project.

/** One row of the workspace list: no body. */
export interface ProjectSummary {
  id: number;
  title: string;
  slug: string;
  isPublished: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  publishedAtUtc: string | null;
}

/** A project opened for editing or preview. */
export interface Project extends ProjectSummary {
  overview: string;
  contribution: string;
  technologies: string[];
  demoUrl: string | null;
  sourceUrl: string | null;
  coverImage: string;
  coverImageAlt: string;
}

/** What Gustavo can save. Saving never publishes; that is a separate action. */
export interface ProjectDraft {
  title: string;
  overview: string;
  contribution: string;
  technologies: string[];
  demoUrl: string | null;
  sourceUrl: string | null;
  coverImage: string;
  coverImageAlt: string;
  publishedAtUtc: string | null;
}
