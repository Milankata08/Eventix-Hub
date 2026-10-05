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

public record EventOption(int Id, string Title);

public record FilterOptions(IReadOnlyList<string> Genres, IReadOnlyList<string> Cities, IReadOnlyList<VenueOption> Venues);

public record ShowtimeCardDto(
    int Id, DateTime StartsAt, int VenueId, string VenueName, string City,
    decimal StandardPrice, decimal VipPrice, int FreeSeats);

public record EventDetailsDto(
    Event Event, double? AverageRating, int ReviewCount, IReadOnlyList<ShowtimeCardDto> Showtimes);

public record ShowtimeFilter(int? EventId = null, int? VenueId = null, DateTime? From = null, DateTime? To = null);

public record ReviewDto(int Id, int UserId, string Author, int Rating, string? Comment, DateTime CreatedAt);

/// <summary>Full event description with its upcoming showtimes, as returned by the JSON API.</summary>
/// <param name="Id">Event id.</param>
/// <param name="Title">Event title.</param>
/// <param name="Description">Optional long description.</param>
/// <param name="Type">Movie or Concert.</param>
/// <param name="Genre">Genre name.</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="PosterUrl">Optional poster image URL.</param>
/// <param name="AverageRating">Mean review rating (1–5), or null when there are no reviews.</param>
/// <param name="ReviewCount">Number of reviews.</param>
/// <param name="Showtimes">Upcoming showtimes ordered by start time.</param>
public record EventApiDto(
    int Id, string Title, string? Description, EventType Type, string Genre, int DurationMinutes,
    string? PosterUrl, double? AverageRating, int ReviewCount, IReadOnlyList<ShowtimeCardDto> Showtimes);
