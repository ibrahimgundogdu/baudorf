using Baudorf.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class WiderrufController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.WiderrufAntraege.OrderByDescending(w => w.CreatedAt).ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Erledigt(int id)
    {
        var a = await db.WiderrufAntraege.FindAsync(id);
        if (a is null) return NotFound();
        a.Erledigt = !a.Erledigt;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.WiderrufAntraege.FindAsync(id);
        if (a is null) return NotFound();
        db.WiderrufAntraege.Remove(a);
        await db.SaveChangesAsync();
        TempData["Success"] = "Widerruf gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
