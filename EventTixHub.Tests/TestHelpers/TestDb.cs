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
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public ApplicationDbContext NewContext() => new SqliteDbContext(_options);

    public void Dispose() => _connection.Dispose();

    // SQLite cannot aggregate or order decimal columns; SQL Server can. Storing money as REAL here
    // keeps production queries on decimal. Two-place prices round-trip exactly.
    private sealed class SqliteDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
        }
    }
}
