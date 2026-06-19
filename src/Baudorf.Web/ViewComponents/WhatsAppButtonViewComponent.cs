using Baudorf.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.ViewComponents;

/// <summary>Schwebender WhatsApp-Button, gesteuert über die Einstellungen.</summary>
public class WhatsAppButtonViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await db.SiteSettings.AsNoTracking()
            .Where(s => s.Key == "whatsapp.enabled" || s.Key == "whatsapp.number")
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        var enabled = string.Equals(settings.GetValueOrDefault("whatsapp.enabled"), "true", StringComparison.OrdinalIgnoreCase);
        var hasNumber = !string.IsNullOrWhiteSpace(settings.GetValueOrDefault("whatsapp.number"));

        return View(enabled && hasNumber);
    }
}
