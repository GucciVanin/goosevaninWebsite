# Manual browser check: real Edge and Safari (F9-US7-AC5)

The automated browser tests (`Docs/tests/README.md`, "Browser tests") run on Chromium, Firefox and WebKit, the engines behind Chrome and Edge, Firefox, and Safari. They are not the branded browsers. This list is the manual pass for **Microsoft Edge** and **Safari**, **latest two versions of each**, that Gustavo runs by hand. It also covers the few things the automation cannot judge (how it looks, how it feels).

What is claimed: AC5 is **automated on the three engines** and **manual for real Edge, real Safari and the previous version of each**. Nothing in this list has been run by an agent.

## Before you start

- Open the running site (the container stack: `https://localhost`; accept the local certificate) in the browser and version you are checking. Note the version (Edge: `edge://settings/help`; Safari: Safari menu, About Safari).
- Use a normal window and, for the mobile items, the browser's responsive mode or a phone.
- Do **not** send a real contact message unless you want the verification email to be sent to you.

## What to open and what to look for

Tick each line for each browser and version. Write anything odd in the Notes column of the table below.

### 1. Home (`/`)

- [ ] **Layout:** hero, About (portrait, your name under it, the positioning line, short bio and summary), Work, Stories, Contact, footer (no Skills section since TE4). Nothing overlaps, is cut off, or sits outside the page. Nothing scrolls sideways.
- [ ] **The animated scene** in the hero moves smoothly (no stutter, no blank or broken canvas), text over it stays readable, and the page is usable while it runs.
- [ ] **Reduced motion:** turn on "Reduce motion" (Windows: Settings, Accessibility, Visual effects, Animation effects off; macOS: System Settings, Accessibility, Display, Reduce motion). Reload. The scene and the other animations stop or become minimal, and no content is missing.
- [ ] **Focus:** press Tab from the top. Every link, button, field and menu item gets a clear, visible outline, in a sensible order. Buttons work with Enter or Space.
- [ ] **Header and menu:** the section links scroll to their sections; at phone width the menu button opens and closes a drawer with the same links.
- [ ] **Links:** GitHub and LinkedIn open in a new tab; the email link opens your mail program.
- [ ] **Colour is not the only signal:** hover, focus, the active section and any error or status are also shown by shape, underline, border or words.

### 2. Contact flow (home page, Contact section)

- [ ] The form has name, email, reason (`Work or collaboration`, `Personal note`) and message, each with a visible label that you can click to focus the field.
- [ ] With a field empty, or an email such as `abc`, an error in words (for example "Enter your name", "Enter a valid email address") appears beside each invalid field after you leave it, or after you press Send or Enter, and keyboard focus moves to the first invalid field. Nothing is sent (no verification email arrives). Say in Notes if it is not obvious which field needs attention.
- [ ] Keyboard only: you can fill every field and reach the Send button in order, with a visible focus each time.
- [ ] (Optional, sends one real verification email to the address you type) Submit a valid message. The page says it must be confirmed by email and does **not** say it was delivered. Open the email link, confirm, and see the confirmation screen.

### 3. Account screens

- [ ] `/sign-in`: the form, the focus outline, the error message for a wrong password (shown as text, not only colour).
- [ ] `/verify-email` and `/contact/verify` (opened without a token): they explain the problem in words and offer a way back.
- [ ] `/access-denied`: clear message and a way back.

### 4. Editor (signed in as the administrator)

- [ ] `/editor`: the workspace lists stories and projects; the Editor link in the header appears only when signed in as administrator.
- [ ] New story and new project forms: Tab through every field including the **date field** (a visible outline must appear on it in Safari too), the image controls and the Save button.
- [ ] Layout at phone width: the forms fit, no sideways scroll.
- [ ] Sign out: the Editor link disappears and `/editor` sends you to sign in.

### 5. Both browsers

- [ ] Zoom to 200%: the home page and the contact form remain usable and nothing is lost.
- [ ] Dark text on dark background or light on light anywhere is a failure; note the screen.

## Where to record the result

Add one row per browser and version to the table below (this file), then commit it. If something fails, open a GitHub issue with the label `needs-triage` and put its number in Notes.

| Date | Browser and version | Operating system | Result (pass, or what failed) | Notes or issue |
| --- | --- | --- | --- | --- |
| | Edge (latest) | | | |
| | Edge (previous) | | | |
| | Safari (latest) | | | |
| | Safari (previous) | | | |
