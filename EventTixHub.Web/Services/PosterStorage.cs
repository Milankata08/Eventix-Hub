namespace EventTixHub.Web.Services;

/// <summary>Saves uploaded event posters under wwwroot/uploads with a random file name.</summary>
public class PosterStorage(IWebHostEnvironment env)
{
    public const long MaxBytes = 2 * 1024 * 1024;
    const string UrlPrefix = "/uploads/";

    /// <summary>Returns the extension to store (".jpg"/".png"), or throws <see cref="BookingException"/>.</summary>
    public static string Validate(string fileName, long length)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".jpeg")
            ext = ".jpg";
        if (ext is not (".jpg" or ".png"))
            throw new BookingException("Плакатът трябва да е JPG или PNG файл.");
        if (length is <= 0 or > MaxBytes)
            throw new BookingException("Плакатът трябва да е до 2 MB.");
        return ext;
    }

    /// <summary>Stores the file and returns its public URL.</summary>
    public async Task<string> SaveAsync(IFormFile file)
    {
        var ext = Validate(file.FileName, file.Length);
        var dir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + ext;
        await using var stream = File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(stream);
        return UrlPrefix + name;
    }

    /// <summary>Deletes a previously uploaded poster; ignores external URLs and missing files.</summary>
    public void Delete(string? posterUrl)
    {
        if (posterUrl is null || !posterUrl.StartsWith(UrlPrefix))
            return;
        var path = Path.Combine(env.WebRootPath, "uploads", Path.GetFileName(posterUrl));
        if (File.Exists(path))
            File.Delete(path);
    }
}
