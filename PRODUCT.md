# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Audience member** (Посетител anonymous, Клиент once logged in). A Bulgarian-speaking adult choosing an evening out: a film, a concert or a play in София, Пловдив or Варна. They browse the Афиш without an account. They log in to pick 1–10 exact seats, pay, and show the QR code at the door. They are often on a phone, sometimes deciding for the same evening, and they also print tickets.
- **Administrator** (Администратор). Staff of the operator. They manage venues and VIP rows, events and posters, showtimes, promo codes, and users and roles. They watch live occupancy and read sales reports, usually on a laptop or desktop.
- **Evaluators.** The teacher and the exam commission grade the project at the sprint exams and the final defense. They walk the customer flow and the admin flow on the demo data.

## Product Purpose

EventTix Hub sells seats for films, concerts and plays in one operator's halls. It replaces box-office-only sales and seats tracked by hand. Buyers choose an exact seat on a live hall plan. The seat is held for 10 minutes while they pay, and it can never be sold twice. Every ticket has a unique code and a QR code. Administrators get occupancy and revenue reports.

Success has two sides. A buyer gets from the Афиш to a ticket with a QR code quickly, never unsure about the seat, the price, the time left or the next step. This must hold on a phone, with a keyboard and with a screen reader. Administrators can run the programme and see occupancy as it happens.

## Positioning

- One Афиш for three art forms (кино, концерти, театър) across four halls in three cities.
- A live hall plan. Seat status refreshes every 5 seconds, and conflicts name the seat ("Ред 3, място 7 е заето."). A hold lasts 10 minutes, and the database refuses a double sale even under concurrent requests.
- Bulgarian domain language throughout: Афиш, прожекция / концерт / представление, and student and pensioner cards checked at the entrance.

## Operating Context

- **Client:** „Сцена и Екран“ ООД, a fictional company made up for the school project (see `docs/ProjectPlan.md` §1). It runs four halls with rectangular seat grids (rows × seats; VIP is set per whole row):
  - Зала 1 – Арена Сердика, София (10 × 14)
  - Зала 2 – Камерна, София (6 × 8)
  - Голяма зала Пловдив (8 × 12)
  - Морска зала Варна (7 × 10)
- **School project:** year-long project at МГ „Акад. Кирил Попов“, graded per sprint. The UI sprint exam is 01–05.03.2027 and the defense is 22–26.03.2027 (`PLAN.md` §1).
- **Runs locally only:** Kestrel at http://localhost:5226 with SQL Server LocalDB and seeded demo data. Demo accounts and the test card are listed in `README.md`.
- **Payment is simulated.** Card data is validated and then discarded, never stored or logged.
- **Prices are in euro.** Tickets are shown on a phone at the door or printed. A printed ticket always uses the light theme.

## Capabilities and Constraints

- **Features:** Афиш with search, filters (type, genre, city, venue, date), sorting and paging; event details with upcoming showtimes, ratings and reviews; live seat map (polling every 5 s); cart with concessions and promo codes; simulated checkout; tickets with QR codes; cancellation; profile with ticket history; admin area (dashboard, events with poster upload, showtimes, venues, promo codes, users, reports with CSV export); REST API with Swagger.
- **Business rules (`PLAN.md` §6):**
  - A hold lasts 10 minutes.
  - At most 10 tickets per order.
  - Sales close when the showtime starts.
  - Cancellation is allowed until 24 h before the start.
  - Concessions: student −20 %, pensioner −30 %. The card is checked at the entrance.
  - Promo codes have a usage limit and a validity window.
  - Each showtime has a Standard price and a VIP price.
- **Stack:** ASP.NET Core MVC (.NET 9) with Razor views, Bootstrap 5.3 (kept; restyled through its CSS variables and `site.css`), jQuery and jquery-validation, and plain JavaScript (`site.js`, `seatmap.js`). There is no SPA framework.
- **Self-hosted assets only:** no external CDN, fonts included. Everything is served from `wwwroot`, because the app runs locally.
- **Language:** UI text and messages are Bulgarian (`lang="bg"`); code and commits are English.
- **Display formats:**
  - Prices are shown as `12,50 €`: Bulgarian decimal comma and a non-breaking space before €. This is display only; model binding stays invariant. Decided 2026-10-06; it supersedes the `12.50 €` in `PLAN.md` §2.
  - Dates in data views stay `dd.MM.yyyy HH:mm`.
- **Showtime wording follows the event type** on public pages: прожекция (film), концерт (concert), представление (theatre).
- **Themes:** light and dark (Bootstrap color modes). The default follows the OS; the toggle choice is remembered; print forces light.
- **Out of scope until asked:**
  - hold extension
  - best-available seat pick
  - marking your own holds on the map
  - door check-in scanning
  - emails
  - a real payment gateway
  - a separate organizer role
- **Open decision:** the admin label for showtimes. Default: keep the spec's umbrella term "Прожекции".

## Brand Commitments

- **Name:** EventTix Hub.
- **Voice:** plain, warm and precise Bulgarian. Errors name the seat or field. The site is honest about the simulation ("Симулирано плащане: данните се проверяват и не се съхраняват.").
- **Demo disclosure:** the public footer states that this is a school project and that payment is simulated, e.g. „Учебен проект · плащането е симулирано“. Checkout keeps its simulated-payment line. Decided 2026-10-06.
- **Binding visual constraints from the user (2026-10-06).** Details are in `DESIGN.md`.
  - "афиш editorial" tone: typographic posters set like Bulgarian theatre афиши
  - a Cyrillic-first display typeface
  - one colour per event type
  - a generated poster when an event has no image
  - keep Bootstrap 5
  - self-host every font

## Evidence on Hand

- **Seed data** (`EventTixHub.Web/Data/DbSeeder.cs`): 9 events (3 per type) with Bulgarian titles, genres and descriptions; 4 venues; showtimes over the next ~14 days; 3 promo codes (`WELCOME10` active, `SUMMER25` expired, `LIMITED50` used up); reviews; 3 demo users.
- **No event has a poster image.** `wwwroot/uploads` does not exist yet. Admins can upload jpg/png up to 2 MB.
  - Some seed titles name real artists ("Концерт на Васил Найденов", "Рок нощ: Остава").
  - Generated posters stay typographic. They never imitate real artwork, artist logos or likenesses.
- **Stray test data in the local DB only:** two events made in admin (`kur`, genre `d`; `test`, genre `testtt`, with showtimes in 2030 and 2052). They are not product content. Remove them in admin, or reset the database before demos.
- **UX critique of 2026-10-06** (score 23/40, with file:line evidence): `.impeccable/critique/2026-10-06T12-56-29Z__eventtixhub-web-views.md`.
- **Absent, and not to be invented:** testimonials, customers, press, sales figures, "sold out" or "popular" claims. Ratings, free-seat counts and prices always come from real data.

## Product Principles

1. **The event and the hall are the hero.** The site shows what you'll see and where you'll sit; chrome stays out of the way.
2. **Never leave a buyer guessing.** The hold, the seat, the price and the next step are always visible and stated in plain Bulgarian.
3. **Speak the art form's language.** Wording, colour and the shape of the hall follow the event type.
4. **Report live state honestly.** Conflicts and expiry are shown as they happen, the seat is named, and the way back is offered.
5. **Works for everyone.** Keyboard, screen reader, a 375 px phone at the door, print, light and dark.

## Accessibility & Inclusion

- **Target:** WCAG 2.2 AA in both themes. Text contrast is at least 4.5:1 (3:1 for large text and UI boundaries). Touch targets are at least 24 × 24 px, and seats at least 32 px.
- **Seat selection** must be fully usable by keyboard and screen reader. `docs/ProjectPlan.md` §4 already promises this.
- **Colour is never the only signal:**
  - posters and badges always carry the type word
  - seat states differ by fill, pattern and outline weight, not hue alone
- **Error messages** are in Bulgarian, linked to their fields (`aria-describedby`), and focus moves to the first error.
- **Motion** respects `prefers-reduced-motion`. Live regions announce changes, never every tick.
