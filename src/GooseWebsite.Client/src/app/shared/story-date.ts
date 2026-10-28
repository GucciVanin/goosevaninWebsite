const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** The moment a story counts as published: its publication date, or the day it was created when it has none. */
export function storyMoment(story: { publishedAtUtc: string | null; createdAtUtc: string }): string {
  return story.publishedAtUtc ?? story.createdAtUtc;
}

/** "22 Sep 2026", in UTC, whatever the browser's locale (so the same story reads the same everywhere). */
export function storyDate(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? '' : `${date.getUTCDate()} ${months[date.getUTCMonth()]} ${date.getUTCFullYear()}`;
}
