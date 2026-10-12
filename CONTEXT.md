# GooseWebsite

Gustavo Couto Vanin's personal website: a public profile, a contact form that verifies the sender, a private editor for projects and stories, and later public browsing and reader comments.

## Language

**Gustavo**:
The site's author. Use this or "Gustavo Couto Vanin" in public text.
_Avoid_: the owner

**Site administrator**:
An account provisioned through the private operator path that may use the private editor. Gustavo is one.
_Avoid_: admin user, owner

**Reader**:
A visitor with a self-registered account, introduced for commenting. A Reader is never a Site administrator.
_Avoid_: member, subscriber

**Story**:
A piece of personal writing published on the site, with title, body, publication date, cover image and tags.
_Avoid_: blog post, article (in product language)

**Project**:
A piece of Gustavo's work presented with his role, tools and outcomes.
_Avoid_: portfolio item, case

### Contact and verification

**Contact submission**:
What a sender fills in on the contact form: name, email, reason and message.
_Avoid_: inquiry, ticket

**Pending message**:
A Contact submission held in memory until the sender completes Verification. It is never stored durably.
_Avoid_: draft, queued message

**Verification**:
A sender or Reader proving they control an email address by clicking the Verification link sent to it.
_Avoid_: confirmation, validation

**Verification link**:
The one-time link emailed to an address to complete Verification. It expires and works once.
_Avoid_: confirmation link, magic link

**Delivery outcome**:
The result of sending a verified Contact submission to Gustavo and the sender: delivered, failed, or Gustavo notified but the sender's receipt failed.
_Avoid_: status, result
