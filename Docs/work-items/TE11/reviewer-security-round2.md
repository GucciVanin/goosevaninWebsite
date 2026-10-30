# TE11 round 2 - Reviewer and Security (commit ad493c4)

Verdict on the Worker round 2 report: accurate (one wording nit: "failed before the fix" was reasoned, not run; reasoning below confirms it).
Verdict on the commit: pass.

## F1 (mailto steering): closed
- Both sites that put the sender address into a URL (the `emailCell` link and the reply `Button`) now use `Encode(Uri.EscapeDataString(senderEmail))`. These are the only two: the confirmation and receipt mails take only the site and verify URLs (token-based), never the address.
- `EscapeDataString` leaves only unreserved characters and `%XX`, so `?`, `&`, `=`, `@` cannot form query parameters; the HTML encode afterwards is a no-op but correct order. No double-encoding problem (Button takes an already-encoded href).
- Visible text and plain text still show the raw address HTML-encoded / plain; Reply-To header unaffected.
- Test `OwnerNotice_PercentEncodesTheSenderAddressInTheMailtoLink`: exact href assertion plus a regex loop over all mailto hrefs forbidding `?` and `&`. Without the fix the href would contain `?bcc=x&body=` and the test fails. Meaningful.

## Truncate: correct
- Empty or length <= 60: returned unchanged. Exactly 60: unchanged. Length > 60 with a high surrogate at index 59: cut at 59 (pair dropped whole). Low surrogate at index 59 (complete pair ending at the cut): kept at 60. A lone high surrogate at 59 is also dropped (harmless).
- Test places the emoji at indices 59-60, asserts the label is 59 `a` and no lone surrogate anywhere in the HTML. Without the fix the label would carry a lone high surrogate, so both assertions fail. Meaningful, though it only covers the straddle case, not the "pair ends exactly at the cut" case (minor, untested but correct by reading).

## Unsubscribe footer tests: adequate
- Subscription test pins the exact footer markup with a regex (link to `/unsubscribe#token=`); it would fail if the footer or link changed.
- `ContactMails_HaveNoUnsubscribeFooter` covers only `Confirmation`, not `OwnerNotice` or the receipt (advisory; those pass null the same way). It would fail if a footer were added to that mail.

## Scope
- Production diff is only `ContactEmailRenderer.cs` (13 added, 3 removed lines): the two href changes and Truncate. Rest is tests and the Worker log. Nothing else changed.
- `dotnet test` filtered to the 4 new tests: 4 passed, 0 failed.

## Findings
- None blocking. Advisory: extend the no-unsubscribe test to OwnerNotice and the receipt mail.
