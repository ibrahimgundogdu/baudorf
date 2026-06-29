using Baudorf.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

public class LegalController(ApplicationDbContext db) : Controller
{
    public Task<IActionResult> Impressum() => RenderAsync("impressum");
    public Task<IActionResult> Datenschutz() => RenderAsync("datenschutz");
    public Task<IActionResult> Agb() => RenderAsync("agb");

    private async Task<IActionResult> RenderAsync(string slug)
    {
        var page = await db.LegalPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug);
        if (page is null) return NotFound();

        ViewData["Title"] = page.Titel;
        return View("Page", page);
    }
}
