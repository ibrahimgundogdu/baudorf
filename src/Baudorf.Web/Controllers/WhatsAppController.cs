using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Controllers;

/// <summary>Click-to-Chat: protokolliert den Klick und leitet zu wa.me weiter.</summary>
public class WhatsAppController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Chat(int? objekt)
    {
        var settings = await db.SiteSettings.AsNoTracking()
            .Where(s => s.Key == "whatsapp.number" || s.Key == "whatsapp.message")
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        var number = new string((settings.GetValueOrDefault("whatsapp.number") ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(number))
            return RedirectToAction("Index", "Kontakt");

        var message = settings.GetValueOrDefault("whatsapp.message") ?? "";

        int? propertyId = objekt;
        if (propertyId is { } pid && !await db.Properties.AnyAsync(p => p.Id == pid))
            propertyId = null;

        db.WhatsAppClicks.Add(new WhatsAppClick
        {
            UserId = User.Identity?.IsAuthenticated == true ? userMgr.GetUserId(User) : null,
            PropertyId = propertyId,
            Quelle = Request.Headers.Referer.ToString(),
            IpAdresse = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();

        var url = $"https://wa.me/{number}";
        if (!string.IsNullOrWhiteSpace(message))
            url += $"?text={Uri.EscapeDataString(message)}";

        return Redirect(url);
    }
}
