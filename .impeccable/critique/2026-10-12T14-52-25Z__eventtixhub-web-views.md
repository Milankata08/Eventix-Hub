---
target: "EventTixHub.Web/Views after final polish (PR #10)"
total_score: 31
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 1
target_identity: "file:C:\\Users\\Kris\\Desktop\\eventixhub\\.claude\\worktrees\\eventtixhub-web-polish-c1ee66\\EventTixHub.Web\\Views"
timestamp: 2026-10-12T14-52-25Z
slug: eventtixhub-web-views
---
# Critique: EventTixHub.Web/Views (after the 2026-10-12 final polish, PR #10, commit 581d82a)

Method: dual-agent (A: design review · B: detector + browser).

## Design Health Score
| # | Heuristic | Score | Key issue |
|---|---|---|---|
| 1 | Visibility of System Status | 4 | Hold shown on seat page, cart, checkout and nav; live region per seat; honest counts |
| 2 | Match System / Real World | 3 | Cart shows 31,50 € next to "Редовен" with no promo breakdown |
| 3 | User Control and Freedom | 3 | Remove per seat, back links, cancel; ticket "Назад" is generic |
| 4 | Consistency and Standards | 3 | Poster meta differs by type ("12:00." stray period, time twice on film/concert) |
| 5 | Error Prevention | 3 | Cancel confirmed only by native confirm(); "Покажи билета" and "Откажи" side by side |
| 6 | Recognition Rather Than Recall | 3 | Seat page 35,00 € vs cart 31,50 €: buyer must infer the promo |
| 7 | Flexibility and Efficiency | 3 | Autosubmitting filters, roving tabindex; concession needs "Приложи" per seat |
| 8 | Aesthetic and Minimalist Design | 3 | Strong posters; empty right column on details; noisy "Намаление за ред 5, място 7" label |
| 9 | Error Recovery | 3 | Field errors excellent; stale antiforgery POST gives a blank 400 page |
| 10 | Help and Documentation | 3 | Inline rules: 10 min hold, 10 tickets, cards checked at the door, simulated payment |
| **Total** | | **31/40** | Good |

## Design Specificity Verdict
Authored: afiш poster wall in Sofia Sans Extra Condensed, one ink per art form, per-type poster layouts, stage bar in the type ink, tinted VIP rows, perforated QR stub. Generic spots: native confirm(), plain Bootstrap form cards in the cart, the profile ticket list.
Detector: CLI file scan [] (skips .cshtml). URL scans of /, /Events/Details/5, /Account/Login, /Account/Register at 1280x800 and 375x812: 0 findings. Browser overlay on /, details, login, seats/27, cart, checkout at 1280 and 375: 3 findings, all false positives (repeating-stripes-gradient = functional .seat-held hatch; body-text-viewport-edge on .summary-promise, which has 16px padding).

## Priority Issues
1. [P1] Cart and checkout have no price breakdown: line prices silently include the promo (31,50 € next to "Редовен"), no subtotal or discount line (Cart.cshtml:74, :116-128; Checkout total only). Fix: base fare per line, then "Междинна сума 70,00 € · WELCOME10 −7,00 € · Общо 63,00 €". Command: clarify.
2. [P2] Concession is a form per seat: select + "Приложи" + long visible label for each seat (Cart.cshtml:59-71); heavy on a phone. Fix: data-autosubmit on the select (site.js pattern exists) with a noscript button; visible label "Намаление", seat in a visually-hidden span. Command: distill.
3. [P2] Blank 400 page after a stale antiforgery POST: status re-execute (Program.cs:90) re-runs /Home/Status/400 as a POST and the global antiforgery filter (Program.cs:44) rejects it again. Fix: [IgnoreAntiforgeryToken] on Status (HomeController.cs:24) and 400 copy "Сесията изтече, опитайте отново". Command: harden.
4. [P3] Ticket page has no h1 and no type word: type shown by the top border colour only (_TicketCard.cshtml), breaking "colour is never the only signal". Fix: h1 or promoted title plus the type badge. Command: polish.
5. [P3] Checkout first-error focus on a phone may leave the error line under the sticky "Плати" bar (A measured field to y=763, error y=765-784, bar from y=742; likely partly an artifact of the hidden pane, where smooth scroll does not run). Fix if confirmed on a device: focus({preventScroll:true}) + scrollIntoView({block:'center'}) for the first error. Command: harden.

## Persona Red Flags
- Casey: ~4 taps per seat for concessions; on checkout the order summary pushes the card form below the fold; "СЦЕНА" centred on the full hall width, not the visible part.
- Riley: blank 400 on a stale session; 403 page offers only "Към афиша"; cancel relies on window.confirm.
- Sam: ticket page has no h1; Афиш has two <nav> landmarks (site nav + "Вид" chips, Index.cshtml:25). Seat map is strong.
- Audience member at the door: profile lists one card per seat with no grouping by showtime (Profile/Index.cshtml:41-55); finding tonight's tickets is slow.

## Minor Observations
Stray period "12:00." on film/concert posters and time repeated in the footer strip; city select orphaned on its own row under the chips; details page dead space on the right at desktop; disabled vs enabled "Добави в количката" close in value; nav hold badge pushes desktop links right.

## Questions to Consider
- Should the cart be one decision ("Кой ще ползва намаление?") instead of a form per seat?
- Is the profile a ticket wallet or an order history? Grouping by showtime may be the real product.
- Why does the ticket, the artefact people keep, get less афиш treatment than the poster they scroll past?
