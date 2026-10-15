import { renderMarkdown } from './markdown';

// A story's body is Markdown, shown safely: formatting works, raw HTML and script links never do.
describe('renderMarkdown', () => {
  it('renders headings, emphasis, lists and links', () => {
    const html = renderMarkdown('# Title\n\nSome *emphasis* and a [link](https://example.com).\n\n- one\n- two');

    expect(html).toContain('<h1>Title</h1>');
    expect(html).toContain('<em>emphasis</em>');
    expect(html).toContain('<a href="https://example.com">link</a>');
    expect(html).toContain('<li>one</li>');
  });

  it('keeps paragraphs apart', () => {
    expect(renderMarkdown('first\n\nsecond')).toContain('<p>first</p>');
    expect(renderMarkdown('first\n\nsecond')).toContain('<p>second</p>');
  });

  it('never lets raw HTML through', () => {
    const html = renderMarkdown('<script>alert(1)</script> <img src=x onerror=alert(1)>');

    expect(html).not.toContain('<script>');
    expect(html).not.toContain('<img');
    expect(html).toContain('&lt;script&gt;');
  });

  it('does not turn a script link into a link', () => {
    const html = renderMarkdown('[click](javascript:alert(1)) and [data](data:text/html;base64,AAAA)');

    expect(html).not.toContain('href="javascript');
    expect(html).not.toContain('href="data');
  });

  it('shows nothing for an empty body', () => {
    expect(renderMarkdown('')).toBe('');
    expect(renderMarkdown('   ')).toBe('');
  });
});
