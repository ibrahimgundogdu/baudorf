using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class HomeContentController(ApplicationDbContext db, IStorageService storage) : Controller
{
    public async Task<IActionResult> Index()
    {
        var sections = await db.HomeSections.AsNoTracking()
            .Include(s => s.Items)
            .OrderBy(s => s.Reihenfolge).ToListAsync();
        return View(sections);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await db.HomeSections.Include(x => x.Items.OrderBy(i => i.Reihenfolge))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        return View(s);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HomeSection model, IFormFile? bild)
    {
        var s = await db.HomeSections.FindAsync(id);
        if (s is null) return NotFound();

        s.Overline = model.Overline; s.Titel = model.Titel; s.Text = model.Text;
        s.CtaText = model.CtaText; s.CtaUrl = model.CtaUrl;
        s.Cta2Text = model.Cta2Text; s.Cta2Url = model.Cta2Url;
        s.Reihenfolge = model.Reihenfolge; s.IstSichtbar = model.IstSichtbar;

        if (bild is { Length: > 0 })
        {
            if (UploadValidation.IsValidImage(bild.FileName, bild.ContentType, bild.Length, out var err))
            {
                if (!string.IsNullOrWhiteSpace(s.BildUrl)) await storage.DeleteAsync(s.BildUrl);
                await using var stream = bild.OpenReadStream();
                s.BildUrl = await storage.SaveAsync(stream, bild.FileName, bild.ContentType);
            }
            else
            {
                TempData["Error"] = err;
            }
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Abschnitt gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id)
    {
        var s = await db.HomeSections.FindAsync(id);
        if (s is null) return NotFound();
        s.IstSichtbar = !s.IstSichtbar;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int id, string? titel, string? text)
    {
        var s = await db.HomeSections.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        var max = s.Items.Count == 0 ? 0 : s.Items.Max(i => i.Reihenfolge);
        s.Items.Add(new HomeSectionItem { Titel = titel, Text = text, Reihenfolge = max + 1 });
        await db.SaveChangesAsync();
        TempData["Success"] = "Element hinzugefügt.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(int id, int itemId, string? titel, string? text)
    {
        var item = await db.HomeSectionItems.FirstOrDefaultAsync(i => i.Id == itemId && i.HomeSectionId == id);
        if (item is null) return NotFound();
        item.Titel = titel; item.Text = text;
        await db.SaveChangesAsync();
        TempData["Success"] = "Element gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id, int itemId)
    {
        var item = await db.HomeSectionItems.FirstOrDefaultAsync(i => i.Id == itemId && i.HomeSectionId == id);
        if (item is null) return NotFound();
        db.HomeSectionItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Element entfernt.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveItem(int id, int itemId, int direction)
    {
        var items = await db.HomeSectionItems.Where(i => i.HomeSectionId == id)
            .OrderBy(i => i.Reihenfolge).ToListAsync();
        var idx = items.FindIndex(i => i.Id == itemId);
        if (idx < 0) return NotFound();
        var swap = idx + direction;
        if (swap >= 0 && swap < items.Count)
        {
            (items[idx].Reihenfolge, items[swap].Reihenfolge) = (items[swap].Reihenfolge, items[idx].Reihenfolge);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id });
    }
}
