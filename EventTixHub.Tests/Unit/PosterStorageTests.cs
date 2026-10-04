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

    [Theory]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, true)]
    [InlineData(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
    [InlineData(".png", new byte[] { 0xFF, 0xD8, 0xFF }, false)]
    [InlineData(".jpg", new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C }, false)]
    [InlineData(".png", new byte[] { 0x89, 0x50 }, false)]
    public void HasImageSignature_ChecksMagicBytes(string ext, byte[] header, bool expected) =>
        Assert.Equal(expected, PosterStorage.HasImageSignature(header, ext));
}
