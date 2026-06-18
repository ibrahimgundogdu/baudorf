namespace Baudorf.Web.Services;

/// <summary>wwwroot/uploads altına yazan yerel disk depolama implementasyonu.</summary>
public class LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger) : IStorageService
{
    private const string UploadsFolder = "uploads";

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var safeName = $"{Guid.NewGuid():N}{ext}".ToLowerInvariant();

        var root = Path.Combine(env.WebRootPath, UploadsFolder);
        Directory.CreateDirectory(root);

        var fullPath = Path.Combine(root, safeName);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fs, ct);
        }

        logger.LogInformation("Stored upload {File}", safeName);
        return $"/{UploadsFolder}/{safeName}";
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return Task.CompletedTask;

        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.WebRootPath, relative);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted upload {File}", url);
        }
        return Task.CompletedTask;
    }
}
