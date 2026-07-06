using Baudorf.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

/// <summary>Öffentliche Leistungen-Seiten (Übersicht + Detail).</summary>
public class LeistungController(ApplicationDbContext db) : Controller
{
    [HttpGet("/Leistungen")]
    public async Task<IActionResult> Index()
    {
        var list = await db.Leistungen.AsNoTracking()
            .Where(l => l.IstVeroeffentlicht)
            .OrderBy(l => l.Reihenfolge)
            .ToListAsync();
        return View(list);
    }

    [HttpGet("/Leistungen/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var leistung = await db.Leistungen.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Slug == slug && l.IstVeroeffentlicht);
        if (leistung is null) return NotFound();

        // Alle Leistungen (inkl. aktueller) für das 6-Feld-Raster wie auf der Startseite.
        var alle = await db.Leistungen.AsNoTracking()
            .Where(l => l.IstVeroeffentlicht)
            .OrderBy(l => l.Reihenfolge)
            .ToListAsync();
        ViewData["Alle"] = alle;

        return View(leistung);
    }
}
