using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EventTixHub.Tests.TestHelpers;

// A throwaway wwwroot for PosterStorage; deleted on Dispose.
public sealed class TempWebRoot : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("eth-").FullName;
    public PosterStorage Posters { get; }

    public TempWebRoot()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(Root);
        Posters = new PosterStorage(env.Object);
    }

    public string[] Files() => Directory.Exists(Path.Combine(Root, "uploads"))
        ? Directory.GetFiles(Path.Combine(Root, "uploads")) : [];

    // A tiny upload that starts with the PNG signature.
    public static IFormFile Png(string name = "p.png")
    {
        var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0]);
        return new FormFile(stream, 0, stream.Length, "Poster", name);
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
