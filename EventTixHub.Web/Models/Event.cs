using System.ComponentModel.DataAnnotations;

namespace EventTixHub.Web.Models;

public class Event : BaseEntity
{
    [Required, StringLength(150)]
    public string Title { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    public EventType Type { get; set; }

    [Required, StringLength(50)]
    public string Genre { get; set; } = null!;

    [Range(1, 600)]
    public int DurationMinutes { get; set; }

    [StringLength(500), Url]
    public string? PosterUrl { get; set; }

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
