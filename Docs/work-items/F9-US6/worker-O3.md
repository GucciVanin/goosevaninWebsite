# Worker record - order O3 (F9-US6)

- Test first: new `LogoSizeKeepsTheRealImageAspectRatio` reads the PNG header of `src/GooseWebsite.Client/public/email/gcv-logo.png` (360x299) and asserts the `<img>` height matches width x ratio within 1 px. Red observed: it failed (height 150 vs expected 124.6, off by 25.4).
- Fix: `AccountVerificationEmailRenderer.cs` logo is now `width="150" height="125"`.
- `WhenSmtpIsNotConfiguredNothingIsSentAndItReportsFailure` now asserts `Assert.False(result)` plus nothing sent; the capturing sink reports failure for an empty call like the real sender (it returned true before, so the name was not what it asserted). That test passed both before and after (its sink change was the tightening), so no red for item 2.
- Preview `Docs/email-templates/account-verification-preview.png` re-rendered in headless Chrome from the real renderer output (a temporary test wrote the HTML to the scratchpad; the logo pointed at the local file; the temp test was deleted) and viewed: logo no longer stretched.
- Totals: `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` 169 of 169 passed.
