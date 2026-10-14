using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

/// <summary>Frei pflegbare Social-Media-Profile (Footer + JSON-LD "sameAs").</summary>
[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class SocialLinksController(ApplicationDbContext db, ISocialLinkService social) : Controller
{
    public async Task<IActionResult> Index()
    {
        var list = await db.SocialLinks.OrderBy(s => s.Reihenfolge).ThenBy(s => s.Id).ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string plattform, string url, string? bezeichnung)
    {
        var norm = SocialPlatforms.NormalizeUrl(url);
        if (norm is null)
        {
            TempData["Error"] = "Bitte eine gültige Adresse eingeben (z. B. https://www.tiktok.com/@baudorf).";
            return RedirectToAction(nameof(Index));
        }

        var maxOrder = await db.SocialLinks.MaxAsync(s => (int?)s.Reihenfolge) ?? -1;
        db.SocialLinks.Add(new SocialLink
        {
            Plattform = SocialPlatforms.Get(plattform).Key,
            Url = norm,
            Bezeichnung = string.IsNullOrWhiteSpace(bezeichnung) ? null : bezeichnung.Trim(),
            Reihenfolge = maxOrder + 1,
            IstAktiv = true
        });
        await db.SaveChangesAsync();
        social.Invalidate();
        TempData["Success"] = "Profil hinzugefügt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string plattform, string url, string? bezeichnung, bool istAktiv)
    {
        var s = await db.SocialLinks.FindAsync(id);
        if (s is null) return NotFound();

        var norm = SocialPlatforms.NormalizeUrl(url);
        if (norm is null)
        {
            TempData["Error"] = "Bitte eine gültige Adresse eingeben (beginnend mit https://).";
            return RedirectToAction(nameof(Index));
        }

        s.Plattform = SocialPlatforms.Get(plattform).Key;
        s.Url = norm;
        s.Bezeichnung = string.IsNullOrWhiteSpace(bezeichnung) ? null : bezeichnung.Trim();
        s.IstAktiv = istAktiv;
        await db.SaveChangesAsync();
        social.Invalidate();
        TempData["Success"] = "Gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction)
    {
        var list = await db.SocialLinks.OrderBy(s => s.Reihenfolge).ThenBy(s => s.Id).ToListAsync();
        // Reihenfolge erst lückenlos durchnummerieren, damit das Tauschen auch bei
        // gleichen Werten (z. B. nach Migration) zuverlässig funktioniert.
        for (var i = 0; i < list.Count; i++) list[i].Reihenfolge = i;

        var idx = list.FindIndex(s => s.Id == id);
        var swap = idx + direction;
        if (idx >= 0 && swap >= 0 && swap < list.Count)
            (list[idx].Reihenfolge, list[swap].Reihenfolge) = (list[swap].Reihenfolge, list[idx].Reihenfolge);

        await db.SaveChangesAsync();
        social.Invalidate();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await db.SocialLinks.FindAsync(id);
        if (s is null) return NotFound();
        db.SocialLinks.Remove(s);
        await db.SaveChangesAsync();
        social.Invalidate();
        TempData["Success"] = "Profil entfernt.";
        return RedirectToAction(nameof(Index));
    }
}
