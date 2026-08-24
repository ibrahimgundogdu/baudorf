using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Services;

/// <summary>
/// Speichert hochgeladene Dateien über den <see cref="IStorageService"/> und registriert sie
/// zugleich in der Mediathek (<see cref="MediaAsset"/>), damit jeder Upload wiederverwendbar ist.
/// </summary>
public interface IMediaLibrary
{
    Task<MediaAsset> SaveAsync(IFormFile file, string? alt = null, CancellationToken ct = default);

    /// <summary>
    /// Mediathek-Inhalt als Vereinigung von Disk (wwwroot/uploads, inkl. Unterordner) und
    /// DB (<see cref="MediaAsset"/>). So bleibt keine Datei unsichtbar; verwaiste DB-Einträge
    /// (Datei fehlt) werden mit <see cref="MediaLibraryItem.FileExists"/>=false markiert.
    /// </summary>
    Task<List<MediaLibraryItem>> ListLibraryAsync(CancellationToken ct = default);
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

    public async Task<List<MediaLibraryItem>> ListLibraryAsync(CancellationToken ct = default)
    {
        var assets = await db.MediaAssets.AsNoTracking().ToListAsync(ct);
        var assetByUrl = new Dictionary<string, MediaAsset>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in assets) assetByUrl[a.Url] = a;

        var diskFiles = storage.ListAll();
        var items = new List<MediaLibraryItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Diskteki her dosya (DB kaydı olsun olmasın) — videolar ve hero görselleri dahil.
        foreach (var f in diskFiles)
        {
            seen.Add(f.Url);
            assetByUrl.TryGetValue(f.Url, out var asset);
            items.Add(new MediaLibraryItem
            {
                AssetId = asset?.Id,
                Url = f.Url,
                FileName = asset?.FileName ?? Path.GetFileName(f.Url),
                Alt = asset?.Alt,
                IsVideo = DisplayHelpers.IsVideoUrl(f.Url),
                FileExists = true,
                SortDate = asset?.CreatedAt ?? f.Modified
            });
        }

        // 2) DB'de olup diski kaybolan kayıtlar ("Datei fehlt") — silinebilsinler diye görünsün.
        foreach (var a in assets)
        {
            if (seen.Contains(a.Url)) continue;
            items.Add(new MediaLibraryItem
            {
                AssetId = a.Id,
                Url = a.Url,
                FileName = a.FileName ?? Path.GetFileName(a.Url),
                Alt = a.Alt,
                IsVideo = DisplayHelpers.IsVideoUrl(a.Url),
                FileExists = false,
                SortDate = a.CreatedAt
            });
        }

        return items.OrderByDescending(i => i.SortDate).ToList();
    }
}
