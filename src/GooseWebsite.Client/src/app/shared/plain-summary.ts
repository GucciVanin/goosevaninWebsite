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
