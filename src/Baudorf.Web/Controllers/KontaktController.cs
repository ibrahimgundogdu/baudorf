using System.Net;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Models.ViewModels;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

public class KontaktController(
    ApplicationDbContext db,
    IEmailService email,
    ILogger<KontaktController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? objekt)
    {
        var vm = new KontaktViewModel();

        if (!string.IsNullOrWhiteSpace(objekt))
        {
            var p = await db.Properties.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == objekt && x.IstVeroeffentlicht);
            if (p is not null)
            {
                vm.ObjektSlug = p.Slug;
                vm.ObjektTitel = p.Titel;
                vm.Interesse = InterestType.KaeuferPrivatinvestor;
                vm.Nachricht = $"Ich interessiere mich für das Objekt \"{p.Titel}\" und bitte um vertrauliche Informationen.";
            }
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("kontakt")]
    public async Task<IActionResult> Senden(KontaktViewModel vm)
    {
        // Honeypot: ausgefülltes verstecktes Feld → Bot. Stillschweigend "Erfolg" zeigen, nichts speichern.
        if (!string.IsNullOrWhiteSpace(vm.Website))
        {
            logger.LogWarning("Honeypot ausgelöst — Anfrage verworfen.");
            ViewData["Gesendet"] = true;
            return View(nameof(Index), new KontaktViewModel());
        }

        if (!ModelState.IsValid)
            return View(nameof(Index), vm);

        int? propertyId = null;
        if (!string.IsNullOrWhiteSpace(vm.ObjektSlug))
        {
            propertyId = await db.Properties
                .Where(p => p.Slug == vm.ObjektSlug)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();
        }

        var lead = new Lead
        {
            Vorname = vm.Vorname.Trim(),
            Nachname = vm.Nachname.Trim(),
            Email = vm.Email.Trim(),
            Telefon = vm.Telefon?.Trim(),
            Interesse = vm.Interesse,
            Nachricht = vm.Nachricht.Trim(),
            PropertyId = propertyId,
            Status = LeadStatus.Neu,
            DsgvoAkzeptiert = vm.DsgvoAkzeptiert,
            IpAdresse = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        await NotifyAdminAsync(lead, vm.ObjektTitel);

        ViewData["Gesendet"] = true;
        return View(nameof(Index), new KontaktViewModel());
    }

    private async Task NotifyAdminAsync(Lead lead, string? objektTitel)
    {
        try
        {
            var adminEmail = await db.SiteSettings
                .Where(s => s.Key == "contact.email")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "andrea.krueger@baudorf.de";

            var objektZeile = objektTitel is null ? "" : $"<p><strong>Objekt:</strong> {WebUtility.HtmlEncode(objektTitel)}</p>";
            var body = $"""
                <h2>Neue Anfrage über das Kontaktformular</h2>
                <p><strong>Name:</strong> {WebUtility.HtmlEncode(lead.Vorname)} {WebUtility.HtmlEncode(lead.Nachname)}</p>
                <p><strong>E-Mail:</strong> {WebUtility.HtmlEncode(lead.Email)}</p>
                <p><strong>Telefon:</strong> {WebUtility.HtmlEncode(lead.Telefon ?? "—")}</p>
                <p><strong>Interesse:</strong> {WebUtility.HtmlEncode(lead.Interesse.Label())}</p>
                {objektZeile}
                <p><strong>Nachricht:</strong><br />{WebUtility.HtmlEncode(lead.Nachricht).Replace("\n", "<br />")}</p>
                """;

            await email.SendAsync(adminEmail, "Neue Anfrage — Baudorf Website", body);
        }
        catch (Exception ex)
        {
            // Mailversand darf die Lead-Speicherung nicht scheitern lassen.
            logger.LogError(ex, "Admin-Benachrichtigung für Lead {LeadId} fehlgeschlagen.", lead.Id);
        }
    }
}
