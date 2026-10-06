using EventTixHub.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Tests.TestHelpers;

public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestDb()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        using var db = new ApplicationDbContext(_options);
        db.Database.EnsureCreated();
    }

    public ApplicationDbContext NewContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
