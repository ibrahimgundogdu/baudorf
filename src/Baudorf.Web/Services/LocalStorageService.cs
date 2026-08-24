namespace Baudorf.Web.Services;

/// <summary>wwwroot/uploads altına yazan yerel disk depolama implementasyonu.</summary>
public class LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger) : IStorageService
{
    private const string UploadsFolder = "uploads";

    private static readonly string[] MediaExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif", ".svg",
        ".mp4", ".webm", ".ogg", ".ogv", ".mov"
    };

    private string Root => Path.Combine(env.WebRootPath, UploadsFolder);

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var safeName = $"{Guid.NewGuid():N}{ext}".ToLowerInvariant();

        Directory.CreateDirectory(Root);

        var fullPath = Path.Combine(Root, safeName);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fs, ct);
        }

        logger.LogInformation("Stored upload {File}", safeName);
        return $"/{UploadsFolder}/{safeName}";
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(url);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted upload {File}", url);
        }
        return Task.CompletedTask;
    }

    public async Task ReplaceAsync(string url, Stream content, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(url)
            ?? throw new InvalidOperationException("Ungültige Ziel-URL.");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fs, ct);
        }
        logger.LogInformation("Replaced upload {File}", url);
    }

    public bool Exists(string url)
    {
        var fullPath = ResolvePath(url);
        return fullPath is not null && File.Exists(fullPath);
    }

    public IReadOnlyList<StoredMedia> ListAll()
    {
        if (!Directory.Exists(Root)) return Array.Empty<StoredMedia>();

        var result = new List<StoredMedia>();
        foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(path);
            if (!MediaExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;

            var relative = Path.GetRelativePath(env.WebRootPath, path).Replace(Path.DirectorySeparatorChar, '/');
            var info = new FileInfo(path);
            result.Add(new StoredMedia($"/{relative}", info.LastWriteTimeUtc, info.Length));
        }
        return result;
    }

    /// <summary>URL'i güvenli şekilde fiziksel yola çevirir; uploads dışına çıkışı (path traversal) engeller.</summary>
    private string? ResolvePath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var relative = url.Split('?')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(env.WebRootPath, relative));

        var rootFull = Path.GetFullPath(Root);
        return fullPath.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : null;
    }
}
