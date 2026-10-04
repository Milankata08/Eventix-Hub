using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public enum EventSort { Date, Title, Rating }

public record EventFilter(
    string? Search = null, EventType? Type = null, string? Genre = null, string? City = null,
    int? VenueId = null, DateTime? Date = null, EventSort Sort = EventSort.Date);

public record EventCardDto(
    int Id, string Title, EventType Type, string Genre, string? PosterUrl,
    double? AverageRating, DateTime? NextShowtime);

public record VenueOption(int Id, string Name);

public record FilterOptions(IReadOnlyList<string> Genres, IReadOnlyList<string> Cities, IReadOnlyList<VenueOption> Venues);

public record ShowtimeCardDto(
    int Id, DateTime StartsAt, int VenueId, string VenueName, string City,
    decimal StandardPrice, decimal VipPrice, int FreeSeats);

public record EventDetailsDto(
    Event Event, double? AverageRating, int ReviewCount, IReadOnlyList<ShowtimeCardDto> Showtimes);

public record ShowtimeFilter(int? EventId = null, int? VenueId = null, DateTime? From = null, DateTime? To = null);

public record ShowtimeRowDto(
    int Id, int EventId, string EventTitle, string VenueName, DateTime StartsAt,
    int Capacity, int Sold, int Held, int Free);

public record ReviewDto(int Id, int UserId, string Author, int Rating, string? Comment, DateTime CreatedAt);
