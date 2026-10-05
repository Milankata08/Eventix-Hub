namespace EventTixHub.Web.Services;

public record DashboardDto(decimal RevenueToday, decimal RevenueThisMonth, int TicketsSoldThisMonth);

public record RevenueRowDto(int Id, string Name, int TicketsSold, decimal Revenue);

public record HourOccupancyDto(int Hour, int Showtimes, double AveragePercent)
{
    public bool IsCritical => AveragePercent >= BookingRules.CriticalOccupancyPercent;
}

public record SalesReport(
    DateTime From, DateTime To,
    IReadOnlyList<RevenueRowDto> ByEvent, IReadOnlyList<RevenueRowDto> ByVenue,
    IReadOnlyList<ShowtimeRowDto> Occupancy, IReadOnlyList<HourOccupancyDto> ByHour);
