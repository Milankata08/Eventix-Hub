using System.ComponentModel.DataAnnotations;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Web.ViewModels;

public enum PosterSize { Card, Hero, Thumb }

public record PosterVM(int EventId, string Title, EventType Type, string Genre, string? PosterUrl,
    DateTime? Date, string? Venue, string? City, PosterSize Size, bool Link = true);

public class AfishaVM
{
    public EventFilter Filter { get; set; } = new();
    public FilterOptions Options { get; set; } = null!;
    public PagedResult<EventCardDto> Events { get; set; } = null!;

    // Filters the pager keeps when changing page.
    public Dictionary<string, string?> RouteValues() => new()
    {
        ["Search"] = Filter.Search,
        ["Type"] = Filter.Type?.ToString(),
        ["Genre"] = Filter.Genre,
        ["City"] = Filter.City,
        ["VenueId"] = Filter.VenueId?.ToString(),
        ["Date"] = Format.DateInput(Filter.Date),
        ["Sort"] = Filter.Sort.ToString()
    };
}

public class EventDetailsVM
{
    public EventDetailsDto Details { get; set; } = null!;
    public PagedResult<ReviewDto> Reviews { get; set; } = null!;
    public bool CanReview { get; set; }
    public int? CurrentUserId { get; set; }
    public bool IsAdmin { get; set; }
}

public class ReviewVM
{
    [Range(1, int.MaxValue, ErrorMessage = "Невалидно събитие.")]
    public int EventId { get; set; }

    [Range(1, 5, ErrorMessage = "Оценката трябва да е между 1 и 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Коментарът може да е до {1} символа.")]
    public string? Comment { get; set; }
}
