using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class LookupsController(ApplicationDbContext db, ILookupService lookups) : Controller
{
    // Aktuell verwaltete Kategorie (weitere lassen sich später ergänzen).
    private const string Kat = "objektart";

    public async Task<IActionResult> Index()
    {
        var list = await db.LookupOptions.Where(l => l.Kategorie == Kat)
            .OrderBy(l => l.Reihenfolge).ThenBy(l => l.Label).ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string label, string? wert)
    {
        label = (label ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            TempData["Error"] = "Bitte einen Namen eingeben.";
            return RedirectToAction(nameof(Index));
        }

        var key = SlugHelper.Generate(string.IsNullOrWhiteSpace(wert) ? label : wert);
        if (string.IsNullOrWhiteSpace(key)) key = "option";
        var basis = key; var n = 2;
        while (await db.LookupOptions.AnyAsync(l => l.Kategorie == Kat && l.Wert == key)) key = $"{basis}-{n++}";

        var maxOrder = await db.LookupOptions.Where(l => l.Kategorie == Kat).MaxAsync(l => (int?)l.Reihenfolge) ?? -1;
        db.LookupOptions.Add(new LookupOption { Kategorie = Kat, Wert = key, Label = label, Reihenfolge = maxOrder + 1 });
        await db.SaveChangesAsync();
        lookups.Invalidate();
        TempData["Success"] = "Option hinzugefügt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string label, bool istAktiv)
    {
        var o = await db.LookupOptions.FindAsync(id);
        if (o is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(label)) o.Label = label.Trim();
        o.IstAktiv = istAktiv;
        await db.SaveChangesAsync();
        lookups.Invalidate();
        TempData["Success"] = "Gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction)
    {
        var list = await db.LookupOptions.Where(l => l.Kategorie == Kat)
            .OrderBy(l => l.Reihenfolge).ThenBy(l => l.Label).ToListAsync();
        var idx = list.FindIndex(l => l.Id == id);
        var swap = idx + direction;
        if (idx >= 0 && swap >= 0 && swap < list.Count)
        {
            (list[idx].Reihenfolge, list[swap].Reihenfolge) = (list[swap].Reihenfolge, list[idx].Reihenfolge);
            await db.SaveChangesAsync();
            lookups.Invalidate();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await db.LookupOptions.FindAsync(id);
        if (o is null) return NotFound();

        // Wird die Option noch von Objekten genutzt? Dann nicht löschen (nur deaktivieren).
        var inUse = await db.Properties.IgnoreQueryFilters().AnyAsync(p => p.ArtKey == o.Wert);
        if (inUse)
        {
            TempData["Error"] = "Diese Option wird von Objekten verwendet und kann nicht gelöscht werden. Sie können sie stattdessen deaktivieren.";
            return RedirectToAction(nameof(Index));
        }

        db.LookupOptions.Remove(o);
        await db.SaveChangesAsync();
        lookups.Invalidate();
        TempData["Success"] = "Option gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
