using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Models;

public class Order : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    [ConcurrencyCheck]
    public OrderStatus Status { get; set; }

    public int? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }

    [Precision(10, 2)]
    public decimal TotalAmount { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Ticket : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ShowtimeId { get; set; }
    public Showtime Showtime { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    [ConcurrencyCheck]
    public TicketStatus Status { get; set; }

    public ConcessionType Concession { get; set; }

    [Precision(10, 2)]
    public decimal Price { get; set; }

    public DateTime? ReservedUntil { get; set; }

    [StringLength(20)]
    public string? Code { get; set; }

    public DateTime? CancelledAt { get; set; }
}
