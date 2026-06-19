using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class SettingsController(ApplicationDbContext db) : Controller
{
    /// <summary>Bekannte, im UI editierbare Einstellungen.</summary>
    public static readonly SettingDescriptor[] Known =
    [
        new("contact.company", "Firmenname", "Kontakt"),
        new("contact.street", "Straße", "Kontakt"),
        new("contact.city", "PLZ / Ort", "Kontakt"),
        new("contact.phone", "Telefon", "Kontakt"),
        new("contact.email", "E-Mail", "Kontakt"),
        new("contact.hours", "Öffnungszeiten", "Kontakt"),
        new("brand.slogan", "Slogan", "Marke"),
        new("brand.claim", "Claim", "Marke"),
        new("whatsapp.enabled", "WhatsApp-Button anzeigen (true/false)", "WhatsApp"),
        new("whatsapp.number", "WhatsApp-Nummer (international, ohne +)", "WhatsApp"),
        new("whatsapp.message", "Vorausgefüllte Nachricht", "WhatsApp"),
        new("social.instagram", "Instagram-URL", "Social"),
        new("social.linkedin", "LinkedIn-URL", "Social"),
    ];

    public async Task<IActionResult> Index()
    {
        var values = await db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value ?? "");
        ViewBag.Values = values;
        return View(Known);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Dictionary<string, string> settings)
    {
        var existing = await db.SiteSettings.ToDictionaryAsync(s => s.Key);
        foreach (var (key, _, _) in Known)
        {
            var value = settings.TryGetValue(key, out var v) ? v?.Trim() ?? "" : "";
            if (existing.TryGetValue(key, out var setting))
                setting.Value = value;
            else
                db.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Einstellungen gespeichert.";
        return RedirectToAction(nameof(Index));
    }
}

public record SettingDescriptor(string Key, string Label, string Group);
