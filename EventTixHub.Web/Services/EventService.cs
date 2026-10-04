using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class EventService(
    IRepository<Event> events,
    IRepository<Showtime> showtimes,
    IRepository<Ticket> tickets,
    IRepository<Venue> venues,
    TimeProvider clock)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    public async Task<PagedResult<EventCardDto>> SearchAsync(EventFilter filter, int page, int pageSize = 12)
    {
        var now = Now;
        var query = events.Query();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(e => e.Title.Contains(s) || e.Genre.Contains(s));
        }
        if (filter.Type is { } type)
            query = query.Where(e => e.Type == type);
        if (!string.IsNullOrWhiteSpace(filter.Genre))
            query = query.Where(e => e.Genre == filter.Genre);
        if (!string.IsNullOrWhiteSpace(filter.City) || filter.VenueId is not null || filter.Date is not null)
        {
            var city = filter.City;
            var venueId = filter.VenueId;
            var from = filter.Date?.Date;
            var to = from?.AddDays(1);
            query = query.Where(e => e.Showtimes.Any(s => s.StartsAt >= now
                && (string.IsNullOrEmpty(city) || s.Venue.City == city)
                && (venueId == null || s.VenueId == venueId)
                && (from == null || (s.StartsAt >= from && s.StartsAt < to))));
        }

        // Anonymous projection: EF cannot order by a member of a constructed record.
        var cards = query.Select(e => new
        {
            e.Id, e.Title, e.Type, e.Genre, e.PosterUrl,
            AverageRating = e.Reviews.Any() ? e.Reviews.Average(r => (double)r.Rating) : (double?)null,
            NextShowtime = e.Showtimes.Where(s => s.StartsAt >= now).Min(s => (DateTime?)s.StartsAt)
        });

        cards = filter.Sort switch
        {
            EventSort.Title => cards.OrderBy(c => c.Title),
            EventSort.Rating => cards.OrderByDescending(c => c.AverageRating).ThenBy(c => c.Title),
            // Events without an upcoming showtime go last.
            _ => cards.OrderBy(c => c.NextShowtime == null).ThenBy(c => c.NextShowtime).ThenBy(c => c.Title)
        };
        var result = await cards.ToPagedResultAsync(page, pageSize);
        return new PagedResult<EventCardDto>(
            result.Items.Select(c => new EventCardDto(c.Id, c.Title, c.Type, c.Genre, c.PosterUrl,
                c.AverageRating, c.NextShowtime)).ToList(),
            result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<FilterOptions> GetFilterOptionsAsync()
    {
        var genres = await events.Query().Select(e => e.Genre).Distinct().OrderBy(g => g).ToListAsync();
        var cities = await venues.Query().Select(v => v.City).Distinct().OrderBy(c => c).ToListAsync();
        var venueList = await venues.Query().OrderBy(v => v.Name)
            .Select(v => new VenueOption(v.Id, v.Name)).ToListAsync();
        return new FilterOptions(genres, cities, venueList);
    }

    public async Task<IReadOnlyList<EventOption>> GetEventOptionsAsync() =>
        await events.Query().OrderBy(e => e.Title).Select(e => new EventOption(e.Id, e.Title)).ToListAsync();

    public async Task<EventDetailsDto?> GetDetailsAsync(int id)
    {
        var ev = await events.GetByIdAsync(id);
        if (ev is null)
            return null;

        var now = Now;
        var ratings = await events.Query().Where(e => e.Id == id)
            .SelectMany(e => e.Reviews).Select(r => r.Rating).ToListAsync();
        var upcoming = await showtimes.Query()
            .Where(s => s.EventId == id && s.StartsAt >= now)
            .OrderBy(s => s.StartsAt)
            .Select(s => new ShowtimeCardDto(s.Id, s.StartsAt, s.VenueId, s.Venue.Name, s.Venue.City,
                s.StandardPrice, s.VipPrice,
                s.Venue.Rows * s.Venue.SeatsPerRow - s.Tickets.Count(t => t.Status == TicketStatus.Paid
                    || (t.Status == TicketStatus.Reserved && t.ReservedUntil > now))))
            .ToListAsync();
        return new EventDetailsDto(ev, ratings.Count == 0 ? null : ratings.Average(), ratings.Count, upcoming);
    }

    public Task<Event?> GetEventAsync(int id) => events.GetByIdAsync(id);

    public async Task<Event> CreateEventAsync(Event ev)
    {
        await events.AddAsync(ev);
        await events.SaveChangesAsync();
        return ev;
    }

    public async Task UpdateEventAsync(Event ev)
    {
        var current = await events.GetByIdAsync(ev.Id)
            ?? throw new BookingException("Събитието не е намерено.");
        current.Title = ev.Title;
        current.Description = ev.Description;
        current.Type = ev.Type;
        current.Genre = ev.Genre;
        current.DurationMinutes = ev.DurationMinutes;
        current.PosterUrl = ev.PosterUrl;
        await events.SaveChangesAsync();
    }

    // BR-15
    public async Task DeleteEventAsync(int id)
    {
        var ev = await events.GetByIdAsync(id)
            ?? throw new BookingException("Събитието не е намерено.");
        if (await tickets.Query().AnyAsync(t => t.Showtime.EventId == id))
            throw new BookingException("Събитие с продадени или резервирани билети не може да се изтрие.");
        events.Remove(ev);
        await events.SaveChangesAsync();
    }

    public async Task<PagedResult<ShowtimeRowDto>> SearchShowtimesAsync(ShowtimeFilter filter, int page, int pageSize = 10)
    {
        var query = showtimes.Query();
        if (filter.EventId is { } eventId)
            query = query.Where(s => s.EventId == eventId);
        if (filter.VenueId is { } venueId)
            query = query.Where(s => s.VenueId == venueId);
        if (filter.From is { } from)
            query = query.Where(s => s.StartsAt >= from.Date);
        if (filter.To is { } to)
            query = query.Where(s => s.StartsAt < to.Date.AddDays(1));
        return await Occupancy(query.OrderBy(s => s.StartsAt)).ToPagedResultAsync(page, pageSize);
    }

    // Sold / held / free per showtime of one event.
    public async Task<IReadOnlyList<ShowtimeRowDto>> GetEventOccupancyAsync(int eventId) =>
        await Occupancy(showtimes.Query().Where(s => s.EventId == eventId).OrderBy(s => s.StartsAt)).ToListAsync();

    public Task<Showtime?> GetShowtimeAsync(int id) => showtimes.GetByIdAsync(id);

    // BR-12
    public async Task<Showtime> CreateShowtimeAsync(Showtime showtime)
    {
        if (showtime.StartsAt <= Now)
            throw new BookingException("Прожекцията трябва да започва в бъдещето.");
        await EnsureNoOverlapAsync(showtime);
        await showtimes.AddAsync(showtime);
        await showtimes.SaveChangesAsync();
        return showtime;
    }

    public async Task UpdateShowtimeAsync(Showtime showtime)
    {
        var current = await showtimes.GetByIdAsync(showtime.Id)
            ?? throw new BookingException("Прожекцията не е намерена.");
        if (showtime.StartsAt != current.StartsAt && showtime.StartsAt <= Now)
            throw new BookingException("Прожекцията трябва да започва в бъдещето.");
        if ((showtime.VenueId != current.VenueId || showtime.EventId != current.EventId)
            && await tickets.Query().AnyAsync(t => t.ShowtimeId == showtime.Id))
            throw new BookingException("Залата и събитието не могат да се сменят, след като има билети.");
        await EnsureNoOverlapAsync(showtime);

        current.EventId = showtime.EventId;
        current.VenueId = showtime.VenueId;
        current.StartsAt = showtime.StartsAt;
        current.StandardPrice = showtime.StandardPrice;
        current.VipPrice = showtime.VipPrice;
        await showtimes.SaveChangesAsync();
    }

    // BR-15
    public async Task DeleteShowtimeAsync(int id)
    {
        var showtime = await showtimes.GetByIdAsync(id)
            ?? throw new BookingException("Прожекцията не е намерена.");
        if (await tickets.Query().AnyAsync(t => t.ShowtimeId == id))
            throw new BookingException("Прожекция с продадени или резервирани билети не може да се изтрие.");
        showtimes.Remove(showtime);
        await showtimes.SaveChangesAsync();
    }

    private IQueryable<ShowtimeRowDto> Occupancy(IQueryable<Showtime> query)
    {
        var now = Now;
        return query.Select(s => new ShowtimeRowDto(s.Id, s.EventId, s.Event.Title, s.Venue.Name, s.StartsAt,
            s.Venue.Rows * s.Venue.SeatsPerRow,
            s.Tickets.Count(t => t.Status == TicketStatus.Paid),
            s.Tickets.Count(t => t.Status == TicketStatus.Reserved && t.ReservedUntil > now),
            s.Venue.Rows * s.Venue.SeatsPerRow
                - s.Tickets.Count(t => t.Status == TicketStatus.Paid
                    || (t.Status == TicketStatus.Reserved && t.ReservedUntil > now))));
    }

    private async Task EnsureNoOverlapAsync(Showtime showtime)
    {
        var duration = (await events.GetByIdAsync(showtime.EventId)
            ?? throw new BookingException("Събитието не е намерено.")).DurationMinutes;
        var others = await showtimes.Query()
            .Where(s => s.VenueId == showtime.VenueId && s.Id != showtime.Id)
            .Select(s => new { s.StartsAt, s.Event.DurationMinutes })
            .ToListAsync();
        if (others.Any(o => BookingRules.Overlaps(showtime.StartsAt, duration, o.StartsAt, o.DurationMinutes)))
            throw new BookingException("В тази зала има друга прожекция в същото време.");
    }
}
