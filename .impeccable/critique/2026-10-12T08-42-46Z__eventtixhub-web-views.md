---
target: EventTixHub.Web after polish sweep
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:C:\\Users\\Kris\\Desktop\\eventixhub\\.claude\\worktrees\\eventtixhub-polish-sweep-68a49d\\EventTixHub.Web\\Views"
timestamp: 2026-10-12T08-42-46Z
slug: eventtixhub-web-views
---
# Critique: EventTixHub.Web/Views (after the 2026-10-12 polish sweep, commit 5ea3746)

Method: dual-agent (A: design review, B: detector + browser).

## Design Health Score
| # | Heuristic | Score | Key issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | Seat page does not recognise the buyer's own held seats; shows "0 места" while the hold ticks |
| 2 | Match System / Real World | 4 | Афиш, event-type wording, "Ред 3, място 7", 12,50 € |
| 3 | User Control and Freedom | 3 | Concessions need select + "Приложи" per seat |
| 4 | Consistency and Standards | 2 | "Количка" active on Seats (_Layout.cshtml:41); theatre poster layout differs; "Място"/"място" casing |
| 5 | Error Prevention | 3 | No warning when re-picking while holding seats |
| 6 | Recognition Rather Than Recall | 3 | No price on the Афиш |
| 7 | Flexibility and Efficiency | 2 | Full reload per filter; no "Днес / уикенд" shortcut |
| 8 | Aesthetic and Minimalist Design | 3 | Strong posters; empty paper in cart/checkout |
| 9 | Error Recovery | 2 | No invalid-field styling, no aria-invalid |
| 10 | Help and Documentation | 2 | Nothing explains hold expiry |
| **Total** | | **27/40** | Acceptable |

## Design Specificity Verdict
Authored, not interchangeable: type-ink posters, condensed Cyrillic display, hall plan, hold badge, ticket stub. Operate surfaces (cart, checkout, profile) fall back to boxed Bootstrap cards.
Detector: CLI skips .cshtml (scanned nothing); URL scans of /, /Events/Details/5, /Account/Login, /Account/Register at 1280 and 375: 0 findings. Browser overlay: only /Booking/Seats/27 — light sold-seat numbers 4.4:1 (#57554F on #C8C6BF, real near-miss, disabled-control exempt); dark held-seat numbers 3.0:1 against the light hatch stripe; hatch "stripes" and summary-promise viewport-edge are false positives.

## Priority Issues
1. [P1] Invalid fields look valid: no .input-validation-error style, no aria-invalid (checkout, login, register). Fix: danger border + aria-invalid via jquery-validate highlight/unhighlight in site.js. Command: harden.
2. [P1] Seat page ignores the buyer's own hold: own seats show as "резервирано", bar says "0 места". Fix (copy only): "Имате N места в количката → Към количката" in the summary bar when a hold exists. Command: clarify.
3. [P2] "Количка" nav active on Seats (_Layout.cshtml:41). Fix: Cart/Checkout actions only. Command: polish.
4. [P2] No price on the Афиш; rating line under some cards only (Home/Index.cshtml:110), ragged rows. Fix: fixed meta row with "от … €" and rating. Command: layout.
5. [P3] Seat-number contrast: light sold 4.4:1; dark held digits on stripes. Fix: darken --seat-sold slightly; solid backing for held numbers. Command: polish.

## Persona Red Flags
- Casey: plan opens scrolled so seats 1–3 hidden; scroll hint below the plan (_Hall.cshtml:49); sticky "Плати" bar covers cardholder field; profile has no "tonight" first.
- Sam: no aria-invalid; duplicate hold badge announced twice on cart/checkout.
- Riley: WELCOME10 carried into a fresh cart; no warning re-entering selection while holding; search autosubmits on blur.

## Minor Observations
Date filter placeholder "dd-----yyyy"; empty right column on details; "Назад" off-baseline next to "Печат" on ticket; checkout lines drop category/concession; admin dashboard has no drill-in from critical showtimes.

## Questions to Consider
- Why does the seat map, where the hold matters most, not know which seats are yours?
- Should the Афиш sell the evening ("Тази вечер") rather than the title?
- Is the invitation per order rather than per seat?
