import { safeReturnUrl } from './return-url';

// After signing in, only a path inside this site may be followed; anything else is ignored (no open redirect).
describe('safeReturnUrl', () => {
  it('accepts a path on this site, with or without a query', () => {
    expect(safeReturnUrl('/editor')).toBe('/editor');
    expect(safeReturnUrl('/editor/stories?draft=1')).toBe('/editor/stories?draft=1');
  });

  it('refuses addresses on other sites and other tricks', () => {
    const refused = [
      'https://evil.example/editor',
      '//evil.example/editor',
      '/\\evil.example',
      '\\\\evil.example',
      'javascript:alert(1)',
      'editor',
      '/editor with spaces',
      '',
    ];

    for (const value of refused) {
      expect(safeReturnUrl(value)).withContext(value).toBeNull();
    }
  });

  it('refuses a missing value', () => {
    expect(safeReturnUrl(null)).toBeNull();
    expect(safeReturnUrl(undefined)).toBeNull();
  });
});
