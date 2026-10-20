# EventTix Hub — Първи спринт (Git, Trello, Data & Business Layer)

Практическо задание 06: първи спринт на проекта „Онлайн платформа за билети за кино и концерти“ (EventTix Hub) — Repository, Business и Data Layer.

Екип: Кристиян (Team Lead, Back-end, UI/UX), Радостин (Front-end), Преслав (QA).
Изпит на спринта: 16–20.11.2026.

## 1. Подготвителна фаза: управление на проекта и среда за разработка

### Задача 1: Git хранилище

| Изискване | Изпълнение |
|---|---|
| Хранилище в GitHub | https://github.com/Milankata08/Eventix-Hub (име на проекта: EventTix Hub; решение `EventTixHub.sln`) |
| `.gitignore` за избрания език | стандартният `.gitignore` за .NET / Visual Studio (`dotnet new gitignore`): изключва `bin/`, `obj/`, `.vs/`, `*.user`, локални бази и др. |
| Сътрудници с права за писане | `Milankata08` (собственик, admin), `presmat` (write), `RADKAPIDKA4` (write) |
| Клониране и начален commit | `git clone https://github.com/Milankata08/Eventix-Hub.git` → промяна → `git commit` → `git push` от всеки член на екипа |

**Работен процес с Git**

- Основният клон е `master`. Всяка задача се прави в отделен клон и влиза в `master` чрез pull request с преглед на кода.
- Съобщенията на commit-ите следват Conventional Commits: `feat:`, `fix:`, `refactor:`, `docs:`, `style:`, `test:`.
- Към момента в хранилището има над 80 commit-а и pull request-и #1–#18.

### Задача 2: Kanban борд в Trello

Борд: **EventTix Hub - Sprint 1**

| Колона | Предназначение |
|---|---|
| Backlog / To Do | предстоящи задачи за Спринт 1 |
| In Progress | задачи в процес на разработка |
| Testing / Code Review | задачи за тестване и преглед на кода (отворен pull request) |
| Done | завършени задачи (слети в `master`, тестовете минават) |

**Картички за Спринт 1**

| № | Картичка | Отговорник | Критерий за приемане | Колона |
|---|---|---|---|---|
| T01 | Създаване на решението: `EventTixHub.Web` (ASP.NET Core MVC, .NET 9) и `EventTixHub.Tests` (xUnit), NuGet пакети, `.gitignore` | Кристиян | `dotnet build` и `dotnet test` минават | Done |
| T02a | Създаване на User и Role Entities (ASP.NET Core Identity, роли `Administrator` и `Customer`) | Кристиян | потребителите и ролите са в базата | Done |
| T02b | Entities Event, Venue, Seat, Showtime | Радостин | валидационни атрибути и връзки по ERD | Done |
| T02c | Entities Order, Ticket, Review, PromoCode | Кристиян | валидационни атрибути и връзки по ERD | Done |
| T02d | `ApplicationDbContext`, индекси, поведение при изтриване, миграция `InitialCreate` | Кристиян | базата се създава на LocalDB; уникалните индекси са в миграцията | Done |
| T02e | ERD диаграма на базата (dbdiagram.io / SSMS) | Радостин | диаграмата съвпада с миграцията | Done |
| T03a | Имплементиране на `IRepository<T>` и `Repository<T>`, регистрация в DI | Кристиян | един родов клас за всички същности | Done |
| T03b | Странициране `PagedResult<T>` и тестова база (SQLite in-memory, фалшив часовник) | Преслав | интеграционните тестове на хранилището минават | Done |
| T04a | Разработка на `BookingRules`: цена по зона, намаления, промо код, закръгляне | Кристиян | цените съвпадат с примерите в тестовете | Done |
| T04b | `BookingRules`: статус на място, срок за отказ, годност на промо код, застъпване на прожекции | Кристиян | правилата нямат достъп до базата и до системния часовник | Done |
| T04c | Модулни тестове на `BookingRules` | Преслав | всички тестове минават с `dotnet test` | Done |
| T05 | Демо данни (`DbSeeder`): 3 потребителя, 4 зали, 9 събития, прожекции, промо кодове, билети, ревюта | Радостин | повторно стартиране не дублира данните | Done |

## 2. Практическа фаза: кодова реализация на Първи спринт

Технологии: C# и .NET 9, ASP.NET Core MVC, ASP.NET Core Identity, Entity Framework Core 9 (Code First), SQL Server LocalDB, xUnit, Moq.

### Задача 1: Модели на данните (Entities)

Класовете са в `EventTixHub.Web/Models`. Всички наследяват `BaseEntity` с първичен ключ `int Id`; потребителят и ролята наследяват `IdentityUser<int>` и `IdentityRole<int>`. Свойствата са капсулирани (`get; set;`), със строги типове (`decimal` за цени, `DateTime` за дати, изброими типове за статуси) и валидационни атрибути (`[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]`). Всеки чужд ключ е свойство `…Id` плюс навигационно свойство.

| Същност от заданието | Същност в EventTix Hub | Файл | Ключове |
|---|---|---|---|
| User | `User` (име, фамилия, имейл, хеш на паролата) | `User.cs` | PK `Id`; 1:N към `Order` и `Review` |
| Role | `Role` (`Administrator`, `Customer`) | `User.cs`, `Roles.cs` | PK `Id`; N:M с `User` чрез `AspNetUserRoles` |
| Workout | `Event` (филм, концерт, театър) | `Event.cs` | PK `Id`; 1:N към `Showtime` и `Review` |
| Exercise | `Venue` и `Seat` (зала и място) | `Venue.cs` | PK `Id`; FK `Seat.VenueId` |
| WorkoutExercise (N:M) | `Showtime` — събитие в зала в даден час | `Showtime.cs` | FK `EventId`, FK `VenueId` |
| Subscription | `Order` (количка / платена / отказана поръчка) | `Order.cs` | FK `UserId`, FK `PromoCodeId` |
| NutritionLog / MealPlan | `Ticket` — билет за място на прожекция | `Order.cs` | FK `OrderId`, `ShowtimeId`, `SeatId` |
| Review | `Review` (оценка 1–5, коментар) | `Review.cs` | FK `UserId`, FK `EventId` |
| PromoCode | `PromoCode` (процент, лимит, срок на валидност) | `PromoCode.cs` | PK `Id`; уникален `Code` |

Изброими типове (`Enums.cs`): `EventType`, `SeatCategory`, `OrderStatus`, `TicketStatus`, `ConcessionType`.

Пример:

```csharp
public class Showtime : BaseEntity, IValidatableObject
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public DateTime StartsAt { get; set; }

    [Range(typeof(decimal), "0.01", "1000"), Precision(10, 2)]
    public decimal StandardPrice { get; set; }

    [Range(typeof(decimal), "0.01", "1000"), Precision(10, 2)]
    public decimal VipPrice { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

Контекстът `ApplicationDbContext : IdentityDbContext<User, Role, int>` (`Data/ApplicationDbContext.cs`) описва уникалните индекси и поведението при изтриване; миграцията `InitialCreate` създава базата. Подробната схема е в `docs/Design.md` (ERD).

### Задача 2: Repository Layer и CRUD операции

Родов интерфейс `IRepository<T>` (`Data/IRepository.cs`) и една реализация `Repository<T>` (`Data/Repository.cs`) за всички същности — без повтарящ се код.

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

| Метод от заданието | Метод в EventTix Hub | Бележка |
|---|---|---|
| `GetAll()` | `Query()` | връща `IQueryable<T>`, така че филтрирането, сортирането и страницирането стават в базата, а не в паметта |
| `GetById(id)` | `GetByIdAsync(id)` | асинхронен |
| `Add(entity)` | `AddAsync(entity)` | асинхронен |
| `Update(entity)` | `Update(entity)` | |
| `Delete(id)` | `Remove(entity)` | услугата първо зарежда записа и проверява дали изтриването е позволено |
| — | `SaveChangesAsync()` | един запис в края на операцията — всичко или нищо |

Регистрация в `Program.cs` за всички типове наведнъж:

```csharp
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

Странициране: разширението `ToPagedResultAsync(page, pageSize)` връща `PagedResult<T>` (записи, страница, общ брой). Интеграционният тест `RepositoryTests` проверява добавяне, промяна, изтриване и странициране върху SQLite in-memory.

### Задача 3: Бизнес логика (Business Logic Layer)

Бизнес логиката е в `EventTixHub.Web/Services`. Чистите правила са в статичния клас `BookingRules` — без достъп до базата и без системния часовник (текущото време се подава като параметър), затова се тестват директно.

#### 1. Калкулатор на цена на билет (аналог на BMR / TDEE калкулатора)

`BookingRules.TicketPrice` изчислява крайната цена на билет:

- **Базова цена по зона:** VIP място → VIP цената на прожекцията; всяко друго място → стандартната цена.
- **Модификатор спрямо вида билет:** Студент −20 %, Пенсионер −30 %, Редовен 0 %.
- **Промо код:** процентът се прилага след намалението.
- **Формула:** цена = базова цена × (1 − намаление) × (1 − промо), закръглена до стотинка (половината нагоре).
- Сумата на поръчката е сборът от цените на билетите (`OrderTotal`).

```csharp
public static decimal TicketPrice(decimal standardPrice, decimal vipPrice, SeatCategory category,
    ConcessionType concession, int promoPercent)
{
    var basePrice = category == SeatCategory.Vip ? vipPrice : standardPrice;
    var price = basePrice * (100 - ConcessionPercent(concession)) / 100m * (100 - promoPercent) / 100m;
    return Math.Round(price, 2, MidpointRounding.AwayFromZero);
}
```

| Пример (от тестовете) | Резултат |
|---|---|
| стандартно място 12,00 €, студент | 9,60 € |
| стандартно място 12,00 €, пенсионер | 8,40 € |
| VIP място 20,00 €, студент, промо 10 % | 14,40 € |
| 10,15 €, пенсионер (закръгляне) | 7,11 € |
| поръчка 9,60 € + 14,40 € | 24,00 € |

#### 2. Проверки за достъп (аналог на `CanUserAccessWorkout`)

| Проверка | Къде | Правило |
|---|---|---|
| Може ли потребителят да пише ревю | `ReviewService.CanReviewAsync(userId, eventId)` | само с платен билет за вече започнала прожекция на събитието и само едно ревю за събитие |
| Може ли билетът да се откаже | `BookingRules.CanCancel(startsAt, now)` | най-късно 24 часа преди началото (точно 24 часа е позволено) |
| Свободно ли е мястото | `BookingRules.StatusOf(status, reservedUntil, now)` | продадено при платен билет, резервирано при неизтекла резервация, иначе свободно |
| Годен ли е промо кодът | `BookingRules.IsPromoUsable(promo, now)` | активен, в срока на валидност и под лимита на използване |
| Достъп до админ панела | `[Authorize(Roles = Roles.Administrator)]` | само за роля `Administrator`; клиентът получава 403 |
| Достъп до чужди билети и поръчки | `TicketService`, `OrderService` | заявката винаги филтрира по `userId`; чужд билет дава 404 |

```csharp
public static bool CanCancel(DateTime startsAt, DateTime now) =>
    startsAt - now >= TimeSpan.FromHours(CancellationDeadlineHours);

public static PromoState StateOf(PromoCode p, DateTime now) =>
    !p.IsActive ? PromoState.Inactive
    : now > p.ValidUntil ? PromoState.Expired
    : now < p.ValidFrom ? PromoState.NotStarted
    : p.TimesUsed >= p.UsageLimit ? PromoState.Exhausted
    : PromoState.Usable;
```

#### 3. Валидация на данни

| Данни | Проверка |
|---|---|
| Положителни стойности | `[Range]`: продължителност 1–600 мин, редове 1–30, места на ред 1–40, цени 0,01–1000 €, оценка 1–5, процент на промо код 1–100, лимит на използване ≥ 1 |
| Имейл адрес | `[EmailAddress]` при регистрация и вход; уникален имейл (ASP.NET Core Identity) |
| Телефон | `[Phone]`, до 20 знака |
| Формат на промо код | `[RegularExpression("^[A-Z0-9]+$")]`, 3–20 знака; въведеният код се приема без значение от главни и малки букви |
| Зависими полета | VIP цената ≥ стандартната (`Showtime.Validate`); краят на валидността е след началото (`PromoCode.Validate`) |
| Бизнес грешки | услугите хвърлят `BookingException` със съобщение на български („Ред 3, място 7 е заето.“); контролерът го показва във формата |

## 3. Тестове на спринта

Тестовете са в `EventTixHub.Tests` и се изпълняват с `dotnet test`.

| Тестов клас | Какво проверява |
|---|---|
| `Unit/BookingRulesTests` | цена по зона, студент и пенсионер, промо след намаление, закръгляне, сума на поръчка, статус на място, срок за отказ (точно 24 ч и 1 минута след срока), годност на промо код, застъпване на прожекции, критична заетост |
| `Integration/RepositoryTests` | добавяне, промяна и изтриване през `Repository<T>`; странициране |
| `Integration/DbSeederTests` | демо данните се зареждат и повторно стартиране не ги дублира |

## 4. Демонстрация на изпита

1. Entities в `Models/` и миграцията `InitialCreate`; ERD диаграмата (`docs/img/erd.png`) и базата в SQL Server Management Studio.
2. `IRepository<T>` и `Repository<T>`, регистрацията в `Program.cs`.
3. `BookingRules` и пускане на `dotnet test`.
4. Борда в Trello и историята на commit-ите и pull request-ите в GitHub.
