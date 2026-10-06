---
target: whole site
total_score: 23
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 2
target_identity: "file:C:\\Users\\Kris\\Desktop\\eventixhub\\EventTixHub.Web\\Views"
timestamp: 2026-10-06T12-56-29Z
slug: eventtixhub-web-views
---
# Critique: EventTix Hub, the whole site (`Views` + `Areas/Admin/Views`)

Method: dual-agent (A: design-review sub-agent · B: detector sub-agent)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 2 | The hold timer is shown only in the cart (`Cart.cshtml:26-28`); there is none on checkout or in the nav. The nav has no active-page state (`_Layout.cshtml:27-43`). If the seat API fails, the map shows "Зареждане…" forever (`seatmap.js:93-97`). |
| 2 | Match System / Real World | 3 | The Bulgarian domain copy is strong. Concerts and plays get film words: "Предстоящи прожекции" (`Events/Details.cshtml:40`) and "ЕКРАН / СЦЕНА" (`Seats.cshtml:20`). `InvariantCulture` (`Program.cs:17-18`) prints "9.00 €", not "9,00 €". Date inputs show the browser's English format. |
| 3 | User Control and Freedom | 3 | Exits are present everywhere (Отказ, Премахни, Изчисти). The hold can't be extended, and expiry and errors send the user back to the Афиш (`BookingController.cs:91-95`, `Cart.cshtml:17-23`). |
| 4 | Consistency and Standards | 2 | Filter buttons look different in public and admin (`Home/Index.cshtml:61-62` vs `Admin/Events/Index.cshtml:36-37`). Deletes use a native `confirm()` in some places and a dedicated page in others. English validation messages appear next to Bulgarian ones (`AdminVMs.cs:108-112`). |
| 5 | Error Prevention | 2 | A past showtime's seat map stays fully clickable until submit. The 10-minute hold and the 10-ticket limit are never stated before you commit. Card-number client validation throws an error. Admin price fields default to an invalid 0, and making someone admin takes one click. |
| 6 | Recognition Rather Than Recall | 3 | The legend, prices and cart lines are visible. Your own holds look like other people's ("резервирано"). The summary shows how many seats, not which (`Seats.cshtml:33`). The nav shows no cart count and no time left. |
| 7 | Flexibility and Efficiency | 1 | No "best available" pick, no quick date choices, no admin sort or bulk actions. The keyboard path through the seat map is broken (`seatmap.js:29`). |
| 8 | Aesthetic and Minimalist Design | 2 | The "Без плакат" placeholder is the largest element on the Афиш. Admin lists have ten red "Изтриване" buttons each. Messages are repeated (`Events/Details.cshtml:33` + `:113`; `Cart.cshtml:9-21`). |
| 9 | Error Recovery | 3 | Errors are specific and Bulgarian ("Ред 3, място 7 е заето.") and linked to their fields with `aria-describedby`. The promo error shows at the page top and wipes the code. Card fields are wiped after a server error (`BookingController.cs:97-98`). |
| 10 | Help and Documentation | 2 | Good inline hints (concession cards are checked at the door; payment is simulated). The hold length, the 24 h cancel rule, the ticket limit and the password rules are never stated. There is no help, FAQ or contact page. |
| **Total** | | **23/40** | **Acceptable** |

## Design Specificity Verdict

**Design review:** the look is interchangeable with any Bootstrap app.

- **Template leftovers:** it is the stock `dotnet new mvc` template plus Bootstrap 5.3 defaults:
  - the navbar (`navbar-toggleable-sm box-shadow`, and an `aria-controls="navbarSupportedContent"` that points at nothing, `_Layout.cshtml:18-21`)
  - the footer (`:85`)
  - `site.css:1-31` (14 px root font on mobile, `#258cfb` focus ring)
- **Defaults everywhere else:** colour `#0d6efd`, `system-ui` type, plain cards, badges and tables.
- **Nothing says cinema, concert, theatre or Bulgaria:**
  - None of the 10 events has a poster, so a 220 px grey "Без плакат" box tops every card (`Home/Index.cshtml:77-84`).
  - Филм, Концерт and Театър share one blue badge (`:86`).
  - Every event uses film words.
  - The purchase ends on a receipt: "Поръчка №1 е платена" (`Confirmation.cshtml:6`), and the ticket is 7 label/value lines (`_TicketCard.cshtml:8-18`).
- **Flat type scale:** every page title is an `h3`-sized 28 px heading.
- **Only the seat map and the QR code are designed for this product.** The Bulgarian voice lives entirely in the copy. Swap the strings and this is a dentist booking app.

Surface types (they decide which heuristics apply):

- The Афиш and event pages count as Persuade (win the visitor over).
- The booking flow, profile and admin count as Operate (complete a task).
- Because Operate pages are included, all 10 heuristics are scored.

**Automated scan:**

- **Razor views:** the scanner can't read `.cshtml`. Scanning the folders skipped all 40 views but still reported "clean". Passing the views one by one only does text matching, which can't see `site.css` or Bootstrap: 0 findings.
- **Live-page scans instead:** run on `/`, `/Events/Details/1`, `/Account/Login` and `/Account/Register`, logged out.
  - Desktop 1280 px: 0 findings.
  - Mobile 390 px: 23 findings from 3 rules, on 2 pages.
- **`undersized-ui-text` ×21:** type and genre badges render at 10.5 px (`Home/Index.cshtml:86-87`, `Events/Details.cshtml:22-23`).
  - Root cause: `site.css:1-3` sets `html { font-size: 14px }` below 768 px, and Bootstrap's `.badge` is .75em.
  - The same mechanism shrinks the seat numbers (`site.css:63`). That one is computed, not a scanner finding.
- **`body-text-viewport-edge` ×1:** the page side margin is 10.5 px at the 14 px root (`_Layout.cshtml:76`). Half a false alarm: the text isn't touching the edge, the margin is just under the rule's 16 px minimum.
- **`edge-flush-cards` ×1:** fires on the showtimes table. False alarm as worded. The real issue behind it: the 498 px table scrolls sideways in a 369 px box. This matches the review finding that "Избери места" is off-screen.
- **No "AI-generated look" rules fired** (gradient text, overused fonts, AI colour palette). That agrees with the review: the problem is template defaults, not AI styling.
- **Where they agree:** both point at mobile (the 14 px root font, tables scrolling sideways). The scan added the 10.5 px badge measurements and confirmed test data on the public home page. It missed everything behind login (seat map, cart, checkout, admin) and the dark-mode contrast that the review measured at 3.29–3.43:1.

**Browser overlays:** the scanner was run inside the browser on 5 desktop pages (dark theme) and 2 mobile passes.

- Desktop: all 5 clean.
- Mobile: `/` had 19 findings, `/Events/Details/1` had 5.
- The tab "[Human] Афиш - EventTix Hub" is still open but shows no highlight boxes. The last page was clean on desktop, and the mobile highlights hide when the window is resized. The highlighting helper has been stopped, so it can't be re-run. There's no visible overlay to look at.

## Overall Impression

The engineering is solid but it wears a template skin. The seat map's live conflict handling and the Bulgarian copy are genuinely good. Everything else looks like `dotnet new mvc`.

Booking breaks exactly where the stakes peak:

- keyboard users lose focus on the seat map
- the hold timer disappears on checkout
- on a phone, the booking button and the totals are off-screen

Biggest opportunity: make the event and the hall the hero.

- When there's no poster, build one from the event type.
- Draw the seat map as the real hall.
- Write the confirmation as an invitation.

## What's Working

1. **The live seat map handles conflicts honestly.**
   - It polls every 5 s and disables sold and held seats.
   - If someone takes a seat you picked, it removes it and names it (`seatmap.js:101-107`). Server errors name the seat too.
   - Every seat button has a full accessible name (`seatmap.js:49-50`).
2. **The Bulgarian microcopy knows the domain.**
   - Examples: "Афиш", "Студент −20%", "Студентската и пенсионерската карта се проверяват на входа.", "Плати 70.00 €", and an honest "Симулирано плащане…".
   - Validation messages are in Bulgarian and linked to their fields with `aria-describedby`. Focus jumps to the first error.
3. **The theme switch is built carefully.**
   - The theme is applied before first paint (`_Layout.cshtml:7-10`), and the toggle exposes `aria-pressed`.
   - Printing forces light mode (`site.js:32-35`).
   - The QR code keeps a white margin in dark mode.

## Priority Issues

**[P0] The seat map can't be used with a keyboard or screen reader**

- **What:** `seatmap.js:29` (`grid.replaceChildren()`) rebuilds every seat button on each click (`:76-81`) and on each 5 s poll (`:113-114`).
  - Measured: pressing Enter on "Ред 5, място 7" selected it, then focus fell to `<body>` 49 ms later.
  - The whole grid is `aria-live="polite"` (`Seats.cshtml:21`, and admin `Showtimes/Details.cshtml:20`), so each rebuild can re-announce 70–140 seats.
  - There's no arrow-key movement: up to 140 Tab presses, reset every 5 s.
- **Why:** keyboard and screen-reader users cannot finish a purchase.
- **Fix:**
  - Render the grid once, then update class, `disabled`, `aria-pressed` and label in place, keyed by seat id.
  - Never replace the focused button. Give the grid a single tab stop, with arrow keys moving inside it.
  - Move `aria-live` to the summary and announce only changes ("Добавено: ред 3, място 5 · общо 35,00 €").
- **Command:** `/impeccable harden`

**[P1] Reassurance disappears between the hold and payment**

- **What:**
  - Nothing near "Добави в количката" mentions the hold rule (`Seats.cshtml:34`).
  - The cart timer is a calm `alert-info` box that looks the same at 0:30. Its `role="status"` region is rewritten every second (`Cart.cshtml:26-27`, `site.js:14`).
  - Checkout has no timer at all.
  - On expiry: two duplicate messages, and the only exit is "Към афиша" (`Cart.cshtml:9-23`).
  - If the hold lapses mid-payment, you're sent back to the cart and the card fields are not refilled (`BookingController.cs:91-98`).
  - Card-number validation throws in the browser. The `CreditCard` attribute (`BookingVMs.cs:51`) needs `additional-methods.js`, which isn't loaded (`_ValidationScriptsPartial.cshtml:1-2`). So every typo costs a trip to the server, which wipes the card fields.
  - `autocomplete="off"` with no `cc-*` tokens blocks card autofill (`Checkout.cshtml:29`).
- **Why:** anxiety and loss at the moment of paying. A lapsed hold is the memory people leave with.
- **Fix:**
  - Show a hold badge in the nav and the checkout header ("Пазим местата ви още 7:42"). Make it more urgent under 2:00, and announce it only at 5:00, 1:00 and 0:00.
  - State the hold rule next to the button.
  - On expiry: one message naming the seats, plus "Опитай отново със същите места" linking to `/Booking/Seats/{id}`.
  - Load `additional-methods.min.js` (already in `wwwroot/lib`) and add the `cc-*` autocomplete tokens.
- **Command:** `/impeccable harden`

**[P1] On a phone, the booking button, the money and the tickets are hidden or misread (375 px)**

- **What:**
  - Event details: "Избери места" is off-screen in a 354 px scroll box, under a 300 px "Без плакат" block (`Events/Details.cshtml:16`, `:47-76`).
  - Cart: a 484 px table in a 354 px box hides the prices, "Премахни" and the total (`Cart.cshtml:30-80`).
  - Checkout: a line reads "място 535.00 €" (seat 5 and 35.00 € with no gap, `Checkout.cshtml:14-16`).
  - Profile: a 638 px table hides "Детайли", the only route to the QR (`Profile/Index.cshtml:39-78`).
  - Seat map: seats shrink to 20×28 px (`site.css:55-67`).
  - Badges render at 10.5 px.
  - Admin tables lack `.table-responsive`, so pages scroll sideways.
- **Why:** phones are how people buy tonight's tickets and show the QR at the door. A misread price becomes a support request.
- **Fix:**
  - Below `md`, show showtimes, cart lines and tickets as stacked cards.
  - Keep prices from breaking: `text-nowrap`, plus a non-breaking space before €.
  - Add a sticky bottom bar with the total and the main button.
  - Make seats at least 32 px and scroll sideways instead of shrinking.
  - Add a direct "Покажи билета" button.
  - Drop the 14 px mobile root font.
- **Command:** `/impeccable adapt`

**[P2] The site has no visual identity**

- **What:**
  - Template chrome everywhere, and "Без плакат" as the main visual.
  - One badge colour for three event types, and film words for concerts and plays.
  - A receipt-style confirmation and a data-list ticket.
  - Cards show no city or venue in a three-city product (`Home/Index.cshtml:85-95`).
  - Test genres "d" and "testtt" appear on the public Афиш.
- **Why:** nothing builds anticipation. Users can't scan by image, type or city, and the purchase ends on a receipt.
- **Fix:**
  - When there's no poster, generate one: a colour per event type, the title set large, the date and the venue.
  - Wording driven by event type: прожекция / концерт / представление.
  - Draw ЕКРАН or СЦЕНА as a shape at the top of the seat map.
  - A confirmation written as an invitation, with the QR as the centrepiece.
  - A Cyrillic-first typeface (e.g. Sofia Sans).
- **Command:** `/impeccable bolder`, or `/impeccable shape` for a full redesign

**[P2] Dark mode fails text contrast (measured)**

- **What:**
  - `.text-danger` error text: 3.41:1, on every form including checkout.
  - `btn-outline-secondary`: 3.29:1, so "Печат", "Назад", "Изчисти" and "Детайли" look disabled.
  - `btn-outline-danger`: 3.41:1. `btn-outline-primary`: 3.43:1.
  - Light mode: the review stars are 1.63:1 and have no text equivalent (`Events/Details.cshtml:121`).
  - Dark mode: free seats stay pure white (`site.css:62,70`), the brightest thing on the page.
- **Why:** payment errors and destructive buttons are the text that most needs to be read.
- **Fix:**
  - Point `.text-danger` and the outline buttons' `--bs-btn-color` at Bootstrap's `*-text-emphasis` colours in dark mode.
  - Give ratings a number or an `aria-label`.
  - Dim the free seats in dark mode.
- **Command:** `/impeccable polish`

## Persona Red Flags

These walk the site as three test users.

**Casey (distracted phone user, checkout):**

- The Афиш opens on a full screen of filters; the first event starts about 440 px down.
- "Избери места" is off-screen to the right.
- On the 14-seat map: 20×28 px seats with 4 px gaps.
- Cart: the total and "Премахни" are off-screen, and the concession select is cut to "Редове…".
- Checkout: no timer, no card autofill, and no number keypad for the expiry date.
- At the door: the QR is reachable only through an off-screen table link.
- Interrupted for more than 10 minutes: back to the Афиш to start again.

**Sam (keyboard and screen-reader user):**

- Seat map: focus is destroyed on every click and every 5 s, and there are no arrow keys. The cart timer region is rewritten every second.
- Concession select: submits on change (`Cart.cshtml:52`), so the first arrow key submits it.
- Афиш: five filters have no label (`Home/Index.cshtml:12,15,24,33,42`).
- Navigation: no skip link, no `aria-current`, and the `aria-controls` points at nothing.
- Admin: ten identical "VIP ред" buttons (`Venues/Details.cshtml:30`). Every user row has controls named just "Роля" and "Промени" (`Users/Index.cshtml:46,52`).

**Riley (stress tester):**

- Past showtime: looks bookable, loses the selection on submit, and still looks bookable afterwards.
- Checkout: a validation TypeError, with nothing shown inline.
- Promo: the error wipes the code and says "невалиден или изчерпан" without saying which.
- Admin: "The Събитие field is required." appears on 10 admin fields.
- Register: the page is reachable while logged in.

## Minor Observations

- **Number formatting:** no non-breaking space before €, so "28.00 / €" wraps. Ticket codes break mid-code.
- **Login context:** after a redirect the login page gives no reason. Logout gives no confirmation.
- **Theme toggle:** 🌓 is emoji-only, and there's no way back to "follow the system".
- **Admin dashboard:** "4%" is clipped inside a 13 px bar (`Admin/Home/Index.cshtml:38`). The bars have no `aria-label`. Red marks ≥90% occupancy, so a near sell-out reads as danger.
- **Admin Showtimes:** past and upcoming are mixed, oldest first.
- **Reports:** "Експорт CSV" is a fourth button colour.
- **Delete page:** doesn't mention what else gets deleted (`Shared/Delete.cshtml:7`).
- **Role changes:** no confirmation.
- **Ticket cancellation:** a native `confirm()` that names nothing. The button disappears inside 24 h with no explanation (`Tickets/Details.cshtml:11-17`).
- **Review rules:** one sentence covers both "already reviewed" and "no ticket" (`Events/Details.cshtml:106-109`).
- **Empty search:** no reset and no suggestion (`Home/Index.cshtml:66-69`).
- **Showtimes:** no weekday shown.
- **404 page:** a bare "404" `h1` (`Status.cshtml:12-13`).
- **File input:** shows the browser's English text.

## Questions to Consider

1. What if the seat map were the hero, drawn as the real hall (screen or stage shape, aisles, the event's colour), with best available seats pre-selected?
2. When there's no poster, could the title, type and date *be* the poster, set like a Bulgarian theatre афиш?
3. What if the confirmation said "До скоро в Морска зала Варна, 8 октомври, 12:00", with the QR as the centrepiece?
4. Should the hold read as a promise ("Пазим местата ви") that only turns urgent in the last 2 minutes, and could buyers extend it once?
5. Should the event type drive the wording, colour and map, instead of one film-shaped template for all three?
