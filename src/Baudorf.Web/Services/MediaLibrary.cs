using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;

namespace Baudorf.Web.Services;

/// <summary>
/// Speichert hochgeladene Dateien über den <see cref="IStorageService"/> und registriert sie
/// zugleich in der Mediathek (<see cref="MediaAsset"/>), damit jeder Upload wiederverwendbar ist.
/// </summary>
public interface IMediaLibrary
{
    Task<MediaAsset> SaveAsync(IFormFile file, string? alt = null, CancellationToken ct = default);
}

public class MediaLibrary(ApplicationDbContext db, IStorageService storage) : IMediaLibrary
{
    public async Task<MediaAsset> SaveAsync(IFormFile file, string? alt = null, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        var url = await storage.SaveAsync(stream, file.FileName, file.ContentType, ct);

        var asset = new MediaAsset
        {
            Url = url,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Alt = alt
        };
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(ct);
        return asset;
    }
}
