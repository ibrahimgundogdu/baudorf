using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class PropertiesController(ApplicationDbContext db, IStorageService storage) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = db.Properties.AsNoTracking().Include(p => p.Medien).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Titel.Contains(q) || (p.Region != null && p.Region.Contains(q)));

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        ViewData["q"] = q;
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new Property { Land = "Deutschland", Status = PropertyStatus.Verfuegbar });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Property model)
    {
        NormalizeSlug(model);
        await ValidateSlugUniqueAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        model.CreatedAt = DateTime.UtcNow;
        db.Properties.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt angelegt. Jetzt Bilder hinzufügen.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Properties.Include(x => x.Medien.OrderBy(m => m.Reihenfolge))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return View("Form", p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Property model)
    {
        var p = await db.Properties.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        NormalizeSlug(model);
        await ValidateSlugUniqueAsync(model, id);
        if (!ModelState.IsValid)
        {
            await db.Entry(p).Collection(x => x.Medien).LoadAsync();
            model.Medien = p.Medien;
            return View("Form", model);
        }

        // Felder übernehmen
        p.Titel = model.Titel; p.Slug = model.Slug; p.Art = model.Art; p.Status = model.Status;
        p.Region = model.Region; p.Land = model.Land; p.AdresseIntern = model.AdresseIntern;
        p.Lat = model.Lat; p.Lng = model.Lng;
        p.Wohnflaeche = model.Wohnflaeche; p.Grundstuecksflaeche = model.Grundstuecksflaeche;
        p.Baujahr = model.Baujahr; p.Zustand = model.Zustand; p.Energieklasse = model.Energieklasse;
        p.Einheiten = model.Einheiten; p.Faktor = model.Faktor; p.RenditeProzent = model.RenditeProzent;
        p.Kaufpreis = model.Kaufpreis; p.Beschreibung = model.Beschreibung;
        p.IstOffMarket = model.IstOffMarket; p.IstFeatured = model.IstFeatured; p.IstVeroeffentlicht = model.IstVeroeffentlicht;
        p.MetaTitle = model.MetaTitle; p.MetaDescription = model.MetaDescription;
        p.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Properties.Include(x => x.Medien).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        foreach (var m in p.Medien)
            await storage.DeleteAsync(m.Url);

        db.Properties.Remove(p);
        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(80 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(int id, List<IFormFile> dateien)
    {
        var p = await db.Properties.Include(x => x.Medien).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        var maxOrder = p.Medien.Count == 0 ? 0 : p.Medien.Max(m => m.Reihenfolge);
        var hatCover = p.Medien.Any(m => m.IstCover);
        var added = 0;

        foreach (var file in dateien.Where(f => f.Length > 0))
        {
            if (!UploadValidation.IsValidImage(file.FileName, file.ContentType, file.Length, out var err))
            {
                TempData["Error"] = err;
                continue;
            }
            await using var stream = file.OpenReadStream();
            var url = await storage.SaveAsync(stream, file.FileName, file.ContentType);
            p.Medien.Add(new PropertyMedia
            {
                Typ = MediaType.Image,
                Url = url,
                Reihenfolge = ++maxOrder,
                IstCover = !hatCover && added == 0 && !p.Medien.Any(m => m.IstCover),
                Alt = p.Titel
            });
            added++;
        }

        await db.SaveChangesAsync();
        if (added > 0) TempData["Success"] = $"{added} Bild(er) hochgeladen.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(int id, int mediaId)
    {
        var medien = await db.PropertyMedia.Where(m => m.PropertyId == id).ToListAsync();
        foreach (var m in medien) m.IstCover = m.Id == mediaId;
        await db.SaveChangesAsync();
        TempData["Success"] = "Titelbild gesetzt.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMedia(int id, int mediaId)
    {
        var m = await db.PropertyMedia.FirstOrDefaultAsync(x => x.Id == mediaId && x.PropertyId == id);
        if (m is null) return NotFound();
        await storage.DeleteAsync(m.Url);
        db.PropertyMedia.Remove(m);
        await db.SaveChangesAsync();
        TempData["Success"] = "Bild entfernt.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveMedia(int id, int mediaId, int direction)
    {
        var medien = await db.PropertyMedia.Where(m => m.PropertyId == id)
            .OrderBy(m => m.Reihenfolge).ToListAsync();
        var idx = medien.FindIndex(m => m.Id == mediaId);
        if (idx < 0) return NotFound();
        var swap = idx + direction;
        if (swap >= 0 && swap < medien.Count)
        {
            (medien[idx].Reihenfolge, medien[swap].Reihenfolge) = (medien[swap].Reihenfolge, medien[idx].Reihenfolge);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    private static void NormalizeSlug(Property model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug) && !string.IsNullOrWhiteSpace(model.Titel))
            model.Slug = SlugHelper.Generate(model.Titel);
        else if (!string.IsNullOrWhiteSpace(model.Slug))
            model.Slug = SlugHelper.Generate(model.Slug);
    }

    private async Task ValidateSlugUniqueAsync(Property model, int? exceptId = null)
    {
        if (string.IsNullOrWhiteSpace(model.Slug)) return;
        var exists = await db.Properties.AnyAsync(p => p.Slug == model.Slug && p.Id != (exceptId ?? 0));
        if (exists)
            ModelState.AddModelError(nameof(Property.Slug), "Dieser Slug ist bereits vergeben.");
    }
}
