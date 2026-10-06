using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Models;

public class Showtime : BaseEntity, IValidatableObject
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public DateTime StartsAt { get; set; }

    [Range(typeof(decimal), "0.01", "1000"), Precision(10, 2)]
    public decimal StandardPrice { get; set; }

    [Range(typeof(decimal), "0.01", "1000"), Precision(10, 2)]
    public decimal VipPrice { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VipPrice < StandardPrice)
            yield return new ValidationResult("VipPrice must be >= StandardPrice.", [nameof(VipPrice)]);
    }
}
