using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class BlogController(ApplicationDbContext db, IStorageService storage, IMediaLibrary media) : Controller
{
    public async Task<IActionResult> Index()
    {
        var list = await db.BlogPosts.AsNoTracking().OrderByDescending(b => b.CreatedAt).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new BlogPost());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPost model, IFormFile? cover)
    {
        await ProcessAsync(model, cover, null, 0);
        if (!ModelState.IsValid) return View("Form", model);

        model.CreatedAt = DateTimeOffset.UtcNow;
        if (model.IstVeroeffentlicht && model.PublishedAt is null) model.PublishedAt = DateTimeOffset.UtcNow;
        db.BlogPosts.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Beitrag angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var b = await db.BlogPosts.FindAsync(id);
        if (b is null) return NotFound();
        return View("Form", b);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlogPost model, IFormFile? cover)
    {
        var b = await db.BlogPosts.FindAsync(id);
        if (b is null) return NotFound();

        await ProcessAsync(model, cover, b.CoverUrl, id);
        if (!ModelState.IsValid) return View("Form", model);

        b.Titel = model.Titel; b.Slug = model.Slug; b.Excerpt = model.Excerpt; b.Body = model.Body;
        b.Kategorie = model.Kategorie; b.Tags = model.Tags;
        b.MetaTitle = model.MetaTitle; b.MetaDescription = model.MetaDescription;
        b.CoverUrl = model.CoverUrl;
        if (model.IstVeroeffentlicht && !b.IstVeroeffentlicht && b.PublishedAt is null)
            b.PublishedAt = DateTimeOffset.UtcNow;
        b.IstVeroeffentlicht = model.IstVeroeffentlicht;

        await db.SaveChangesAsync();
        TempData["Success"] = "Beitrag gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await db.BlogPosts.FindAsync(id);
        if (b is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(b.CoverUrl)) await storage.DeleteAsync(b.CoverUrl);
        db.BlogPosts.Remove(b);
        await db.SaveChangesAsync();
        TempData["Success"] = "Beitrag gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ProcessAsync(BlogPost model, IFormFile? cover, string? existingCover, int exceptId)
    {
        // Slug ist optional → automatisch aus Titel (oder eingegebenem Slug) erzeugen und
        // bei Kollision "-2", "-3", … anhängen, bis er eindeutig ist.
        ModelState.Remove(nameof(BlogPost.Slug));
        var basis = SlugHelper.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Titel : model.Slug);
        if (string.IsNullOrWhiteSpace(basis)) basis = "beitrag";
        var slug = basis; var n = 2;
        while (await db.BlogPosts.AnyAsync(b => b.Slug == slug && b.Id != exceptId)) slug = $"{basis}-{n++}";
        model.Slug = slug;

        // CoverUrl ist bereits aus dem Formular gebunden (Bestand oder aus der Mediathek gewählt).
        if (string.IsNullOrWhiteSpace(model.CoverUrl)) model.CoverUrl = existingCover;
        if (cover is { Length: > 0 })
        {
            if (UploadValidation.IsValidImage(cover.FileName, cover.ContentType, cover.Length, out var err))
            {
                // Kein Löschen der alten Datei: sie bleibt in der Mediathek wiederverwendbar.
                model.CoverUrl = (await media.SaveAsync(cover)).Url;
            }
            else
            {
                ModelState.AddModelError("cover", err!);
            }
        }
    }
}
