using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class LegalController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.LegalPages.OrderBy(l => l.Titel).ToListAsync());

    public async Task<IActionResult> Edit(int id)
    {
        var page = await db.LegalPages.FindAsync(id);
        if (page is null) return NotFound();
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LegalPage model)
    {
        var page = await db.LegalPages.FindAsync(id);
        if (page is null) return NotFound();

        page.Titel = model.Titel;
        page.Overline = model.Overline;
        page.BodyHtml = model.BodyHtml ?? string.Empty;
        page.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = "Rechtsseite gespeichert.";
        return RedirectToAction(nameof(Index));
    }
}
