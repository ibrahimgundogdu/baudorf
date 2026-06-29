using System.Net;
using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Models.ViewModels;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

/// <summary>Öffentliches Widerrufsformular („Vertrag widerrufen") inkl. Admin-Benachrichtigung.</summary>
public class WiderrufController(
    ApplicationDbContext db,
    IEmailService email,
    ILogger<WiderrufController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new WiderrufViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("kontakt")]
    public async Task<IActionResult> Senden(WiderrufViewModel vm)
    {
        // Honeypot: ausgefülltes verstecktes Feld → Bot. Stillschweigend "Erfolg".
        if (!string.IsNullOrWhiteSpace(vm.Website))
        {
            logger.LogWarning("Widerruf-Honeypot ausgelöst — verworfen.");
            ViewData["Gesendet"] = true;
            return View(nameof(Index), new WiderrufViewModel());
        }

        if (!ModelState.IsValid)
            return View(nameof(Index), vm);

        var antrag = new WiderrufAntrag
        {
            Vorname = vm.Vorname.Trim(),
            Nachname = vm.Nachname.Trim(),
            Email = vm.Email.Trim(),
            Strasse = vm.Strasse?.Trim(),
            PlzOrt = vm.PlzOrt?.Trim(),
            Vertragsidentifikation = vm.Vertragsidentifikation.Trim(),
            DatumBeschreibung = vm.DatumBeschreibung.Trim(),
            Bestaetigt = vm.Bestaetigt,
            IpAdresse = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        db.WiderrufAntraege.Add(antrag);
        await db.SaveChangesAsync();

        await NotifyAdminAsync(antrag);

        ViewData["Gesendet"] = true;
        return View(nameof(Index), new WiderrufViewModel());
    }

    private async Task NotifyAdminAsync(WiderrufAntrag a)
    {
        try
        {
            var adminEmail = await db.SiteSettings
                .Where(s => s.Key == "contact.email")
                .Select(s => s.Value)
                .FirstOrDefaultAsync() ?? "andrea.krueger@baudorf.de";

            var body = $"""
                <h2>Neuer Vertragswiderruf</h2>
                <p><strong>Name:</strong> {WebUtility.HtmlEncode(a.Vorname)} {WebUtility.HtmlEncode(a.Nachname)}</p>
                <p><strong>E-Mail:</strong> {WebUtility.HtmlEncode(a.Email)}</p>
                <p><strong>Anschrift:</strong> {WebUtility.HtmlEncode(a.Strasse ?? "—")}, {WebUtility.HtmlEncode(a.PlzOrt ?? "—")}</p>
                <p><strong>Vertragsidentifikation:</strong> {WebUtility.HtmlEncode(a.Vertragsidentifikation)}</p>
                <p><strong>Datum / Beschreibung:</strong><br />{WebUtility.HtmlEncode(a.DatumBeschreibung).Replace("\n", "<br />")}</p>
                <p><strong>Widerrufswille bestätigt:</strong> {(a.Bestaetigt ? "Ja" : "Nein")}</p>
                """;

            await email.SendAsync(adminEmail, "Vertragswiderruf — Baudorf Website", body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin-Benachrichtigung für Widerruf {Id} fehlgeschlagen.", a.Id);
        }
    }
}
