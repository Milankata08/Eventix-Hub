using System.ComponentModel.DataAnnotations;

namespace EventTixHub.Web.Models;

public class Venue : BaseEntity
{
    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    [Required, StringLength(50)]
    public string City { get; set; } = null!;

    [Required, StringLength(200)]
    public string Address { get; set; } = null!;

    [Range(1, 30)]
    public int Rows { get; set; }

    [Range(1, 40)]
    public int SeatsPerRow { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}

public class Seat : BaseEntity
{
    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Row { get; set; }

    [Range(1, int.MaxValue)]
    public int Number { get; set; }

    public SeatCategory Category { get; set; }
}
