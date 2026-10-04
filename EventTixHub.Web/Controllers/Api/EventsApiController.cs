using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers.Api;

[ApiController]
[Route("api/events")]
public class EventsApiController(EventService eventService) : ControllerBase
{
    /// <summary>Paged list of events matching the afisha filters.</summary>
    /// <param name="filter">Search text, type, genre, city, venue, date and sort order.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page (1–50).</param>
    [HttpGet]
    [ProducesResponseType<PagedResult<EventCardDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<EventCardDto>> List(
        [FromQuery] EventFilter filter, int page = 1, int pageSize = 12) =>
        await eventService.SearchAsync(filter, page, Math.Clamp(pageSize, 1, 50));

    /// <summary>Event details with rating and upcoming showtimes.</summary>
    /// <param name="id">Event id.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EventApiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventApiDto>> Get(int id)
    {
        var d = await eventService.GetDetailsAsync(id);
        if (d is null)
            return NotFound();
        var e = d.Event;
        return new EventApiDto(e.Id, e.Title, e.Description, e.Type, e.Genre, e.DurationMinutes,
            e.PosterUrl, d.AverageRating, d.ReviewCount, d.Showtimes);
    }
}
