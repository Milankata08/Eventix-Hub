using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class ReviewService(IRepository<Review> reviews, IRepository<Ticket> tickets, TimeProvider clock)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    public Task<PagedResult<ReviewDto>> GetForEventAsync(int eventId, int page, int pageSize = 10) =>
        reviews.Query()
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(r => new ReviewDto(r.Id, r.UserId, r.User.FirstName + " " + r.User.LastName,
                r.Rating, r.Comment, r.CreatedAt))
            .ToPagedResultAsync(page, pageSize);

    // BR-13
    public async Task<bool> CanReviewAsync(int userId, int eventId) =>
        !await reviews.Query().AnyAsync(r => r.UserId == userId && r.EventId == eventId)
        && await HasAttendedAsync(userId, eventId);

    public async Task<Review> AddAsync(int userId, int eventId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new BookingException("Оценката трябва да е между 1 и 5.");
        if (comment?.Length > 1000)
            throw new BookingException("Коментарът може да е най-много 1000 символа.");
        if (await reviews.Query().AnyAsync(r => r.UserId == userId && r.EventId == eventId))
            throw new BookingException("Вече сте оценили това събитие.");
        if (!await HasAttendedAsync(userId, eventId))
            throw new BookingException("Можете да оценявате само събития, на които сте били с платен билет.");

        var review = new Review
        {
            UserId = userId, EventId = eventId, Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(), CreatedAt = Now
        };
        await reviews.AddAsync(review);
        try
        {
            await reviews.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique (UserId, EventId): a double submit lost the race.
            throw new BookingException("Вече сте оценили това събитие.");
        }
        return review;
    }

    // The author or an admin; the controller supplies isAdmin from the user's role.
    public async Task DeleteAsync(int reviewId, int userId, bool isAdmin)
    {
        var review = await reviews.Query().FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new BookingException("Оценката не е намерена.");
        if (review.UserId != userId && !isAdmin)
            throw new BookingException("Можете да изтриете само собствена оценка.");
        reviews.Remove(review);
        await reviews.SaveChangesAsync();
    }

    private Task<bool> HasAttendedAsync(int userId, int eventId)
    {
        var now = Now;
        return tickets.Query().AnyAsync(t => t.Status == TicketStatus.Paid && t.Order.UserId == userId
            && t.Showtime.EventId == eventId && t.Showtime.StartsAt <= now);
    }
}
