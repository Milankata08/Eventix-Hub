using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Unit;

public class OccupancyTests
{
    [Theory]
    [InlineData(10, 8, 1, 1, 90.0, true)]
    [InlineData(10, 8, 0, 2, 80.0, false)]
    [InlineData(0, 0, 0, 0, 0.0, false)]
    public void Percent_And_IsCritical(int capacity, int sold, int held, int free, double percent, bool critical)
    {
        var row = new ShowtimeRowDto(1, 1, "Шоу", "Зала 1", new DateTime(2026, 10, 5, 19, 0, 0), capacity, sold, held);

        Assert.Equal((free, percent, critical), (row.Free, row.Percent, row.IsCritical));
    }
}
