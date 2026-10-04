# EventTix Hub

Онлайн платформа за билети за кино, концерти и театър: афиш, схема на залата на живо, резервация за 10 минути, симулирано плащане, билети с QR код, админ панел, справки и REST API със Swagger. Защитена срещу двойна продажба на място чрез филтриран уникален индекс в базата.

Стек: ASP.NET Core MVC (.NET 9), EF Core 9, SQL Server LocalDB, ASP.NET Core Identity, Bootstrap 5.

## Изисквания

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server LocalDB (идва с Visual Studio 2022 или SQL Server Express LocalDB)

## Стартиране

```bash
git clone <url-на-хранилището>
cd eventixhub
dotnet run --project EventTixHub.Web
```

Отворете http://localhost:5226. Базата `EventTixHub` се създава автоматично на `(localdb)\MSSQLLocalDB`, миграциите се прилагат и в режим Development се добавят демо данни (повторно стартиране не ги дублира). Низът за връзка е в `EventTixHub.Web/appsettings.json`.

## Демо акаунти

| Роля | Имейл | Парола |
|---|---|---|
| Администратор | `admin@eventtix.bg` | `Admin123!` |
| Клиент | `user@eventtix.bg` | `User123!` |
| Клиент | `maria@eventtix.bg` | `User123!` |

Промо кодове: `WELCOME10`. Тестова карта за плащане: `4111 1111 1111 1111`, `12/30`, `123`.

## Тестове

```bash
dotnet test
```

Модулни тестове (xUnit, Moq) и интеграционни тестове (SQLite in-memory) — общо 68.

## Swagger

http://localhost:5226/swagger (само в Development): `GET /api/events`, `GET /api/events/{id}`, `GET /api/showtimes/{id}/seats`.

## Документация

- [Документация](docs/Documentation.md) — описание, изисквания, роли, база данни, ER и UML диаграми, бизнес правила, тестови сценарии, стартиране.
- [Ръководство на потребителя](docs/UserGuide.md) — стъпки за клиент и администратор.
- [PLAN.md](PLAN.md) — спецификация и тикети.
