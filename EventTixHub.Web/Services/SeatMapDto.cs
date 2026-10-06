using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public record SeatDto(int Id, int Row, int Number, SeatCategory Category, SeatStatus Status);

public record SeatMapDto(
    int ShowtimeId, EventType EventType, string EventTitle, string VenueName, DateTime StartsAt,
    decimal StandardPrice, decimal VipPrice,
    IReadOnlyList<SeatDto> Seats, int FreeCount, int HeldCount, int SoldCount);
