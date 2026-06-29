using Baudorf.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

/// <summary>Öffentliche „Markt &amp; Insights"-Seiten (Liste + Detail).</summary>
public class BlogController(ApplicationDbContext db) : Controller
{
    [HttpGet("/Aktuelles")]
    public async Task<IActionResult> Index()
    {
        var posts = await db.BlogPosts.AsNoTracking()
            .Where(b => b.IstVeroeffentlicht)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .ToListAsync();
        return View(posts);
    }

    [HttpGet("/Aktuelles/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var post = await db.BlogPosts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IstVeroeffentlicht);
        if (post is null) return NotFound();

        var weitere = await db.BlogPosts.AsNoTracking()
            .Where(b => b.IstVeroeffentlicht && b.Id != post.Id)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(2)
            .ToListAsync();
        ViewData["Weitere"] = weitere;

        return View(post);
    }
}
