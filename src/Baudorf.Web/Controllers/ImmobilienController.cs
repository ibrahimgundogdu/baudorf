using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

public class ImmobilienController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr) : Controller
{
    private const int PageSize = 9;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ImmobilienFilter filter)
    {
        var query = db.Properties
            .AsNoTracking()
            .Where(p => p.IstVeroeffentlicht)
            .Include(p => p.Medien)
            .AsQueryable();

        if (filter.Art is { } art)
            query = query.Where(p => p.Art == art);

        if (filter.Status is { } status)
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(p => p.Titel.Contains(q) || (p.Region != null && p.Region.Contains(q)));
        }

        if (filter.PreisMax is { } max)
            query = query.Where(p => p.Kaufpreis != null && p.Kaufpreis <= max);

        query = filter.Sort switch
        {
            "preis-auf" => query.OrderBy(p => p.Kaufpreis ?? decimal.MaxValue),
            "preis-ab" => query.OrderByDescending(p => p.Kaufpreis ?? decimal.MinValue),
            "flaeche" => query.OrderByDescending(p => p.Wohnflaeche ?? p.Grundstuecksflaeche ?? 0),
            _ => query.OrderByDescending(p => p.IstFeatured).ThenByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        var page = Math.Clamp(filter.Page, 1, totalPages);

        var objekte = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new ImmobilienListViewModel
        {
            Objekte = objekte,
            Filter = filter,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total
        });
    }

    [HttpGet("Immobilien/Details/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var objekt = await db.Properties
            .AsNoTracking()
            .Include(p => p.Medien.OrderBy(m => m.Reihenfolge))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IstVeroeffentlicht);

        if (objekt is null) return NotFound();

        var istAngemeldet = User.Identity?.IsAuthenticated == true;
        var istFreigegeben = false;
        if (istAngemeldet)
        {
            var user = await userMgr.GetUserAsync(User);
            istFreigegeben = user?.IstFreigegeben == true;
        }

        var gesperrt = objekt.IstOffMarket && !istFreigegeben;

        var aehnliche = await db.Properties
            .AsNoTracking()
            .Where(p => p.IstVeroeffentlicht && p.Id != objekt.Id && p.Art == objekt.Art)
            .Include(p => p.Medien)
            .OrderByDescending(p => p.IstFeatured)
            .Take(3)
            .ToListAsync();

        ViewData["Title"] = objekt.MetaTitle ?? objekt.Titel;
        ViewData["MetaDescription"] = objekt.MetaDescription;

        return View(new PropertyDetailViewModel
        {
            Objekt = objekt,
            AehnlicheObjekte = aehnliche,
            IstGesperrt = gesperrt,
            IstAngemeldet = istAngemeldet
        });
    }
}
