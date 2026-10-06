# Redesign verification (2026-10-06)

Captures: `desktop-<page>-<light|dark>.png`, `mobile-<page>-<light|dark>.png` (1280 and 390 px) for afisha, film, concert, theatre, seats, cart, checkout, confirmation, ticket, profile, login, 404, admin-dashboard, admin-events, admin-venue.

## Detector (`impeccable detect --json`, 7 public URLs, 1280 and 390 px)
Before: tight-leading and all-caps-body on poster label lines (both viewports), body-text-viewport-edge on the footer (390 px).
Fixed: `.label-caps` line-height 1.35; poster label lines sentence case (month stays caps); `.container` gutter 16 px.
After: 0 findings at 1280 px; 2 at 390 px, both `clipped-overflow-container` on the details band poster (`/Events/Details/1`, `/4`). Justified: the clipped positioned child is the 1 px `.visually-hidden` date text, and `overflow: hidden` is the poster's job. Pages behind login were checked from captures, not the URL scan.

## Defect found in captures (fixed)
`.type-badge` had no rule, so Bootstrap's white badge text was invisible on paper in light mode (seats page). Now ink text with a 3 px underline in the type ink.

## Contrast, measured in the browser
| Pair | Light | Dark |
|---|---|---|
| `.text-danger` on page | 5.87 | 8.18 |
| outline-secondary / outline-primary text | 16.14 | 15.88 |
| outline-danger text | 5.87 | 8.18 (border is transparent by design: quiet text action) |
| btn-primary / btn-secondary text on fill | 17.32 | 14.58 |
| free seat outline vs page | 3.43 | 4.25 |
| free seat number on seat | 17.32 | 12.32 |
| review stars | 16.14 (was 1.63) | 15.88 |
Critique's 3.29–3.43 dark pairs are now 8.18–15.88.

## Finish review
No `impeccable-finish-reviewer` agent exists in this environment, so the review was done by the implementer from the captures, detector output and contrast table above. Scope: self-review only; no independent verdict.

## Not done by the agent
Manual checks M59–M69 (Chrome and Firefox) and the keyboard-only phone purchase stay with the user.

## DESIGN.md drift from the seed
Seat radius 10/10/4/4 px (seed 7/7/2/2); label line-height 1.35 (seed 1.2); type badge is ink text with a type underline, not a filled field; gutter 16 px at all widths; poster fit factor stays 0.55.
