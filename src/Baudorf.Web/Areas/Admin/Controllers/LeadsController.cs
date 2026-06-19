using System.Net;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class LeadsController(ApplicationDbContext db, IEmailService email) : Controller
{
    public async Task<IActionResult> Index(LeadStatus? status)
    {
        var query = db.Leads.AsNoTracking().Include(l => l.Property).AsQueryable();
        if (status is { } s) query = query.Where(l => l.Status == s);
        var list = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        ViewData["status"] = status;
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await db.Leads.Include(l => l.Property).FirstOrDefaultAsync(l => l.Id == id);
        if (lead is null) return NotFound();
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, LeadStatus status, string? notiz, string? zugewiesenAn)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();
        lead.Status = status;
        lead.Notiz = notiz;
        lead.ZugewiesenAn = zugewiesenAn;
        await db.SaveChangesAsync();
        TempData["Success"] = "Anfrage aktualisiert.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string betreff, string antwort)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();

        if (string.IsNullOrWhiteSpace(antwort))
        {
            TempData["Error"] = "Bitte geben Sie eine Antwort ein.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var body = $"<p>{WebUtility.HtmlEncode(antwort).Replace("\n", "<br />")}</p>" +
                   "<hr /><p style=\"color:#888;font-size:13px\">Baudorf Immobilien GmbH · Auf der Egge 68 · 42555 Velbert</p>";
        await email.SendAsync(lead.Email, string.IsNullOrWhiteSpace(betreff) ? "Ihre Anfrage bei Baudorf Immobilien" : betreff, body);

        var stamp = DateTime.UtcNow.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        lead.Notiz = $"[{stamp}] Antwort gesendet:\n{antwort}\n\n{lead.Notiz}".Trim();
        if (lead.Status == LeadStatus.Neu) lead.Status = LeadStatus.InBearbeitung;
        await db.SaveChangesAsync();

        TempData["Success"] = "Antwort gesendet und protokolliert.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();
        db.Leads.Remove(lead);
        await db.SaveChangesAsync();
        TempData["Success"] = "Anfrage gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
