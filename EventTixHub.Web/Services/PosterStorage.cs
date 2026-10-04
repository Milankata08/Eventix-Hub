namespace EventTixHub.Web.Services;

/// <summary>Saves uploaded event posters under wwwroot/uploads with a random file name.</summary>
public class PosterStorage(IWebHostEnvironment env)
{
    public const long MaxBytes = 2 * 1024 * 1024;
    const string UploadsDir = "uploads";
    const string UrlPrefix = "/" + UploadsDir + "/";

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

    /// <summary>True when the leading bytes match the PNG or JPEG signature for <paramref name="ext"/>.</summary>
    public static bool HasImageSignature(ReadOnlySpan<byte> header, string ext) => ext == ".png"
        ? header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
        : header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF });

    /// <summary>Stores the file and returns its public URL.</summary>
    public async Task<string> SaveAsync(IFormFile file)
    {
        var ext = Validate(file.FileName, file.Length);
        var header = new byte[8];
        await using (var peek = file.OpenReadStream())
            header = header[..await peek.ReadAsync(header)];
        if (!HasImageSignature(header, ext))
            throw new BookingException("Файлът не е валидно JPG или PNG изображение.");

        var dir = Path.Combine(env.WebRootPath, UploadsDir);
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + ext;
        var path = Path.Combine(dir, name);
        try
        {
            await using var stream = File.Create(path);
            await file.CopyToAsync(stream);
        }
        catch
        {
            File.Delete(path); // no partial file left behind
            throw;
        }
        return UrlPrefix + name;
    }

    /// <summary>Best-effort delete of an uploaded poster; ignores external URLs and I/O failures.</summary>
    public void Delete(string? posterUrl)
    {
        if (posterUrl is null || !posterUrl.StartsWith(UrlPrefix))
            return;
        try
        {
            File.Delete(Path.Combine(env.WebRootPath, UploadsDir, Path.GetFileName(posterUrl)));
        }
        catch (IOException)
        {
        }
    }
}
