using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class TeamController(ApplicationDbContext db, IStorageService storage) : Controller
{
    public async Task<IActionResult> Index()
    {
        var list = await db.TeamMembers.AsNoTracking().OrderBy(t => t.Reihenfolge).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new TeamMember());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeamMember model, IFormFile? foto)
    {
        await ProcessPhotoAsync(model, foto, null);
        if (!ModelState.IsValid) return View("Form", model);
        db.TeamMembers.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Team-Mitglied angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var t = await db.TeamMembers.FindAsync(id);
        if (t is null) return NotFound();
        return View("Form", t);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeamMember model, IFormFile? foto)
    {
        var t = await db.TeamMembers.FindAsync(id);
        if (t is null) return NotFound();
        await ProcessPhotoAsync(model, foto, t.FotoUrl);
        if (!ModelState.IsValid) return View("Form", model);

        t.Name = model.Name; t.Rolle = model.Rolle; t.Bio = model.Bio;
        t.FotoUrl = model.FotoUrl; t.Reihenfolge = model.Reihenfolge; t.IstSichtbar = model.IstSichtbar;
        await db.SaveChangesAsync();
        TempData["Success"] = "Team-Mitglied gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await db.TeamMembers.FindAsync(id);
        if (t is null) return NotFound();
        db.TeamMembers.Remove(t);
        await db.SaveChangesAsync();
        TempData["Success"] = "Team-Mitglied gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ProcessPhotoAsync(TeamMember model, IFormFile? foto, string? existing)
    {
        model.FotoUrl = existing;
        if (foto is { Length: > 0 })
        {
            if (UploadValidation.IsValidImage(foto.FileName, foto.ContentType, foto.Length, out var err))
            {
                if (!string.IsNullOrWhiteSpace(existing)) await storage.DeleteAsync(existing);
                await using var stream = foto.OpenReadStream();
                model.FotoUrl = await storage.SaveAsync(stream, foto.FileName, foto.ContentType);
            }
            else ModelState.AddModelError("foto", err!);
        }
    }
}
