using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using MockQueryable;
using Moq;

namespace EventTixHub.Tests.Unit;

public class ReviewServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly List<Review> _reviews = [];
    private readonly List<Ticket> _tickets = [];
    private readonly Mock<IRepository<Review>> _reviewRepo = new();

    private ReviewService Service()
    {
        _reviewRepo.Setup(r => r.Query()).Returns(_reviews.BuildMock());
        var tickets = new Mock<IRepository<Ticket>>();
        tickets.Setup(r => r.Query()).Returns(_tickets.BuildMock());
        return new ReviewService(_reviewRepo.Object, tickets.Object, new FakeClock(Now));
    }

    private Ticket Attended(int userId = 1, int eventId = 1, int hoursAgo = 3) => new()
    {
        Status = TicketStatus.Paid,
        Order = new Order { UserId = userId },
        Showtime = new Showtime { EventId = eventId, StartsAt = Now.DateTime.AddHours(-hoursAgo) }
    };

    [Fact]
    public async Task AddReview_RejectsUserWithoutAttendedPaidTicket()
    {
        // Paid ticket, but the showtime has not started yet.
        _tickets.Add(Attended(hoursAgo: -2));

        await Assert.ThrowsAsync<BookingException>(() => Service().AddAsync(1, 1, 5, "Супер"));

        _reviewRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddReview_RejectsUserWithNoTicketAtAll()
    {
        await Assert.ThrowsAsync<BookingException>(() => Service().AddAsync(1, 1, 5, "Супер"));
    }

    [Fact]
    public async Task AddReview_SavesReviewForAttendee()
    {
        _tickets.Add(Attended());

        await Service().AddAsync(1, 1, 4, "Добре");

        _reviewRepo.Verify(r => r.AddAsync(It.Is<Review>(v =>
            v.UserId == 1 && v.EventId == 1 && v.Rating == 4 && v.CreatedAt == Now.DateTime)), Times.Once);
        _reviewRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReview_RejectsSecondReviewOfSameEvent()
    {
        _tickets.Add(Attended());
        _reviews.Add(new Review { UserId = 1, EventId = 1, Rating = 3 });

        await Assert.ThrowsAsync<BookingException>(() => Service().AddAsync(1, 1, 5, null));
    }

    [Fact]
    public async Task DeleteReview_RejectsOtherUserWhoIsNotAdmin()
    {
        _reviews.Add(new Review { Id = 7, UserId = 1, EventId = 1, Rating = 3 });

        await Assert.ThrowsAsync<BookingException>(() => Service().DeleteAsync(7, userId: 2, isAdmin: false));

        _reviewRepo.Verify(r => r.Remove(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task DeleteReview_AllowsAdmin()
    {
        var review = new Review { Id = 7, UserId = 1, EventId = 1, Rating = 3 };
        _reviews.Add(review);

        await Service().DeleteAsync(7, userId: 2, isAdmin: true);

        _reviewRepo.Verify(r => r.Remove(review), Times.Once);
    }
}
