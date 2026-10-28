import { plainSummary } from './plain-summary';

describe('plainSummary', () => {
  it('cuts a long summary at a word boundary', () => {
    const text = plainSummary('word '.repeat(100), 50);

    expect(text.length).toBeLessThanOrEqual(51);
    expect(text.endsWith('…')).toBeTrue();
    expect(text).not.toContain('wor…');
  });

  it('removes the Markdown markup and keeps the link text', () => {
    expect(plainSummary('A **personal** site.\n\nIt has a [blog](https://example.test).')).toBe('A personal site. It has a blog.');
  });

  it('never leaves half of an emoji when it cuts, even without a space to cut at', () => {
    const text = plainSummary('👍🏽'.repeat(100), 31);

    expect(text).not.toMatch(/[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]/);
    expect(text.endsWith('…')).toBeTrue();
  });
});
