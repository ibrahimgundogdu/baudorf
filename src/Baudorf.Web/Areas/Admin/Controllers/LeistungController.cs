using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class LeistungController(ApplicationDbContext db, IMediaLibrary media) : Controller
{
    public async Task<IActionResult> Index()
    {
        var list = await db.Leistungen.AsNoTracking().OrderBy(l => l.Reihenfolge).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new Leistung { Reihenfolge = 1, IstVeroeffentlicht = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Leistung model, IFormFile? cover)
    {
        await ProcessAsync(model, cover, null);
        if (!ModelState.IsValid) return View("Form", model);

        model.CreatedAt = DateTime.UtcNow;
        db.Leistungen.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Leistung angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var l = await db.Leistungen.FindAsync(id);
        if (l is null) return NotFound();
        return View("Form", l);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Leistung model, IFormFile? cover)
    {
        var l = await db.Leistungen.FindAsync(id);
        if (l is null) return NotFound();

        await ProcessAsync(model, cover, l.CoverUrl);
        if (!ModelState.IsValid) return View("Form", model);

        l.Titel = model.Titel; l.Slug = model.Slug; l.Overline = model.Overline;
        l.Teaser = model.Teaser; l.Body = model.Body; l.CoverUrl = model.CoverUrl;
        l.MetaTitle = model.MetaTitle; l.MetaDescription = model.MetaDescription;
        l.Reihenfolge = model.Reihenfolge; l.IstVeroeffentlicht = model.IstVeroeffentlicht;

        await db.SaveChangesAsync();
        TempData["Success"] = "Leistung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await db.Leistungen.FindAsync(id);
        if (l is null) return NotFound();
        db.Leistungen.Remove(l);
        await db.SaveChangesAsync();
        TempData["Success"] = "Leistung gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ProcessAsync(Leistung model, IFormFile? cover, string? existingCover)
    {
        if (string.IsNullOrWhiteSpace(model.Slug) && !string.IsNullOrWhiteSpace(model.Titel))
            model.Slug = SlugHelper.Generate(model.Titel);
        else if (!string.IsNullOrWhiteSpace(model.Slug))
            model.Slug = SlugHelper.Generate(model.Slug);

        if (!string.IsNullOrWhiteSpace(model.Slug))
            ModelState.Remove(nameof(Leistung.Slug));

        if (!string.IsNullOrWhiteSpace(model.Slug) &&
            await db.Leistungen.AnyAsync(l => l.Slug == model.Slug && l.Id != model.Id))
            ModelState.AddModelError(nameof(Leistung.Slug), "Dieser Slug ist bereits vergeben.");

        // CoverUrl ist bereits aus dem Formular gebunden (Bestand oder aus der Mediathek gewählt).
        if (string.IsNullOrWhiteSpace(model.CoverUrl)) model.CoverUrl = existingCover;
        if (cover is { Length: > 0 })
        {
            if (UploadValidation.IsValidImage(cover.FileName, cover.ContentType, cover.Length, out var err))
                model.CoverUrl = (await media.SaveAsync(cover)).Url;
            else
                ModelState.AddModelError("cover", err!);
        }
    }
}
