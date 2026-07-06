using System.Net;
using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Baudorf.Web.Controllers;

/// <summary>
/// Cookie-Einwilligung: serverseitiger Nachweis (DSGVO Art. 5 Abs. 2 / Art. 7 —
/// Rechenschaftspflicht). Datenminimierung: IP anonymisiert, kein Personenbezug nötig.
/// </summary>
public class CookieController(ApplicationDbContext db) : Controller
{
    public record ConsentDto(string[]? Categories, string? Version, string? Action);

    private static readonly HashSet<string> Erlaubt =
        new(StringComparer.OrdinalIgnoreCase) { "necessary", "statistics", "marketing" };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consent([FromBody] ConsentDto dto)
    {
        // Kategorien gegen Whitelist säubern; "necessary" ist immer gesetzt.
        var cats = new List<string> { "necessary" };
        if (dto.Categories != null)
        {
            foreach (var c in dto.Categories)
            {
                var key = c?.Trim().ToLowerInvariant() ?? "";
                if (Erlaubt.Contains(key) && !cats.Contains(key))
                    cats.Add(key);
            }
        }

        var aktion = dto.Action switch
        {
            "accept" or "reject" or "custom" => dto.Action!,
            _ => "custom"
        };

        var version = (dto.Version ?? "").Trim();
        if (version.Length == 0) version = "unbekannt";
        if (version.Length > 40) version = version[..40];

        var ua = Request.Headers.UserAgent.ToString();
        if (ua.Length > 400) ua = ua[..400];

        db.ConsentLogs.Add(new ConsentLog
        {
            Referenz = Guid.NewGuid(),
            Kategorien = string.Join(",", cats),
            Version = version,
            Aktion = aktion,
            IpAnonymisiert = AnonymizeIp(HttpContext.Connection.RemoteIpAddress),
            UserAgent = string.IsNullOrWhiteSpace(ua) ? null : ua
        });
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>IPv4: letztes Oktett → 0; IPv6: nur /48-Präfix behalten (Rest genullt).</summary>
    private static string? AnonymizeIp(IPAddress? ip)
    {
        if (ip is null) return null;
        var bytes = ip.GetAddressBytes();
        if (bytes.Length == 4)
        {
            bytes[3] = 0;
            return new IPAddress(bytes).ToString();
        }
        if (bytes.Length == 16)
        {
            for (var i = 6; i < 16; i++) bytes[i] = 0;
            return new IPAddress(bytes).ToString();
        }
        return null;
    }
}
