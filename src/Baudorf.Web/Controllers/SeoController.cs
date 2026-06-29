using System.Text;
using System.Xml.Linq;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Baudorf.Web.Controllers;

/// <summary>robots.txt und sitemap.xml — dynamisch erzeugt mit absoluten URLs.</summary>
public class SeoController(ApplicationDbContext db, IOptions<SiteOptions> siteOpt) : Controller
{
    private readonly SiteOptions _site = siteOpt.Value;

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var sitemapUrl = SeoUrls.Absolute(Request, _site, "/sitemap.xml");
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /Admin");
        sb.AppendLine("Disallow: /Identity");
        sb.AppendLine("Disallow: /WhatsApp");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {sitemapUrl}");
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(ns + "urlset");

        XElement Url(string path, string changefreq, string priority, DateTime? lastmod = null)
        {
            var el = new XElement(ns + "url",
                new XElement(ns + "loc", SeoUrls.Absolute(Request, _site, path)),
                new XElement(ns + "changefreq", changefreq),
                new XElement(ns + "priority", priority));
            if (lastmod.HasValue)
            {
                el.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-dd")));
            }
            return el;
        }

        // Statische öffentliche Seiten
        urlset.Add(Url("/", "weekly", "1.0"));
        urlset.Add(Url("/Immobilien", "daily", "0.9"));
        urlset.Add(Url("/Aktuelles", "weekly", "0.7"));
        urlset.Add(Url("/Kontakt", "monthly", "0.6"));
        urlset.Add(Url("/Legal/Impressum", "yearly", "0.3"));
        urlset.Add(Url("/Legal/Datenschutz", "yearly", "0.3"));
        urlset.Add(Url("/Legal/Agb", "yearly", "0.3"));

        // Veröffentlichte, NICHT off-market Objekte (off-market ist gated → kein Index)
        var objekte = await db.Properties
            .Where(p => p.IstVeroeffentlicht && !p.IstOffMarket)
            .Select(p => new { p.Slug, p.UpdatedAt, p.CreatedAt })
            .ToListAsync();

        foreach (var o in objekte)
        {
            urlset.Add(Url($"/Immobilien/Details/{o.Slug}", "weekly", "0.8", o.UpdatedAt ?? o.CreatedAt));
        }

        // Veröffentlichte Blog-/Insights-Beiträge
        var beitraege = await db.BlogPosts
            .Where(b => b.IstVeroeffentlicht)
            .Select(b => new { b.Slug, b.PublishedAt, b.CreatedAt })
            .ToListAsync();

        foreach (var b in beitraege)
        {
            urlset.Add(Url($"/Aktuelles/{b.Slug}", "monthly", "0.6", b.PublishedAt ?? b.CreatedAt));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);
        return Content(doc.Declaration + "\n" + doc, "application/xml; charset=utf-8");
    }
}
