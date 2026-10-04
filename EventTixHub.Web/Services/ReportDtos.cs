namespace EventTixHub.Web.Services;

public record DashboardDto(decimal RevenueToday, decimal RevenueThisMonth, int TicketsSoldThisMonth);

public record RevenueRowDto(int Id, string Name, int TicketsSold, decimal Revenue);

public record OccupancyRowDto(
    int ShowtimeId, string EventTitle, string VenueName, DateTime StartsAt,
    int Capacity, int Taken, double Percent, bool IsCritical);

public record HourOccupancyDto(int Hour, int Showtimes, double AveragePercent);
