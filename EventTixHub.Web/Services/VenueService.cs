using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class VenueService(IRepository<Venue> venues, IRepository<Seat> seats, IRepository<Showtime> showtimes)
{
    public Task<PagedResult<Venue>> SearchAsync(string? search, string? city, int page, int pageSize = 10)
    {
        var query = venues.Query();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(v => v.Name.Contains(s) || v.Address.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(v => v.City == city);
        return query.OrderBy(v => v.Name).ToPagedResultAsync(page, pageSize);
    }

    public Task<List<string>> GetCitiesAsync() =>
        venues.Query().Select(v => v.City).Distinct().OrderBy(c => c).ToListAsync();

    public Task<List<Venue>> GetAllAsync() => venues.Query().OrderBy(v => v.Name).ToListAsync();

    public Task<Venue?> GetByIdAsync(int id) => venues.GetByIdAsync(id);

    public Task<List<Seat>> GetSeatsAsync(int venueId) =>
        seats.Query().Where(s => s.VenueId == venueId).OrderBy(s => s.Row).ThenBy(s => s.Number).ToListAsync();

    // BR-14: Rows x SeatsPerRow Standard seats.
    public async Task<Venue> CreateAsync(Venue venue)
    {
        await venues.AddAsync(venue);
        foreach (var seat in NewSeats(venue))
            venue.Seats.Add(seat);
        await venues.SaveChangesAsync();
        return venue;
    }

    // BR-14: the grid can change only while the venue has no showtimes.
    public async Task UpdateAsync(Venue venue)
    {
        var current = await venues.GetByIdAsync(venue.Id)
            ?? throw new BookingException("Залата не е намерена.");
        var gridChanged = venue.Rows != current.Rows || venue.SeatsPerRow != current.SeatsPerRow;
        if (gridChanged && await showtimes.Query().AnyAsync(s => s.VenueId == venue.Id))
            throw new BookingException("Редовете и местата не могат да се променят, след като залата има прожекции.");

        current.Name = venue.Name;
        current.City = venue.City;
        current.Address = venue.Address;
        if (gridChanged)
        {
            // Keep seats inside the new grid (and their category); drop the rest, add the missing ones.
            var existing = await seats.Query().Where(s => s.VenueId == venue.Id).ToListAsync();
            foreach (var seat in existing.Where(s => s.Row > venue.Rows || s.Number > venue.SeatsPerRow))
                seats.Remove(seat);
            var kept = existing.Where(s => s.Row <= venue.Rows && s.Number <= venue.SeatsPerRow)
                .Select(s => (s.Row, s.Number)).ToHashSet();
            foreach (var seat in NewSeats(current, venue.Rows, venue.SeatsPerRow)
                         .Where(s => !kept.Contains((s.Row, s.Number))))
                await seats.AddAsync(seat);
            current.Rows = venue.Rows;
            current.SeatsPerRow = venue.SeatsPerRow;
        }
        await venues.SaveChangesAsync();
    }

    // BR-15
    public async Task DeleteAsync(int id)
    {
        var venue = await venues.GetByIdAsync(id)
            ?? throw new BookingException("Залата не е намерена.");
        if (await showtimes.Query().AnyAsync(s => s.VenueId == id))
            throw new BookingException("Зала с прожекции не може да се изтрие.");
        venues.Remove(venue);
        await venues.SaveChangesAsync();
    }

    // BR-14: a row that is all VIP becomes Standard; any other row becomes VIP.
    public async Task ToggleRowVipAsync(int venueId, int row)
    {
        var rowSeats = await seats.Query().Where(s => s.VenueId == venueId && s.Row == row).ToListAsync();
        if (rowSeats.Count == 0)
            throw new BookingException("Редът не е намерен.");
        var target = rowSeats.All(s => s.Category == SeatCategory.Vip) ? SeatCategory.Standard : SeatCategory.Vip;
        foreach (var seat in rowSeats)
            seat.Category = target;
        await seats.SaveChangesAsync();
    }

    private static IEnumerable<Seat> NewSeats(Venue venue) => NewSeats(venue, venue.Rows, venue.SeatsPerRow);

    private static IEnumerable<Seat> NewSeats(Venue venue, int rows, int seatsPerRow) =>
        from row in Enumerable.Range(1, rows)
        from number in Enumerable.Range(1, seatsPerRow)
        select new Seat { Venue = venue, Row = row, Number = number, Category = SeatCategory.Standard };
}
