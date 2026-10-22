# Real mail client check

An agent cannot open Gmail, Outlook or a phone, and the logo cannot load while the site address is `localhost`. This checklist is for Gustavo, with a real send. It serves F9-US6-AC1.

## Before you start

- The site must be reachable at a public address, and `PublicBaseUrl` must be that address. Otherwise the logo shows as a broken image or alt text; that is expected and not a failure of the email.
- Send the account verification email by registering a Reader with an address you own. Send the contact emails with a real contact submission (or a MailerSend test message).
- Use a throwaway message text. Do not paste anything private.

## Clients to open each email in

1. Gmail on the web
2. Gmail on a phone (Android or iOS)
3. Outlook (Windows desktop, and Outlook on the web)
4. Apple Mail (Mac or iPhone)
5. Any client in dark mode (or with the device dark mode on)
6. The plain-text view (Gmail: "Show original" or the plain-text setting; Apple Mail: View, Message, Plain Text Alternative)

## What to look for

| Check | Expected |
| --- | --- |
| Logo | Visible once the site is public, with the "GCV, Gustavo Couto Vanin" alt text if images are blocked |
| Look | Near-black navy page, cyan and magenta accents, monospace labels, corner-accent button. Outlook for Windows may show square, flat colours, which is acceptable |
| Link | The button and the printed address both open the verification page (account email) or the confirm page (contact email) |
| Expiry and ignore text | The account email says the link expires in 24 hours and that it can be ignored if the account was not requested |
| Remote content | Show the image and link sources (View source or the client's "Show original"). The only remote image is the logo on the site's own address. No tracking pixel, no third-party host, no script |
| Layout on a phone | One column, readable text, the button easy to tap, no horizontal scrolling |
| Dark mode | Text stays readable; the dark background is not turned into a washed-out or low-contrast colour |
| Plain-text view | The greeting, the full verification link, the expiry sentence and the ignore sentence are all readable without the HTML |
| Contact emails | Names and message text containing `<b>bold</b>` appear as text, not as bold |

## Record the result

Note the client, the device, the date and any problem (with a screenshot) in the F9-US6 evidence in `Docs/product-backlog.md`. Known follow-up: the plain-text alternative of the three contact emails is set in the MailerSend dashboard and must be checked there.
