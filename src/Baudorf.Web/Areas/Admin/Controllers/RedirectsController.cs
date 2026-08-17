using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedirectEntity = Baudorf.Web.Models.Entities.Redirect; // Namenskollision mit ControllerBase.Redirect()

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class RedirectsController(ApplicationDbContext db, IRedirectService redirects) : Controller
{
    // ---------- Weiterleitungen ----------

    public async Task<IActionResult> Index()
    {
        var list = await db.Redirects.AsNoTracking()
            .OrderByDescending(r => r.Treffer).ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
        ViewData["Offen404"] = await db.NotFoundHits.CountAsync(n => !n.Erledigt);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create(string? von) => View("Form", new RedirectEntity { VonPfad = von ?? "" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RedirectEntity model)
    {
        Sanitize(model);
        if (!ModelState.IsValid) return View("Form", model);

        if (await db.Redirects.AnyAsync(r => r.VonPfad == model.VonPfad))
        {
            ModelState.AddModelError(nameof(RedirectEntity.VonPfad), "Für diesen Pfad existiert bereits eine Weiterleitung.");
            return View("Form", model);
        }

        model.CreatedAt = DateTimeOffset.UtcNow;
        db.Redirects.Add(model);
        // Passenden 404-Eintrag als erledigt markieren.
        await MarkiereHitErledigtAsync(model.VonPfad);
        await db.SaveChangesAsync();
        redirects.InvalidateCache();
        TempData["Success"] = "Weiterleitung angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await db.Redirects.FindAsync(id);
        if (r is null) return NotFound();
        return View("Form", r);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RedirectEntity model)
    {
        var r = await db.Redirects.FindAsync(id);
        if (r is null) return NotFound();

        Sanitize(model);
        if (!ModelState.IsValid) return View("Form", model);

        if (await db.Redirects.AnyAsync(x => x.VonPfad == model.VonPfad && x.Id != id))
        {
            ModelState.AddModelError(nameof(RedirectEntity.VonPfad), "Für diesen Pfad existiert bereits eine Weiterleitung.");
            return View("Form", model);
        }

        r.VonPfad = model.VonPfad; r.NachPfad = model.NachPfad; r.Code = model.Code;
        r.IstAktiv = model.IstAktiv; r.Notiz = model.Notiz;
        await db.SaveChangesAsync();
        redirects.InvalidateCache();
        TempData["Success"] = "Weiterleitung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await db.Redirects.FindAsync(id);
        if (r is null) return NotFound();
        db.Redirects.Remove(r);
        await db.SaveChangesAsync();
        redirects.InvalidateCache();
        TempData["Success"] = "Weiterleitung gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- 404-Protokoll ----------

    public async Task<IActionResult> Protokoll(bool alle = false)
    {
        var query = db.NotFoundHits.AsNoTracking().AsQueryable();
        if (!alle) query = query.Where(n => !n.Erledigt);
        var list = await query.OrderByDescending(n => n.Anzahl).ThenByDescending(n => n.Zuletzt)
            .Take(300).ToListAsync();
        ViewData["Alle"] = alle;
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HitErledigt(int id)
    {
        var h = await db.NotFoundHits.FindAsync(id);
        if (h is not null) { h.Erledigt = true; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Protokoll));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HitLoeschen(int id)
    {
        var h = await db.NotFoundHits.FindAsync(id);
        if (h is not null) { db.NotFoundHits.Remove(h); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Protokoll));
    }

    // ---------- Helfer ----------

    private static void Sanitize(RedirectEntity model)
    {
        model.VonPfad = IRedirectService.Normalize(model.VonPfad);
        model.NachPfad = (model.NachPfad ?? "").Trim();
        if (model.Code != 302) model.Code = 301;
        // Ziel darf nicht auf sich selbst zeigen (Schleife).
        // (Vergleich case-insensitive nur für relative Pfade.)
    }

    private async Task MarkiereHitErledigtAsync(string vonPfad)
    {
        var hit = await db.NotFoundHits.FirstOrDefaultAsync(n => n.Pfad == vonPfad);
        if (hit is not null) hit.Erledigt = true;
    }
}
