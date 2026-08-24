using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class MediaController(ApplicationDbContext db, IMediaLibrary media, IStorageService storage) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await media.ListLibraryAsync();

        // "In Verwendung": jede URL, die irgendwo im Inhalt referenziert wird.
        var usage = await CollectUsageAsync();
        foreach (var i in items)
        {
            if (usage.TryGetValue(i.Url, out var places))
            {
                i.InUse = true;
                i.UsedIn = string.Join(", ", places.OrderBy(p => p));
            }
        }

        return View(items);
    }

    /// <summary>JSON-Liste für den Medien-Picker (Mediathek-Tab) — Disk + DB vereint.</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await media.ListLibraryAsync();
        return Json(items
            .Where(i => i.FileExists) // Picker: nur real vorhandene Dateien anbieten
            .Select(i => new { Id = i.AssetId ?? 0, i.Url, i.Alt, i.FileName, i.IsVideo }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(90_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 90_000_000)]
    public async Task<IActionResult> Upload(List<IFormFile> dateien)
    {
        var saved = new List<object>();
        var fehler = new List<string>();

        foreach (var file in dateien.Where(f => f.Length > 0))
        {
            if (!UploadValidation.IsValidMedia(file.FileName, file.ContentType, file.Length, out var err))
            {
                fehler.Add($"{file.FileName}: {err}");
                continue;
            }
            var asset = await media.SaveAsync(file);
            saved.Add(new { asset.Id, asset.Url, asset.Alt, asset.FileName });
        }

        if (IsAjax())
        {
            return Json(new { ok = saved, errors = fehler });
        }

        if (fehler.Count > 0) TempData["Error"] = string.Join(" · ", fehler);
        TempData["Success"] = $"{saved.Count} Datei(en) hochgeladen.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Ersetzt die Datei einer bestehenden URL durch einen neuen Upload — Name/URL bleiben gleich,
    /// deshalb aktualisieren sich alle Verwendungen (Objekte, Hero, …) automatisch.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(90_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 90_000_000)]
    public async Task<IActionResult> Replace(string url, IFormFile? datei)
    {
        if (string.IsNullOrWhiteSpace(url) || datei is null || datei.Length == 0)
        {
            TempData["Error"] = "Keine Datei zum Ersetzen gewählt.";
            return RedirectToAction(nameof(Index));
        }

        if (!UploadValidation.IsValidMedia(datei.FileName, datei.ContentType, datei.Length, out var err))
        {
            TempData["Error"] = $"{datei.FileName}: {err}";
            return RedirectToAction(nameof(Index));
        }

        // Gleiche Dateiendung erzwingen — sonst würde die URL nicht mehr zum Inhaltstyp passen.
        var urlExt = Path.GetExtension(url.Split('?')[0]);
        var newExt = Path.GetExtension(datei.FileName);
        if (!string.Equals(urlExt, newExt, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = $"Zum Ersetzen bitte eine Datei mit gleicher Endung ({urlExt}) wählen.";
            return RedirectToAction(nameof(Index));
        }

        await using (var stream = datei.OpenReadStream())
        {
            await storage.ReplaceAsync(url, stream);
        }

        // Metadaten des passenden MediaAsset (falls vorhanden) aktualisieren.
        var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Url == url);
        if (asset is not null)
        {
            asset.SizeBytes = datei.Length;
            asset.ContentType = datei.ContentType;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Datei ersetzt — alle Verwendungen zeigen jetzt das neue Bild.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlt(int id, string? alt)
    {
        var asset = await db.MediaAssets.FindAsync(id);
        if (asset is null) return NotFound();
        asset.Alt = alt;
        await db.SaveChangesAsync();
        TempData["Success"] = "Alt-Text gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await db.MediaAssets.FindAsync(id);
        if (asset is null) return NotFound();

        await storage.DeleteAsync(asset.Url);
        db.MediaAssets.Remove(asset);
        await db.SaveChangesAsync();
        TempData["Success"] = "Datei gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Löscht eine reine Disk-Datei ohne MediaAsset-Eintrag ("verwaiste" Datei).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            await storage.DeleteAsync(url);
            TempData["Success"] = "Datei gelöscht.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sammelt alle im Inhalt referenzierten Medien-URLs → Herkunftslabel(s).</summary>
    private async Task<Dictionary<string, HashSet<string>>> CollectUsageAsync()
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        void Add(string? url, string label)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!map.TryGetValue(url, out var set)) map[url] = set = new HashSet<string>();
            set.Add(label);
        }

        foreach (var u in await db.PropertyMedia.Select(m => new { m.Url, m.ThumbnailUrl }).ToListAsync())
        {
            Add(u.Url, "Objekt");
            Add(u.ThumbnailUrl, "Objekt");
        }
        foreach (var u in await db.HomeSections.Select(s => s.BildUrl).ToListAsync()) Add(u, "Startseite");
        foreach (var u in await db.HomeSectionItems.Select(s => s.BildUrl).ToListAsync()) Add(u, "Startseite");
        foreach (var u in await db.TeamMembers.Select(t => t.FotoUrl).ToListAsync()) Add(u, "Team");
        foreach (var u in await db.BlogPosts.Select(b => b.CoverUrl).ToListAsync()) Add(u, "News");
        foreach (var u in await db.Leistungen.Select(l => l.CoverUrl).ToListAsync()) Add(u, "Leistung");

        return map;
    }

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
