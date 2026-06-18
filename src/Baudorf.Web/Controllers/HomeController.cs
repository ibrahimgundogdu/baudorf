using System.Diagnostics;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
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

        var settings = await db.SiteSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);

        var vm = new HomeViewModel
        {
            FeaturedObjekte = featured,
            Team = team,
            Insights = insights,
            Settings = settings
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
