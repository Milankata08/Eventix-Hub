using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Unit;

public class PosterStorageTests
{
    [Theory]
    [InlineData("a.jpg", ".jpg")]
    [InlineData("A.JPEG", ".jpg")]
    [InlineData("b.png", ".png")]
    public void Validate_AcceptsJpgAndPng(string name, string expected) =>
        Assert.Equal(expected, PosterStorage.Validate(name, 1000));

    [Theory]
    [InlineData("a.gif", 1000)]
    [InlineData("a.exe", 1000)]
    [InlineData("a", 1000)]
    [InlineData("a.jpg", 0)]
    [InlineData("a.jpg", PosterStorage.MaxBytes + 1)]
    public void Validate_RejectsBadTypeOrSize(string name, long length) =>
        Assert.Throws<BookingException>(() => PosterStorage.Validate(name, length));

    [Fact]
    public void Validate_AcceptsExactlyMaxSize() =>
        Assert.Equal(".png", PosterStorage.Validate("a.png", PosterStorage.MaxBytes));
}
