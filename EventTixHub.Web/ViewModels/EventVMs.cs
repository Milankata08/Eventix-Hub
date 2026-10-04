using System.ComponentModel.DataAnnotations;
using EventTixHub.Web.Services;

namespace EventTixHub.Web.ViewModels;

public class AfishaVM
{
    public EventFilter Filter { get; set; } = new();
    public FilterOptions Options { get; set; } = null!;
    public PagedResult<EventCardDto> Events { get; set; } = null!;
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
    public int EventId { get; set; }

    [Range(1, 5, ErrorMessage = "Оценката трябва да е между 1 и 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Коментарът може да е до {1} символа.")]
    public string? Comment { get; set; }
}
