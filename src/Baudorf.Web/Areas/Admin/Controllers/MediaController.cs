using Baudorf.Web.Data;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class MediaController(ApplicationDbContext db, IMediaLibrary media, IStorageService storage) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.MediaAssets.OrderByDescending(m => m.CreatedAt).ToListAsync());

    /// <summary>JSON-Liste für den Medien-Picker (Mediathek-Tab).</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await db.MediaAssets
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.Id, m.Url, m.Alt, m.FileName })
            .ToListAsync();
        return Json(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> dateien)
    {
        var saved = new List<object>();
        var fehler = new List<string>();

        foreach (var file in dateien.Where(f => f.Length > 0))
        {
            if (!UploadValidation.IsValidImage(file.FileName, file.ContentType, file.Length, out var err))
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

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
