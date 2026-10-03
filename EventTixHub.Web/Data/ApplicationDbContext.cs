using EventTixHub.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, int>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Seat>().HasIndex(s => new { s.VenueId, s.Row, s.Number }).IsUnique();
        b.Entity<Review>().HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
        b.Entity<PromoCode>().HasIndex(p => p.Code).IsUnique();

        // One cart per user.
        b.Entity<Order>().HasIndex(o => o.UserId).IsUnique().HasFilter("[Status] = 0");

        // Double-booking lock: one non-cancelled ticket per (showtime, seat).
        b.Entity<Ticket>().HasIndex(t => new { t.ShowtimeId, t.SeatId }).IsUnique().HasFilter("[Status] <> 2");
        b.Entity<Ticket>().HasIndex(t => t.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

        b.Entity<Seat>().HasOne(s => s.Venue).WithMany(v => v.Seats).HasForeignKey(s => s.VenueId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Showtime>().HasOne(s => s.Event).WithMany(e => e.Showtimes).HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Review>().HasOne(r => r.Event).WithMany(e => e.Reviews).HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Review>().HasOne(r => r.User).WithMany(u => u.Reviews).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Showtime>().HasOne(s => s.Venue).WithMany(v => v.Showtimes).HasForeignKey(s => s.VenueId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().HasOne(o => o.PromoCode).WithMany(p => p.Orders).HasForeignKey(o => o.PromoCodeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Ticket>().HasOne(t => t.Showtime).WithMany(s => s.Tickets).HasForeignKey(t => t.ShowtimeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Ticket>().HasOne(t => t.Seat).WithMany().HasForeignKey(t => t.SeatId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Ticket>().HasOne(t => t.Order).WithMany(o => o.Tickets).HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
