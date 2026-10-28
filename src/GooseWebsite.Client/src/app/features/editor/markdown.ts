import MarkdownIt from 'markdown-it';

// Raw HTML is switched off, and markdown-it refuses javascript:, vbscript: and data: links, so a story body can format
// text but can never add markup or script. Angular sanitizes the result again where it is shown.
const markdown = new MarkdownIt({ html: false, linkify: true, breaks: false });

// Public pages already have their own h1 (the title), so a body's headings move down by `headingOffset` levels
// (`#` becomes h2); the editor preview keeps the author's levels. Levels stop at h6.
markdown.core.ruler.push('heading_offset', (state) => {
  const offset = (state.env as { headingOffset?: number } | undefined)?.headingOffset ?? 0;
  if (offset <= 0) return;
  for (const token of state.tokens) {
    if (token.type === 'heading_open' || token.type === 'heading_close') token.tag = `h${Math.min(6, Number(token.tag.slice(1)) + offset)}`;
  }
});

export function renderMarkdown(source: string, options: { headingOffset?: number } = {}): string {
  return source.trim() ? markdown.render(source, { headingOffset: options.headingOffset ?? 0 }) : '';
}
