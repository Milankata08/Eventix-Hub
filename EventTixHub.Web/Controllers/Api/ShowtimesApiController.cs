using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/showtimes")]
public class ShowtimesApiController(TicketService ticketService) : ControllerBase
{
    /// <summary>Seat map of a showtime with live statuses, counts and prices.</summary>
    /// <param name="id">Showtime id.</param>
    [HttpGet("{id:int}/seats")]
    [ProducesResponseType<SeatMapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeatMapDto>> Seats(int id)
    {
        var map = await ticketService.GetSeatMapAsync(id);
        return map is null ? NotFound() : map;
    }
}
