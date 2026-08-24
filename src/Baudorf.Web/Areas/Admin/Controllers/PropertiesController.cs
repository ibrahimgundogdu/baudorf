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
public class PropertiesController(ApplicationDbContext db, IStorageService storage, IMediaLibrary media) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = db.Properties.AsNoTracking().Include(p => p.Medien).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Titel.Contains(q) || (p.Region != null && p.Region.Contains(q)));

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        ViewData["q"] = q;
        ViewData["TrashCount"] = await db.Properties.IgnoreQueryFilters().CountAsync(p => p.IstGeloescht);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new Property { Land = "Deutschland", Status = PropertyStatus.Verfuegbar });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Property model)
    {
        // Slug ist nicht mehr Pflicht → evtl. Validierungsfehler des Leerfelds verwerfen,
        // danach automatisch einen eindeutigen Slug erzeugen.
        ModelState.Remove(nameof(Property.Slug));
        if (!ModelState.IsValid) return View("Form", model);
        await AssignUniqueSlugAsync(model);

        model.CreatedAt = DateTimeOffset.UtcNow;
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

        ModelState.Remove(nameof(Property.Slug));
        if (!ModelState.IsValid)
        {
            await db.Entry(p).Collection(x => x.Medien).LoadAsync();
            model.Medien = p.Medien;
            return View("Form", model);
        }
        await AssignUniqueSlugAsync(model, id);

        // Felder übernehmen
        p.Titel = model.Titel; p.Slug = model.Slug; p.Art = model.Art; p.Status = model.Status;
        p.Region = model.Region; p.Land = model.Land; p.AdresseIntern = model.AdresseIntern;
        p.Lat = model.Lat; p.Lng = model.Lng;
        p.Wohnflaeche = model.Wohnflaeche; p.Gewerbeflaeche = model.Gewerbeflaeche;
        p.Grundstuecksflaeche = model.Grundstuecksflaeche;
        p.Baujahr = model.Baujahr; p.Zustand = model.Zustand; p.Energieklasse = model.Energieklasse;
        p.Einheiten = model.Einheiten; p.Faktor = model.Faktor; p.RenditeProzent = model.RenditeProzent;
        p.Kaufpreis = model.Kaufpreis; p.Beschreibung = model.Beschreibung; p.VideoUrl = model.VideoUrl;
        p.IstOffMarket = model.IstOffMarket; p.IstFeatured = model.IstFeatured; p.IstVeroeffentlicht = model.IstVeroeffentlicht;
        p.MetaTitle = model.MetaTitle; p.MetaDescription = model.MetaDescription;
        p.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>Soft-Delete: verschiebt das Objekt in den Papierkorb (wiederherstellbar).
    /// Dateien/Bilder bleiben erhalten, bis endgültig gelöscht wird.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Properties.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        p.IstGeloescht = true;
        p.GeloeschtAm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt in den Papierkorb verschoben. Es kann dort wiederhergestellt werden.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Papierkorb (Soft-Delete) ----------

    public async Task<IActionResult> Papierkorb()
    {
        var list = await db.Properties.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.IstGeloescht).Include(p => p.Medien)
            .OrderByDescending(p => p.GeloeschtAm).ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var p = await db.Properties.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.IstGeloescht);
        if (p is null) return NotFound();

        p.IstGeloescht = false;
        p.GeloeschtAm = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt wiederhergestellt.";
        return RedirectToAction(nameof(Papierkorb));
    }

    /// <summary>Endgültig löschen (aus dem Papierkorb): entfernt Datensatz + zugehörige Dateien.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePermanent(int id)
    {
        var p = await db.Properties.IgnoreQueryFilters().Include(x => x.Medien)
            .FirstOrDefaultAsync(x => x.Id == id && x.IstGeloescht);
        if (p is null) return NotFound();

        foreach (var m in p.Medien)
            await storage.DeleteAsync(m.Url);

        db.Properties.Remove(p);
        await db.SaveChangesAsync();
        TempData["Success"] = "Objekt endgültig gelöscht.";
        return RedirectToAction(nameof(Papierkorb));
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
            var url = (await media.SaveAsync(file)).Url;
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

    /// <summary>Ein bereits in der Mediathek vorhandenes Bild (URL unter /uploads) als Medium anhängen.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFromLibrary(int id, string url)
    {
        var p = await db.Properties.Include(x => x.Medien).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        // Nur eigene Mediathek-URLs zulassen (keine beliebigen externen Adressen).
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Ungültige Bildauswahl.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Doppelte Zuordnung desselben Bildes vermeiden.
        if (p.Medien.Any(m => m.Url == url))
        {
            TempData["Error"] = "Dieses Bild ist bereits zugeordnet.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var maxOrder = p.Medien.Count == 0 ? 0 : p.Medien.Max(m => m.Reihenfolge);
        p.Medien.Add(new PropertyMedia
        {
            Typ = MediaType.Image,
            Url = url,
            Reihenfolge = maxOrder + 1,
            IstCover = !p.Medien.Any(m => m.IstCover),
            Alt = p.Titel
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Bild aus der Mediathek hinzugefügt.";
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

    /// <summary>
    /// Weist dem Objekt einen eindeutigen, URL-sicheren Slug zu: Basis ist der eingegebene Slug,
    /// sonst der Titel. Bei Kollision wird "-2", "-3", … angehängt, bis er frei ist.
    /// </summary>
    private async Task AssignUniqueSlugAsync(Property model, int? exceptId = null)
    {
        var basis = SlugHelper.Generate(
            string.IsNullOrWhiteSpace(model.Slug) ? model.Titel : model.Slug);
        if (string.IsNullOrWhiteSpace(basis)) basis = "objekt";

        var slug = basis;
        var i = 2;
        // IgnoreQueryFilters: auch Objekte im Papierkorb belegen ihren Slug (Unique-Index gilt für alle).
        while (await db.Properties.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug && p.Id != (exceptId ?? 0)))
            slug = $"{basis}-{i++}";

        model.Slug = slug;
    }
}
