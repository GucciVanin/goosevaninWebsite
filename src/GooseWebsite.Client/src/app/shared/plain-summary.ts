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

  let end = text.lastIndexOf(' ', limit) > 0 ? text.lastIndexOf(' ', limit) : limit;
  // Never cut inside an emoji: back up to the last whole character (grapheme) that fits, and never inside a surrogate pair.
  let boundary = 0;
  for (const part of new Intl.Segmenter(undefined, { granularity: 'grapheme' }).segment(text.slice(0, end + 2))) {
    if (part.index + part.segment.length > end) break;
    boundary = part.index + part.segment.length;
  }

  end = boundary;

  return text.slice(0, end).trimEnd() + '…';
}
