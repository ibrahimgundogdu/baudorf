using System.Diagnostics;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

public class HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr) : Controller
{
    public async Task<IActionResult> Index()
    {
        var featured = await db.Properties
            .AsNoTracking()
            .Where(p => p.IstVeroeffentlicht && p.IstFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.Medien)
            .Take(6)
            .ToListAsync();

        var team = await db.TeamMembers
            .AsNoTracking()
            .Where(t => t.IstSichtbar)
            .OrderBy(t => t.Reihenfolge)
            .ToListAsync();

        var insights = await db.BlogPosts
            .AsNoTracking()
            .Where(b => b.IstVeroeffentlicht)
            .OrderByDescending(b => b.PublishedAt)
            .Take(3)
            .ToListAsync();

        var leistungen = await db.Leistungen
            .AsNoTracking()
            .Where(l => l.IstVeroeffentlicht)
            .OrderBy(l => l.Reihenfolge)
            .ToListAsync();

        var settings = await db.SiteSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);

        var sections = await db.HomeSections
            .AsNoTracking()
            .Where(s => s.IstSichtbar)
            .Include(s => s.Items.OrderBy(i => i.Reihenfolge))
            .ToDictionaryAsync(s => s.Key, s => s);

        var vm = new HomeViewModel
        {
            FeaturedObjekte = featured,
            Team = team,
            Insights = insights,
            Leistungen = leistungen,
            Settings = settings,
            Sections = sections,
            CanViewOffMarket = await CanViewOffMarketAsync()
        };
        return View(vm);
    }

    /// <summary>Off-Market-Freigabe: angemeldet UND (Admin/Redakteur ODER als Investor freigegeben).</summary>
    private async Task<bool> CanViewOffMarketAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return false;
        if (User.IsInRole("Admin") || User.IsInRole("Redakteur")) return true;
        var user = await userMgr.GetUserAsync(User);
        return user?.IstFreigegeben == true;
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
