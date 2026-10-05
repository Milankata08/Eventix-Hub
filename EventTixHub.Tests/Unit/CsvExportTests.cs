using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;

namespace EventTixHub.Tests.Unit;

public class CsvExportTests
{
    private static ReportsVM Vm(params RevenueRowDto[] byEvent) =>
        new(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), byEvent, [],
            [new ShowtimeRowDto(1, 1, "Шоу", "Зала 1", new DateTime(2026, 10, 5, 19, 0, 0), 100, 25, 0)], []);

    [Fact]
    public void Build_WritesSectionsWithInvariantNumbersAndSemicolons()
    {
        var csv = CsvExport.Build(Vm(new RevenueRowDto(1, "Концерт", 3, 37.5m)));
        Assert.Contains("Приход по събития", csv);
        Assert.Contains("Концерт;3;37.50", csv);
        Assert.Contains("Шоу;Зала 1;05.10.2026 19:00;100;25;25.0", csv);
    }

    [Fact]
    public void Build_QuotesSpecialCharsAndNeutralisesFormulas()
    {
        var csv = CsvExport.Build(Vm(new RevenueRowDto(1, "A;\"B\"", 1, 1m), new RevenueRowDto(2, "=SUM(A1)", 1, 1m)));
        Assert.Contains("\"A;\"\"B\"\"\";1;1.00", csv);
        Assert.Contains("'=SUM(A1);1;1.00", csv);
    }
}
