using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Integration;

public class RepositoryTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static Venue NewVenue(string name) =>
        new() { Name = name, City = "София", Address = "ул. Тест 1", Rows = 5, SeatsPerRow = 8 };

    private Repository<Venue> Repo() => new(_db.NewContext());

    [Fact]
    public async Task Add_Update_Remove_RoundTrip()
    {
        var repo = Repo();
        var venue = NewVenue("Зала 1");
        await repo.AddAsync(venue);
        await repo.SaveChangesAsync();

        var loaded = await Repo().GetByIdAsync(venue.Id);
        Assert.Equal("Зала 1", loaded!.Name);

        var updateRepo = Repo();
        var toUpdate = (await updateRepo.GetByIdAsync(venue.Id))!;
        toUpdate.Name = "Зала 2";
        updateRepo.Update(toUpdate);
        await updateRepo.SaveChangesAsync();
        Assert.Equal("Зала 2", (await Repo().GetByIdAsync(venue.Id))!.Name);

        var removeRepo = Repo();
        removeRepo.Remove((await removeRepo.GetByIdAsync(venue.Id))!);
        await removeRepo.SaveChangesAsync();
        Assert.Null(await Repo().GetByIdAsync(venue.Id));
    }

    [Fact]
    public async Task ToPagedResultAsync_ReturnsRequestedPageAndTotals()
    {
        var repo = Repo();
        for (var i = 1; i <= 25; i++)
            await repo.AddAsync(NewVenue($"Зала {i:00}"));
        await repo.SaveChangesAsync();

        var page = await Repo().Query().OrderBy(v => v.Name).ToPagedResultAsync(page: 3, pageSize: 10);

        Assert.Equal(5, page.Items.Count);
        Assert.Equal("Зала 21", page.Items[0].Name);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }
}
