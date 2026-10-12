# EventTix Hub — Implementation Plan

Spec and tickets for **Проект 2: Онлайн платформа за билети за кино и концерти ("EventTix Hub")**, written to be executed with `/mattpocock-skills:implement`.

Sources (all read in full):

- `Проект 2.Онлайн платформа за билети за кино и концерти.pdf` — scope, entities, sprint contents, grading.
- `Обща техническа реализация.pdf` — mandatory stack, CRUD/search/paging/validation/auth conventions, minimum and "excellent" requirements, sprint dates. Its §11 sprint *contents* describe a different project (restaurants); only the **dates** apply here.
- `03.Планиране на проекта.pdf` — planning assignment: a 9-section plan (client, users and roles, functional and non-functional requirements, team roles, architecture and tasks, testing, deployment, maintenance). Delivered as `docs/ProjectPlan.md`; deployment is local only.
- `01-2 Примерен проект - OrgVacationManager/` — teacher's sample (ASP.NET MVC 5 + EF6, .NET Framework). Reference for ideas only (separate data layer, filter + pager view models, seeder). Do **not** copy: plaintext passwords, GET requests that delete data, `new DbContext()` inside controllers, crashes on unknown ids.

---

## 0. How to execute this plan

- Run `/mattpocock-skills:implement` once per sprint block, e.g. *"implement PLAN.md tickets T01–T05"*. Inside a block, follow the "Blocked by" edges.
- Typecheck: `dotnet build EventTixHub.sln`
- One test class: `dotnet test --filter "FullyQualifiedName~BookingRulesTests"`
- Full suite: `dotnet test`
- Migrations: `dotnet ef migrations add <Name> --project EventTixHub.Web` (local tool from T01)
- Run: `dotnet run --project EventTixHub.Web` (Development profile; Swagger at `/swagger`)
- TDD only at the pre-agreed seams in §9. Controllers and views are verified by hand against the documented test scenarios (T17/T18).
- Commit after every ticket (Conventional Commits, English).

## 1. Deadlines and grading

| Sprint | Scope | Assigned | Exam | Points |
|---|---|---|---|---|
| 1 | Data layer, repository, core business logic | 05.10.2026 | 16–20.11.2026 | 25 |
| 2 | Service layer, unit + Moq + integration tests | 01.12.2026 | 25–29.01.2027 | 30 |
| 3 | UI / presentation layer | 03.02.2027 | 01–05.03.2027 | 25 |
| 4 | Documentation, QA, defense | 08.03.2027 | 22–26.03.2027 | 20 |

Mandatory minimum (general doc §13): working DB, 5–6+ related models, CRUD, users and roles, authorization, at least 5 project-specific business rules, search, filtering, paging, validation, error handling, documentation, test scenarios.

"Excellent" extras this plan delivers: dashboard, statistics, charts, REST API, AJAX, sorting, logging, unit tests, integration tests. Optional: file upload, CSV export (see T19/T20).

## 2. Decisions

| Topic | Decision | Why |
|---|---|---|
| Stack | ASP.NET Core MVC + EF Core + SQL Server LocalDB + ASP.NET Core Identity | Mandated by the general doc |
| Target | `net9.0`, EF Core 9.x | SDK 9.0.308 is installed; VS 2022 supports .NET 9. Retarget to `net10.0` later = change TFM + bump packages |
| Projects | `EventTixHub.Web` (teacher's recommended folders) + `EventTixHub.Tests` | Fewest projects; layers separated by folders and the repository seam |
| Repo root | `C:\Users\Kris\Desktop\eventimhub` (current folder); sample folder git-ignored | `/implement` commits to the current repo |
| Language | Code, commits: English. UI text, validation messages, docs: Bulgarian | Bulgarian commission |
| Currency | EUR, displayed `12,50 €` | Bulgaria uses the euro since 01.01.2026 |
| Time | Local `DateTime` everywhere (one timezone). "Now" comes from injected `TimeProvider` | Deterministic tests (hold expiry, deadlines) |
| Culture | Invariant culture for model binding; explicit display formats `dd.MM.yyyy HH:mm`; money displays in bg-BG (`12,50 €`) via `Format.Money` | Avoids comma/dot decimal binding bugs |
| Roles | `Administrator` (organizer / admin panel), `Customer` | Spec treats organizer and admin as one module |
| Payment | Simulated card form, validated then discarded, never stored or logged | No real gateway required |
| Real time | AJAX polling every 5 s | No SignalR client lib needed |
| Charts | Bootstrap progress bars | Zero extra JS library |
| QR | `QRCoder` NuGet, PNG data URI rendered server-side | Works offline |
| Price categories | `SeatCategory` enum (Standard, Vip) on each seat; prices per category on each showtime | Covers "VIP, Стандартни" without an extra entity |
| Showtimes | Separate `Showtime` entity (event + venue + time + prices) | Spec requires "прожекции"; keeps reviews per event |

## 3. Conventions (the "standards" for `/code-review`)

- Nullable enabled, implicit usings, file-scoped namespaces, async EF calls with `Async` suffix.
- Controllers are thin: validate ViewModel → call service → map → view/redirect. Controllers never touch `ApplicationDbContext`.
- Services never touch `HttpContext` or ViewModels. They use `IRepository<T>`, `TimeProvider`, `ILogger`, and `UserManager<User>` (UserService only).
- `BookingRules` is pure: no I/O, no `DateTime.Now`.
- Rule violations throw `BookingException` with a Bulgarian, user-facing message. Missing entities → `null` from reads, 404 in controllers.
- Every state change is a POST. Deletes = GET confirmation page + POST.
- Forms bind to ViewModels only, never to entities (no over-posting).
- Money is `decimal` with `[Precision(10,2)]`; all price math lives in `BookingRules`.
- No service interfaces (one implementation each), no AutoMapper, no MediatR, no extra layers.

## 4. Glossary (code ↔ UI)

| Code | UI (BG) | Meaning |
|---|---|---|
| Event | Събитие | A film, concert or play. `EventType`: Movie, Concert, Theatre |
| Venue | Зала | A hall with a fixed seat grid, in a city, at an address |
| Seat | Място | Row + number in a venue; `SeatCategory` = price zone (Standard / Vip) |
| Showtime | Прожекция / представление | An event in a venue at `StartsAt`, with StandardPrice and VipPrice |
| Hold | Резервация | A `Reserved` ticket that blocks a seat until `ReservedUntil` (10 min) |
| Ticket | Билет | One seat for one showtime: Reserved → Paid → Cancelled. Paid tickets get a unique `Code` shown as QR |
| Order | Поръчка | A user's tickets. `Pending` order = cart (Количка); then Paid or Cancelled |
| Concession | Намаление | Per-ticket discount: Student 20 %, Pensioner 30 % |
| PromoCode | Промо код | Percent discount with a usage limit and validity window |
| Occupancy | Заетост | Sold / held / free seats of a showtime |
| Cancellation deadline | Срок за отказ | 24 h before `StartsAt` |

## 5. Domain model

`BaseEntity { int Id }` — base of every domain entity (generic repository constraint). Validation attributes on entities; ViewModels repeat them with Bulgarian messages.

| Entity | Fields (validation) | Notes |
|---|---|---|
| **User** : `IdentityUser<int>` | FirstName (req, ≤50), LastName (req, ≤50); Email, UserName, PhoneNumber, PasswordHash from Identity | Nav: Orders, Reviews |
| **Role** : `IdentityRole<int>` | Name | `Administrator`, `Customer` (constants in a static `Roles` class) |
| **Event** | Title (req, ≤150), Description (≤2000), Type `EventType`, Genre (req, ≤50), DurationMinutes (1–600), PosterUrl (opt, ≤500, URL) | Nav: Showtimes, Reviews |
| **Venue** | Name (req, ≤100), City (req, ≤50), Address (req, ≤200), Rows (1–30), SeatsPerRow (1–40) | Nav: Seats, Showtimes |
| **Seat** | VenueId, Row (≥1), Number (≥1), Category `SeatCategory` | Unique (VenueId, Row, Number) |
| **Showtime** | EventId, VenueId, StartsAt, StandardPrice, VipPrice (0.01–1000; VipPrice ≥ StandardPrice) | Nav: Tickets |
| **Order** | UserId, CreatedAt, PaidAt?, Status `OrderStatus` **[ConcurrencyCheck]**, PromoCodeId?, TotalAmount | Filtered unique index (UserId) where `[Status] = 0` → one cart per user |
| **Ticket** | OrderId, ShowtimeId, SeatId, Status `TicketStatus` **[ConcurrencyCheck]**, Concession `ConcessionType`, Price, ReservedUntil?, Code? (≤20, unique), CancelledAt? | **Filtered unique index (ShowtimeId, SeatId) where `[Status] <> 2`** — the double-booking lock |
| **Review** | UserId, EventId, Rating (1–5), Comment (≤1000), CreatedAt | Unique (UserId, EventId) |
| **PromoCode** | Code (req, 3–20, `^[A-Z0-9]+$`, stored uppercase, unique), DiscountPercent (1–100), UsageLimit (≥1), TimesUsed **[ConcurrencyCheck]**, ValidFrom, ValidUntil (> ValidFrom), IsActive | Nav: Orders |

Enums (persisted as int; index filters depend on the values — never reorder):
`EventType { Movie, Concert, Theatre }`, `SeatCategory { Standard, Vip }`, `OrderStatus { Pending = 0, Paid = 1, Cancelled = 2 }`, `TicketStatus { Reserved = 0, Paid = 1, Cancelled = 2 }`, `ConcessionType { Regular, Student, Pensioner }`. Bulgarian `[Display(Name = ...)]` on members.

Relationships (for the ERD):

- User 1–N Order, User 1–N Review, User N–M Role (Identity `AspNetUserRoles`)
- Event 1–N Showtime, Event 1–N Review → User N–M Event through Review
- Venue 1–N Seat, Venue 1–N Showtime
- Showtime 1–N Ticket, Seat 1–N Ticket → Showtime N–M Seat through Ticket
- Order 1–N Ticket, PromoCode 1–N Order

Delete behavior:

- Cascade: Venue→Seat, Event→Showtime, Event→Review, User→Review.
- Restrict: Showtime→Ticket, Seat→Ticket, Order→Ticket, Venue→Showtime, User→Order, PromoCode→Order.
- Services check first and return a friendly message; the FK restriction is the backstop. Restrict on Ticket also avoids SQL Server "multiple cascade paths".

## 6. Business rules

| # | Rule |
|---|---|
| BR-01 | **Seat status.** For a showtime, a seat is *Sold* if it has a Paid ticket, *Held* if it has a Reserved ticket with `ReservedUntil > now`, otherwise *Free* (no ticket, Cancelled, or expired hold). Pure function `BookingRules.StatusOf`. |
| BR-02 | **No double booking.** Reserving checks every requested seat is Free and names the taken seat ("Ред 3, място 7 е заето"). The filtered unique index allows at most one non-cancelled ticket per (showtime, seat) even when two requests pass the check at the same moment; the losing insert's `DbUpdateException` becomes a `BookingException`. |
| BR-03 | **Seat hold.** A reservation holds seats for 10 minutes. Expired holds on the requested seats are deleted (own `SaveChanges`) before new holds are inserted. Checkout rejects and removes expired holds. |
| BR-04 | **Sales window.** Seats can be reserved and paid only while `now < StartsAt`. |
| BR-05 | **Cart limits.** Max 10 active tickets per cart; seat ids distinct; seats must belong to the showtime's venue; one Pending order per user. |
| BR-06 | **Zone price.** Base price = VipPrice for Vip seats, StandardPrice otherwise. |
| BR-07 | **Concession.** Per ticket: Student −20 %, Pensioner −30 %, Regular 0 %. UI notes that the ID card is checked at the entrance. |
| BR-08 | **Promo code.** Usable when IsActive, `ValidFrom ≤ now ≤ ValidUntil`, `TimesUsed < UsageLimit`. Applies its percent to every ticket after the concession. Validated when applied to the cart and again at checkout. `TimesUsed` increments once per paid order; `[ConcurrencyCheck]` stops concurrent orders from exceeding the limit. Codes are case-insensitive (normalized to uppercase). |
| BR-09 | **Rounding.** Ticket price = `round(base × (1 − concession%) × (1 − promo%), 2, MidpointRounding.AwayFromZero)`. Order total = sum of ticket prices. Only `BookingRules` computes money. |
| BR-10 | **Checkout.** Order Pending → Paid (+PaidAt, TotalAmount). Each ticket → Paid, `ReservedUntil` cleared, final price stored, unique code generated (`"ETH-" + 10 hex chars` from `RandomNumberGenerator`). A concurrent change (hold deleted, promo exhausted, double submit) raises `DbUpdateConcurrencyException` → `BookingException`, nothing saved. |
| BR-11 | **Cancellation.** Only the owner, only Paid tickets, only while `StartsAt − now ≥ 24 h` (exactly 24 h is allowed). Ticket → Cancelled (+CancelledAt); the seat becomes Free. The order becomes Cancelled when all its tickets are cancelled. Promo usage is not returned. Revenue counts only Paid tickets. |
| BR-12 | **Scheduling.** A new showtime must start in the future and must not overlap another showtime in the same venue (`[StartsAt, StartsAt + DurationMinutes)`; back-to-back is allowed). A showtime's venue cannot change once it has tickets. |
| BR-13 | **Reviews.** Rating 1–5, comment ≤1000, one review per user per event, only users with a Paid ticket for a showtime of that event that has already started. The author or an admin can delete it. |
| BR-14 | **Venue layout.** Creating a venue generates Rows × SeatsPerRow Standard seats. The admin toggles whole rows to/from VIP on the venue seat map. Rows / SeatsPerRow can change only while the venue has no showtimes (seats are regenerated). |
| BR-15 | **Safe deletes.** Refused with a message: event or showtime with tickets, venue with showtimes, promo code with `TimesUsed > 0` (deactivate it instead), user with orders. `UsageLimit` cannot be set below `TimesUsed`. |
| BR-16 | **Admin self-protection.** An admin cannot change their own role or delete themselves. |

Why this is the "concurrency lock": the pre-check gives friendly errors; the filtered unique index is the real lock (the database serializes conflicting inserts, across processes); `[ConcurrencyCheck]` on `Ticket.Status`, `Order.Status` and `PromoCode.TimesUsed` turns every racing UPDATE/DELETE into `WHERE ... AND Status = @original`, so a hold that is being paid cannot be deleted at the same time, and vice versa. No in-process locks.

## 7. Architecture

```
Presentation  Controllers, Areas/Admin, ViewModels, Views, wwwroot/js   (MVC + 3 JSON endpoints)
     ↓
Services      EventService, VenueService, TicketService, OrderService, PromoCodeService,
              ReviewService, UserService, ReportService, BookingRules (pure), DTO records
     ↓
Data          ApplicationDbContext (IdentityDbContext<User, Role, int>), IRepository<T>/Repository<T>,
              DbSeeder, Migrations
     ↓
SQL Server LocalDB
```

Folder layout:

```
eventimhub/                       git repo root (sample folder ignored)
├── PLAN.md, README.md, EventTixHub.sln, .gitignore
├── .config/dotnet-tools.json     dotnet-ef 9.x
├── docs/Documentation.md, docs/UserGuide.md
├── EventTixHub.Web/
│   ├── Program.cs
│   ├── Models/                   entities, enums, Roles constants
│   ├── Data/                     ApplicationDbContext, IRepository, Repository, DbSeeder, Migrations/
│   ├── Services/                 services, BookingRules, BookingException, PagedResult, DTO records
│   ├── ViewModels/
│   ├── Controllers/  Controllers/Api/
│   ├── Areas/Admin/Controllers/  Areas/Admin/Views/
│   ├── Views/
│   └── wwwroot/css/site.css, wwwroot/js/seatmap.js, wwwroot/js/site.js
└── EventTixHub.Tests/
    ├── Unit/                     seams A and B
    ├── Integration/              seam C
    └── TestHelpers/              TestDb (SQLite in-memory), FakeClock
```

Generic repository (shape is a decision; registered open-generic, scoped):

```csharp
public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(); // commits the request's shared DbContext = unit of work
}
```

All repositories in one request share one scoped `DbContext`, so one `SaveChangesAsync` commits every pending change atomically.

Services (concrete classes, scoped):

| Service | Responsibilities | Rules |
|---|---|---|
| EventService | Afisha search (text, type, genre, city, venue, date; sort by date/title/rating; paged), filter option lists, event details with upcoming showtimes + free-seat counts + average rating, admin CRUD for events and showtimes, per-event occupancy summary | BR-12, BR-15 |
| VenueService | Venue CRUD, seat generation, VIP row toggle | BR-14, BR-15 |
| TicketService | Seat map with statuses and counts, cancel, user ticket history (upcoming / past / cancelled, paged), ticket details | BR-01, BR-11 |
| OrderService | Reserve seats, remove from cart, cart with live price preview, set concession, apply / remove promo, checkout, order confirmation | BR-02–10 |
| PromoCodeService | CRUD, search, active filter | BR-08, BR-15 |
| ReviewService | Reviews per event (paged), CanReview, add, delete | BR-13 |
| UserService | Register (Customer role), profile read/update, admin list (search, role filter, paged), set role, delete | BR-15, BR-16 |
| ReportService | Dashboard KPIs; revenue by event; revenue by venue; occupancy per showtime (≥ 90 % flagged "критична") and average occupancy by start hour; date range | — |
| BookingRules (static) | Constants `HoldMinutes = 10`, `CancellationDeadlineHours = 24`, `MaxTicketsPerOrder = 10`, `StudentPercent = 20`, `PensionerPercent = 30`, `CriticalOccupancyPercent = 90`; pure `TicketPrice`, `OrderTotal`, `StatusOf`, `CanCancel`, `IsPromoUsable`, `Overlaps` | BR-01, 06–09, 11, 12 |

- Services return entities for CRUD and small `record` DTOs for projections (afisha card, seat map, cart, report rows). ViewModels wrap DTOs; no second mapping.
- `PagedResult<T>(Items, Page, TotalPages, TotalCount)` + `IQueryable<T>.ToPagedResultAsync(page, pageSize)`; page size 10 (afisha 12).
- Services read now via `TimeProvider.GetLocalNow().DateTime`.

Error handling:

- `BookingException` → controller catches → TempData error alert + redirect, or ModelState error + re-render.
- Unknown id → `NotFound()`.
- Unhandled → `UseExceptionHandler("/Home/Error")` + logging; `UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}")` renders Bulgarian 403/404 pages.
- `ILogger` warning on every booking conflict.

Security:

- Identity password hashing and lockout defaults; cookie `LoginPath = /Account/Login`, `AccessDeniedPath = /Account/AccessDenied`.
- `[Authorize(Roles = Roles.Administrator)]` on every Admin-area controller; `[Authorize]` on booking, profile, tickets and review POSTs; ownership checks inside services.
- Global `AutoValidateAntiforgeryTokenAttribute`; all forms use the form tag helper.
- Card number / CVV never stored or logged.

## 8. Pages and endpoints

Customer side:

| Route | Page | Access |
|---|---|---|
| `/` (Home/Index) | Афиш: search box, filters (type, genre, city, venue, date), sort, paged event cards (poster or placeholder, type badge, genre, rating, next showtime) | anyone |
| `/Events/Details/{id}` | Event info, average rating, upcoming showtimes (date, venue, city, prices, free seats, "Избери места"), reviews (paged) + review form when allowed, delete own review | anyone; review POST logged in |
| `/Booking/Seats/{showtimeId}` | Seat map: legend, live status (poll 5 s), click to select, price preview, "Добави в количката" | logged in |
| `/Booking/Cart` | Held tickets with countdown, concession per ticket, promo code, totals, remove ticket, "Плащане" | logged in |
| `/Booking/Checkout` | Simulated payment form | logged in |
| `/Booking/Confirmation/{orderId}` | Generated tickets: event, venue, date, row/seat, category, concession, price, code, QR, print | owner |
| `/Tickets/Details/{id}`, POST `/Tickets/Cancel/{id}` | Single ticket with QR; cancel (BR-11) | owner |
| `/Profile`, `/Profile/Edit` | Personal data + ticket history (filter, paged, cancel buttons) | logged in |
| `/Account/Register`, `Login`, POST `Logout`, `AccessDenied` | Identity | anyone |

Admin area (`/Admin/...`, Administrator only):

| Route | Page |
|---|---|
| `/Admin` (Dashboard) | KPIs (revenue today / this month, tickets sold this month) + upcoming showtimes with occupancy bars |
| `/Admin/Events` | CRUD, search, type/genre filter, paged; Details lists showtimes with occupancy |
| `/Admin/Showtimes` | CRUD, filters (event, venue, date range), sold/held/free columns; Details = live seat map |
| `/Admin/Venues` | CRUD, search, city filter; Details = seat map with per-row VIP toggle |
| `/Admin/PromoCodes` | CRUD, search, active/expired filter |
| `/Admin/Users` | List, search, role filter, change role, delete |
| `/Admin/Reports` | Revenue by event, revenue by venue, occupancy over time; date range |

JSON API (attribute-routed `[ApiController]`, documented in Swagger):

| Endpoint | Used by |
|---|---|
| `GET /api/events` (same filters as afisha, paged) | Swagger / external clients |
| `GET /api/events/{id}` | Swagger / external clients |
| `GET /api/showtimes/{id}/seats` → seats `{id,row,number,category,status}` + counts + prices | Seat picker and admin monitor (polling) |

## 9. Testing — pre-agreed seams

| Seam | What | How | Graded as |
|---|---|---|---|
| A | `BookingRules` public functions | Plain xUnit, no mocks | Unit tests |
| B | Service public methods | `Mock<IRepository<T>>` (MockQueryable `BuildMock()` for async LINQ) + `FakeClock` | Unit tests + Mocking with Moq |
| C | Service public methods over real `Repository<T>` + `ApplicationDbContext` | SQLite in-memory (test subclass stores `decimal` as REAL), `EnsureCreated`, `FakeClock` | Integration tests (service + repository) |

Nothing else gets automated tests; UI is checked by the manual scenarios in T17/T18. Good tests here assert outcomes through the public interface (returned value, entity state, `BookingException`), take expected values from the spec as literals, and cover one behavior each. Verify `SaveChangesAsync` only to prove a rejected command saved nothing.

Seam A — `BookingRulesTests`:

- A1 Standard seat costs StandardPrice; VIP seat costs VipPrice (Theory).
- A2 Student concession: 12.00 → 9.60.
- A3 Pensioner concession: 12.00 → 8.40.
- A4 Promo after concession: VIP 20.00, Student, promo 10 % → 14.40.
- A5 Midpoint rounds away from zero: 10.15, Pensioner → 7.11.
- A6 Order total: 9.60 + 14.40 → 24.00.
- A7 Seat status (Theory): no ticket → Free; Reserved until future → Held; Reserved expired → Free; Paid → Sold; Cancelled → Free.
- A8 Cancel exactly 24 h before start → allowed.
- A9 Cancel 23 h 59 min before start → rejected.
- A10 Promo usability (Theory): inactive, not started, expired, limit reached → false; valid → true.
- A11 Overlap (Theory): back-to-back → no overlap; start inside another showtime → overlap.

Seam B — Moq (`TicketServiceTests`, `OrderServiceTests`, `EventServiceTests`, `ReviewServiceTests`):

- B1 Reserve rejects a seat that is already sold.
- B2 Reserve rejects a showtime that already started.
- B3 Reserve rejects more than 10 seats in one cart.
- B4 Reserve rejects a seat from another venue.
- B5 ApplyPromo rejects an exhausted code.
- B6 Checkout rejects a cart whose hold expired.
- B7 Cancel rejects someone else's ticket.
- B8 Cancel before the deadline marks the ticket Cancelled and saves.
- B9 Cancel after the deadline is rejected and nothing is saved.
- B10 CreateShowtime rejects an overlap in the same venue.
- B11 AddReview rejects a user without an attended paid ticket.

Seam C — integration (`RepositoryTests`, `BookingFlowTests`):

- C1 Repository CRUD round trip + paging.
- C2 A second user cannot reserve a seat held by the first.
- C3 The database refuses a second non-cancelled ticket for the same showtime seat.
- C4 After the hold expires (fake clock +11 min) another user can reserve the seat.
- C5 Full flow, seat selection → generated ticket: reserve 2 seats → Student concession → promo → checkout → order Paid, tickets Paid with distinct codes, seats Sold, promo `TimesUsed = 1`.
- C6 A cancelled ticket's seat can be bought again.

## 10. Tickets

### Sprint 1 — Data and business layer

**T01 — Solution scaffold.** Blocked by: none.

- `git init` in the repo root; `.gitignore` from `dotnet new gitignore` + `01-2 Примерен проект - OrgVacationManager/`.
- `EventTixHub.sln` with `EventTixHub.Web` (`dotnet new mvc -f net9.0`, no auth option) and `EventTixHub.Tests` (`dotnet new xunit -f net9.0`) referencing Web.
- Web packages (9.x): `Microsoft.EntityFrameworkCore`, `.SqlServer`, `.Tools`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Test packages: `Moq` (≥ 4.20.72), `MockQueryable.Moq`, `Microsoft.EntityFrameworkCore.Sqlite`. Swashbuckle and QRCoder come later with the tickets that need them.
- Local tool manifest with `dotnet-ef` 9.x.
- [ ] `dotnet build` and `dotnet test` pass; `dotnet ef --version` reports 9.x; first commit.

**T02 — Domain model, DbContext, Identity, initial migration.** Blocked by: T01.

- Entities, enums, `BaseEntity`, `Roles` per §5 with validation attributes.
- `ApplicationDbContext : IdentityDbContext<User, Role, int>` with indexes, filters, delete behaviors.
- `Program.cs`: SQL Server (`DefaultConnection` = LocalDB, database `EventTixHub`), `AddIdentity<User, Role>()` + EF stores, cookie paths, `TimeProvider.System`, `Database.Migrate()` on startup.
- Migration `InitialCreate`.
- [ ] Migration has the filtered unique index Tickets (ShowtimeId, SeatId) `[Status] <> 2`, Orders (UserId) `[Status] = 0`, and unique Seats (VenueId, Row, Number), Reviews (UserId, EventId), PromoCodes (Code), Tickets (Code).
- [ ] App starts and creates the database on LocalDB without cascade-path errors.

**T03 — Generic repository, paging, test DB.** Blocked by: T02.

- `IRepository<T>` / `Repository<T>` per §7, open-generic DI registration.
- `PagedResult<T>` + `ToPagedResultAsync`.
- Test helpers: `TestDb` (one open SQLite in-memory connection, `EnsureCreated`, fresh context per call, `decimal` stored as REAL), `FakeClock : TimeProvider` (settable now, UTC local zone).
- [ ] C1 green.

**T04 — BookingRules (TDD, seam A).** Blocked by: T02.

- `BookingRules` constants and pure functions per §7; `BookingException`.
- [ ] A1–A11 green; `BookingRules` has no I/O and no `DateTime.Now`.

**T05 — Seed data.** Blocked by: T02.

- `DbSeeder` after `Migrate()`, only when there are no events. Uses DbContext + UserManager/RoleManager.
- Roles; `admin@eventtix.bg / Admin123!` (Administrator); `user@eventtix.bg / User123!` (Customer) + one more customer.
- 4 venues in София, Пловдив, Варна (6–10 rows × 8–14 seats, VIP rows); 9+ events (3 per type, Bulgarian titles and genres); 2–4 non-overlapping showtimes per event over the next 14 days, relative to now; realistic prices (films 9/14 €, concerts 35/60 €).
- One past showtime where the demo customer has a Paid ticket (enables reviews) + paid tickets on upcoming showtimes (occupancy and report data).
- Promo codes: `WELCOME10` (10 %, limit 100, active), `SUMMER25` (expired), `LIMITED50` (limit 1, already used). A few reviews.
- [ ] Fresh database → seeded data present; a second start does not duplicate; seeded tickets respect the unique indexes.

Sprint 1 exam demo: entities + migration + database diagram in SSMS, generic repository, `BookingRules` + `dotnet test`.

### Sprint 2 — Service layer and tests

**T06 — TicketService: seat map and holds.** Blocked by: T03, T04.

- `GetSeatMapAsync` (BR-01, counts free/held/sold), `ReserveSeatsAsync` (BR-02–05), `RemoveFromCartAsync`.
- [ ] B1–B4, C2, C3, C4 green.

**T07 — OrderService and PromoCodeService.** Blocked by: T06.

- Cart DTO with price preview, `SetConcessionAsync`, `ApplyPromoAsync` / `RemovePromoAsync` (BR-08), `CheckoutAsync` (BR-09, BR-10), `GetUserOrderAsync`.
- PromoCodeService CRUD + search (BR-15).
- [ ] B5, B6, C5 green.

**T08 — TicketService: cancellation and history.** Blocked by: T07.

- `CancelTicketAsync` (BR-11), `GetUserTicketsAsync` (upcoming / past / cancelled, paged), `GetUserTicketAsync`.
- [ ] B7, B8, B9, C6 green.

**T09 — EventService, VenueService, ReviewService.** Blocked by: T03, T04.

- Afisha search / filter / sort / paging, details, admin CRUD for events and showtimes (BR-12, BR-15), venues + seat generation + VIP toggle (BR-14, BR-15), reviews (BR-13).
- [ ] B10, B11 green.

**T10 — UserService and ReportService.** Blocked by: T03.

- UserService per §7 (BR-15, BR-16); ReportService queries.
- [ ] Build green. No tests: thin queries, and SQLite cannot aggregate decimals reliably; checked by hand in T15.

Sprint 2 exam demo: services own all logic; 22 unit tests (A + B, 11 with Moq); 6 integration tests including C5 (seat selection → generated ticket).

### Sprint 3 — UI

**T11 — Layout, account, profile, error handling.** Blocked by: T05, T08, T10.

- `_Layout` (Bootstrap 5 from the template, `lang="bg"`, navbar: Афиш, Количка, Профил, Админ панел for admins, Вход / Регистрация / Изход with user name), `_Alerts` partial (TempData success / error), `_Pager` partial (`‹ Предишна | 1 | 2 | 3 | Следваща ›`, keeps current filters).
- Invariant binding culture and display formats; Bulgarian Error / 403 / 404 pages; exception handler and status-code pages; global antiforgery filter.
- AccountController: Register (FirstName, LastName, Email, Password, ConfirmPassword → Customer, signed in), Login (remember me, return URL), Logout (POST), AccessDenied.
- ProfileController: Index (personal data + ticket history: filter, paging, cancel button when allowed), Edit (FirstName, LastName, PhoneNumber).
- [ ] Register / login / logout work; anonymous `/Profile` redirects to login; validation messages are Bulgarian; an unknown URL shows the Bulgarian 404 page.

**T12 — Afisha and event details.** Blocked by: T09, T11.

- Home/Index and Events/Details per §8, including reviews (list, add, delete own).
- [ ] Every filter narrows results; filters survive paging; review rules show Bulgarian messages.

**T13 — Seat selection, cart, payment, tickets.** Blocked by: T07, T08, T12.

- `GET /api/showtimes/{id}/seats`.
- `wwwroot/js/seatmap.js`: renders the grid from the API; legend (свободно, VIP, избрано, резервирано, продадено); select free seats only; price preview; hidden `seatIds` inputs inside the antiforgery form; polls every 5 s and drops selections that became unavailable with a notice; `monitor` mode (read-only + counts) for admins; seats are `<button>`s with Bulgarian `aria-label`s.
- BookingController: Seats, Reserve (POST → Cart), Cart (countdown to the earliest expiry, concession select, promo apply / remove, totals, remove ticket), Checkout GET/POST (`PaymentVM`: cardholder, `[CreditCard]` number, MM/YY not in the past, CVV 3–4 digits; discarded after validation), Confirmation.
- TicketsController: Details (QR via QRCoder), Cancel (POST).
- [ ] Full purchase works in the browser with seeded data.
- [ ] Two browsers: a seat held in one shows as unavailable in the other within 5 s; reserving a just-taken seat shows a Bulgarian error and refreshes the map.
- [ ] Expired cart shows a message; cancel inside 24 h is hidden in the UI and rejected by the server.

**T14 — Admin panel.** Blocked by: T09, T10, T13.

- Area `Admin` (`[Authorize(Roles = Administrator)]` everywhere, area route before the default route, own `_ViewImports` / `_ViewStart`).
- Dashboard, Events, Showtimes, Venues, PromoCodes, Users per §8. Create/Edit ViewModels use `IValidatableObject` for field dependencies (VipPrice ≥ StandardPrice, ValidUntil > ValidFrom, StartsAt in the future, UsageLimit ≥ TimesUsed). Deletes show BR-15 messages.
- Showtimes/Details: live seat map (monitor mode). Venues/Details: per-row VIP toggle (POST).
- [ ] A Customer gets the 403 page on `/Admin`.
- [ ] Admin creates venue → event → showtime and sees it on the afisha; an overlapping showtime is rejected with a message; occupancy updates live after a purchase in another browser.

**T15 — Sales reports.** Blocked by: T14.

- `/Admin/Reports`: date range (default current month); tables + progress bars for revenue by event, revenue by venue, occupancy per showtime ("критична" badge ≥ 90 %), average occupancy by start hour.
- [ ] Numbers match the seeded paid tickets (cancelled excluded); an empty range shows "Няма данни".

### Sprint 4 — Documentation, QA, defense

**T16 — REST API and Swagger.** Blocked by: T13.

- `GET /api/events`, `GET /api/events/{id}`; `Swashbuckle.AspNetCore`; XML doc comments on API actions and DTOs (`GenerateDocumentationFile` + `NoWarn 1591`); Swagger UI at `/swagger` in Development.
- [ ] `/swagger` lists exactly the 3 API endpoints with descriptions and response schemas; MVC controllers do not appear.

**T17 — Documentation (Bulgarian).** Blocked by: T15, T16.

- `docs/Documentation.md` with the 10 required sections: 1 описание на системата; 2 функционални изисквания (user stories); 3 роли и права (matrix); 4 структура на базата данни (tables, keys, indexes, constraints); 5 ER диаграма (Mermaid `erDiagram`); 6 архитектура (layer diagram, UML class diagram, booking sequence diagram — Mermaid); 7 основни страници; 8 бизнес правила (BR-01–BR-16); 9 тестови сценарии (manual table: valid input, invalid input, unauthorized access, delete, edit, search, filter, paging, every BR — plus the automated tests mapped to BRs); 10 инструкции за стартиране.
- `docs/UserGuide.md`: customer flow (register → find event → seats → cart → pay → ticket → cancel → review) and admin flow (venue → event → showtime → promo → monitoring → reports).
- `README.md`: purpose, prerequisites (.NET 9 SDK, SQL Server LocalDB), run, demo accounts, tests, Swagger URL, links to docs.
- [ ] All 10 sections present; Mermaid renders in VS Code / GitHub; README steps work on a fresh clone (database created and seeded automatically).

**T18 — Regression QA.** Blocked by: T17.

- Full `dotnet test`; drop the database, start fresh, walk every manual scenario from Documentation §9 in the browser as anonymous, Customer and Administrator; fix defects; `/code-review`; tag `v1.0`.
- [ ] All automated tests green; all manual scenarios pass; no unhandled-exception page during the walkthrough.

### Optional extras (any time after T14)

- **T19 — Poster upload** ("качване на файлове"): `IFormFile` → `wwwroot/uploads`, jpg/png ≤ 2 MB, random file name; replaces PosterUrl input.
- **T20 — CSV export of reports** ("експорт"): UTF-8 with BOM so Excel shows Cyrillic.
- **T21 — Dark mode toggle**: navbar button switches Bootstrap 5.3 color modes (`data-bs-theme` on `<html>`); choice kept in `localStorage`, default follows the OS theme; tickets always print light.
- **T22 — Redesign** (`.scratch/redesign/`): афиш-style visual system (Sofia Sans self-hosted, type inks, poster wall, hall plan, hold badge, invitation ticket, admin programme office); verified against the critique; `DESIGN.md` re-synced from the build.

## 11. Pitfalls

1. Global `dotnet-ef` is 8.0.27 — use the local 9.x manifest. EF Core 9 `Migrate()` throws on pending model changes: add a migration after every model change.
2. Seed with runtime code (`DbSeeder`), not `HasData` — EF 9 rejects non-deterministic seed values such as `DateTime.Now`.
3. SQL Server "multiple cascade paths" — keep every FK into Ticket on Restrict.
4. `HasFilter("[Status] <> 2")` / `"[Status] = 0"` works on SQL Server and SQLite. Enum int values are part of the schema.
5. Unique index on nullable `Ticket.Code`: EF adds `IS NOT NULL` automatically for SQL Server; SQLite allows multiple NULLs.
6. Decimals: `[Precision(10,2)]`. SQLite cannot aggregate or order decimals, so `TestDb` maps every `decimal` to REAL (test-only); production queries stay on `decimal`. Stored two-place prices round-trip exactly, but computed sums may not — keep money math in C#; don't run report SQL in SQLite tests.
7. Culture: `<input type="number" step="0.01">` and `datetime-local` post invariant formats; bind with invariant culture; set Bulgarian `ErrorMessage` on every attribute (defaults are English).
8. Moq + async EF: `list.BuildMock()` from MockQueryable. `Include` is a no-op on mocks — populate navigations in test data. Don't use `ExecuteUpdate`/`ExecuteDelete` in services.
9. Unit of work: `SaveChangesAsync` on any repository saves everything tracked in the request. After a failed save, stop using the context and return the error.
10. Delete expired holds with their own `SaveChanges` before inserting new holds (the filtered unique index would otherwise see both rows).
11. Areas need their own `_ViewImports.cshtml` + `_ViewStart.cshtml`, and the area route must be mapped before the default route.
12. Swagger lists only attribute-routed `[ApiController]` actions (conventional MVC routes are excluded — good). `GenerateDocumentationFile` needs `NoWarn 1591`.
13. Identity with int keys: `IdentityDbContext<User, Role, int>`, `AddIdentity<User, Role>()`; current user id = `int.Parse(userManager.GetUserId(User)!)`.
14. If the test project can't see ASP.NET Core types, add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to it.
15. Seed passwords must satisfy Identity's default policy (upper, lower, digit, symbol, 6+).

## 12. Out of scope (add when asked)

- Real payment gateway — simulated form; add Stripe test mode if required.
- Email confirmations — add SMTP if the teacher wants "изпращане на имейли".
- SignalR push — 5 s polling is enough.
- Separate Organizer role / event ownership.
- Ticket check-in scanning at the door.
- Soft delete and audit log — deletes are restricted instead.
- Docker and automatic deployment.
- `I*Service` interfaces — add if the teacher insists; mechanical.
- Localization framework — Bulgarian strings are hard-coded.
- Per-seat VIP editing — rows are toggled as a whole.
- Refund tracking — revenue = Paid, non-cancelled tickets.

## 13. Requirement coverage

| Requirement (source) | Where |
|---|---|
| Register, login, profile, ticket history | T08, T11 |
| Afisha with date / genre / city / venue filters + search | T09, T12 |
| Interactive seat selection by rows and seats | T06, T13 |
| Cart, student / pensioner / promo discounts, unique code + QR | T04, T07, T13 |
| Cancellation up to N hours before the event | BR-11, T04, T08 |
| Reviews and ratings | BR-13, T09, T12 |
| Admin CRUD: events, showtimes, VIP / Standard price categories, venues | T09, T14 |
| Real-time occupancy monitoring | T06, T13, T14 |
| Promo codes with usage limit | BR-08, T07, T14 |
| Sales reports: by event, by venue, time-critical occupancy | T10, T15 |
| Entities User, Role, Event, Venue, Seat, Ticket, Order, Review, PromoCode | T02 |
| Double-booking concurrency lock | BR-02, BR-03, T02, T04, T06 |
| Price by seat zone + promo code | BR-06–09, T04, T07 |
| Generic repository without repeated code (7 pts) | T03 |
| Seat-status algorithm + real-time duplicate blocking (5 pts) | BR-01, BR-02, T02, T04, T06 |
| EventService, TicketService, OrderService, UserService | T06–T10 |
| 10–15 unit tests, Moq, integration tests | §9, T04, T06–T09 |
| Admin panel and role management | T11, T14 |
| UX: navigation and error handling | T11 |
| Swagger, UML, ERD, user guide | T16, T17 |
| Search, filtering, paging, validation, error handling (general minimum) | T09, T11–T15 |
| At least 5 business rules | §6 (16 rules) |
| Regression QA | T18 |
