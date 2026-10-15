import MarkdownIt from 'markdown-it';

// Raw HTML is switched off, and markdown-it refuses javascript:, vbscript: and data: links, so a story body can format
// text but can never add markup or script. Angular sanitizes the result again where it is shown.
const markdown = new MarkdownIt({ html: false, linkify: true, breaks: false });

export function renderMarkdown(source: string): string {
  return source.trim() ? markdown.render(source) : '';
}
