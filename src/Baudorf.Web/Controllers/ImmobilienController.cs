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

    /// <summary>
    /// Off-Market-Freigabe: angemeldet UND (Admin/Redakteur ODER als Investor freigegeben).
    /// Erstbesucher sehen Off-Market nur verschleiert; freigegebene Nutzer klar.
    /// </summary>
    private async Task<bool> CanViewOffMarketAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return false;
        if (User.IsInRole("Admin") || User.IsInRole("Redakteur")) return true;
        var user = await userMgr.GetUserAsync(User);
        return user?.IstFreigegeben == true;
    }

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

        // Vertrauliche Off-Market-Daten (Preis, Faktor, Kennzahlen, Bild-URLs) serverseitig
        // entfernen, wenn der Nutzer nicht freigegeben ist — nichts davon erreicht das DOM.
        var darfOffMarket = await CanViewOffMarketAsync();
        if (!darfOffMarket)
            foreach (var o in objekte.Where(o => o.IstOffMarket))
                o.RedactOffMarket();

        return View(new ImmobilienListViewModel
        {
            Objekte = objekte,
            Filter = filter,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total,
            CanViewOffMarket = darfOffMarket
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
        var darfOffMarket = await CanViewOffMarketAsync();
        var gesperrt = objekt.IstOffMarket && !darfOffMarket;

        // Objekt-Aufruf protokollieren (für Admin-Aktivität).
        db.PropertyViews.Add(new PropertyView
        {
            PropertyId = objekt.Id,
            UserId = istAngemeldet ? userMgr.GetUserId(User) : null,
            IpAdresse = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();

        var aehnliche = await db.Properties
            .AsNoTracking()
            .Where(p => p.IstVeroeffentlicht && p.Id != objekt.Id && p.Art == objekt.Art)
            .Include(p => p.Medien)
            .OrderByDescending(p => p.IstFeatured)
            .Take(3)
            .ToListAsync();

        // WICHTIG: vertrauliche Off-Market-Daten serverseitig entfernen, BEVOR die View rendert —
        // so gelangen Preis, Kennzahlen und Bild-URLs gar nicht erst ins HTML (nicht per F12 lesbar).
        if (gesperrt) objekt.RedactOffMarket();
        foreach (var a in aehnliche)
            if (a.IstOffMarket && !darfOffMarket) a.RedactOffMarket();

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
